// SPDX-License-Identifier: MIT
using System.Text;
using System.Text.Json;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;

namespace Nev.Core;

/// <summary>Runs local model inference to answer typed questions about input state.</summary>
public sealed class DecisionModel : IDisposable
{
    private readonly InferenceSession _session;
    private readonly bool _ownsSession;
    private readonly BpeTokenizer _tokenizer;

    //Reserved Tokens
    private readonly IReadOnlyDictionary<string, int> _specialTokenIds;
    private readonly int _clsTokenId;
    private readonly int _sepTokenId;
    private readonly int _maskTokenId;
    private readonly int _padTokenId;
    private readonly bool _normalizeNfc;
    private readonly int _maxLen;
    private readonly int _headMaxLen;
    private readonly double[] _temperatures;
    private readonly Dictionary<string, double> _temperaturesByOptions;

    public DecisionModel(string modelType = nameof(ModelCheckpoint.English), ISessionProvider? provider = null)
    {
        provider ??= SessionProvider.Create(modelType);
        // The default provider is cached and shared. Custom-provider sessions are owned by this instance.
        _ownsSession = provider is not SessionProvider;
        _session = provider.GetSession();

        (_tokenizer, _specialTokenIds, _normalizeNfc) = CreateTokenizer(provider.GetTokenizerPath());
        using var tokenizerConfig = JsonDocument.Parse(File.ReadAllText(provider.GetTokenizerConfigPath()));

        var tokenizerConfigRoot = tokenizerConfig.RootElement;
        _clsTokenId = _specialTokenIds[tokenizerConfigRoot.GetProperty("cls_token").GetString()!];  // e.g. "[CLS]"
        _sepTokenId = _specialTokenIds[tokenizerConfigRoot.GetProperty("sep_token").GetString()!];  // e.g. "[SEP]"
        _maskTokenId = _specialTokenIds[tokenizerConfigRoot.GetProperty("mask_token").GetString()!];  // e.g. "[MASK]"
        _padTokenId = _specialTokenIds[tokenizerConfigRoot.GetProperty("pad_token").GetString()!];  // e.g. "[PAD]"

        using var config = JsonDocument.Parse(File.ReadAllText(provider.GetRLConfigPath()));
        var root = config.RootElement;
        _maxLen = root.TryGetProperty("max_len", out var maxLen) ? maxLen.GetInt32() : 512;
        _headMaxLen = root.TryGetProperty("head_max_len", out var headMaxLen) ? headMaxLen.GetInt32() : 192;

        _temperatures = root.TryGetProperty("temperature", out var temperatures)
            ? temperatures.EnumerateArray().Select(value => ClampTemperature(value.GetDouble())).ToArray()
            : [1, 1, 1];

        _temperaturesByOptions = root.TryGetProperty("temperature_by_options", out var byOptions)
            ? byOptions.EnumerateObject().ToDictionary(
                property => property.Name,
                property => ClampTemperature(property.Value.GetDouble()),
                StringComparer.Ordinal)
            : new Dictionary<string, double>(StringComparer.Ordinal);
    }

    public IReadOnlyDictionary<string, NodeMetadata> InputMetadata => _session.InputMetadata;

    public IReadOnlyDictionary<string, NodeMetadata> OutputMetadata => _session.OutputMetadata;

    public int[] EncodeAsTokenIDs(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return _tokenizer.EncodeToIds(_normalizeNfc ? text.Normalize(NormalizationForm.FormC) : text).ToArray();
    }

    public IDisposableReadOnlyCollection<DisposableNamedOnnxValue> Run(
        DenseTensor<long> inputIds,
        DenseTensor<long> attentionMask,
        DenseTensor<long> markerPositions,
        DenseTensor<bool> markerMask,
        DenseTensor<long> questionTypes)
        => _session.Run(
        [
            NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask),
            NamedOnnxValue.CreateFromTensor("marker_pos", markerPositions),
            NamedOnnxValue.CreateFromTensor("marker_mask", markerMask),
            NamedOnnxValue.CreateFromTensor("qtype", questionTypes),
        ]);

    public PredictionResult Predict(object? state, QuestionSet questions)
    {
        ArgumentNullException.ThrowIfNull(questions);
        if (questions.Count == 0)
            return new PredictionResult(new Dictionary<string, Answer>(), 0);

        // Keep user state from injecting mask tokens that the model treats as option markers.
        var stateText = JsonInputFormatter.State(state).Replace("[MASK]", " ", StringComparison.Ordinal);
        var stateIds = EncodeAsTokenIDs(stateText);
        var rows = questions.Select(entry =>
        {
            var question = entry.Value;
            var sequence = SequenceBuilder.Build(question, stateIds, EncodeAsTokenIDs, _clsTokenId, _sepTokenId, _maskTokenId, _maxLen, _headMaxLen);
            return (Id: entry.Key, question, sequence.Ids, sequence.Markers);
        }).ToArray();

        var batchSize = rows.Length;
        var sequenceLength = rows.Max(row => row.Ids.Length);
        var markerCount = Math.Max(2, rows.Max(row => row.Markers.Length));
        var padId = _padTokenId;
        var inputIds = new DenseTensor<long>(new[] { batchSize, sequenceLength });
        var attentionMask = new DenseTensor<long>(new[] { batchSize, sequenceLength });
        var markerPositions = new DenseTensor<long>(new[] { batchSize, markerCount });
        var markerMask = new DenseTensor<bool>(new[] { batchSize, markerCount });
        var questionTypes = new DenseTensor<long>(new[] { batchSize });
        var inputTokenCount = 0;

        for (var rowIndex = 0; rowIndex < batchSize; rowIndex++)
        {
            var row = rows[rowIndex];
            questionTypes[rowIndex] = (long)row.question.Type;
            inputTokenCount += row.Ids.Length;
            for (var tokenIndex = 0; tokenIndex < sequenceLength; tokenIndex++)
                inputIds[rowIndex, tokenIndex] = padId;
            for (var tokenIndex = 0; tokenIndex < row.Ids.Length; tokenIndex++)
            {
                inputIds[rowIndex, tokenIndex] = row.Ids[tokenIndex];
                attentionMask[rowIndex, tokenIndex] = 1;
            }
            for (var markerIndex = 0; markerIndex < row.Markers.Length; markerIndex++)
            {
                markerPositions[rowIndex, markerIndex] = row.Markers[markerIndex];
                markerMask[rowIndex, markerIndex] = true;
            }
        }

        using var outputs = Run(inputIds, attentionMask, markerPositions, markerMask, questionTypes);
        var logits = outputs.Single(output => output.Name == "logits").AsTensor<float>();
        var actionLogits = outputs.Single(output => output.Name == "act_logits").AsTensor<float>();
        var answers = new Dictionary<string, Answer>(StringComparer.Ordinal);

        for (var rowIndex = 0; rowIndex < batchSize; rowIndex++)
        {
            var row = rows[rowIndex];
            var rowLogits = new float[row.Markers.Length];
            for (var markerIndex = 0; markerIndex < rowLogits.Length; markerIndex++)
                rowLogits[markerIndex] = logits[rowIndex, markerIndex];

            var optionCount = row.Markers.Length;
            answers.Add(row.Id, AnswerDecoder.Decode(
                row.question,
                rowLogits,
                ResolveTemperature(row.question.Type, optionCount),
                [actionLogits[rowIndex, 0], actionLogits[rowIndex, 1]]));
        }

        return new PredictionResult(answers, inputTokenCount);
    }

    public void Dispose()
    {
        if (_ownsSession)
            _session.Dispose();
    }

    private static (BpeTokenizer Tokenizer, IReadOnlyDictionary<string, int> SpecialTokenIds, bool NormalizeNfc) CreateTokenizer(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var model = root.GetProperty("model");
        var preTokenizerType = root.GetProperty("pre_tokenizer").GetProperty("type").GetString();
        var normalizerType = root.GetProperty("normalizer").GetProperty("type").GetString();
        var vocabulary = JsonSerializer.Deserialize<Dictionary<string, int>>(
            model.GetProperty("vocab").GetRawText())!;
        var specialTokens = root.GetProperty("added_tokens").EnumerateArray()
            .ToDictionary(token => token.GetProperty("content").GetString()!, token => token.GetProperty("id").GetInt32());
        foreach (var token in specialTokens)
            vocabulary[token.Key] = token.Value;

        var merges = model.GetProperty("merges").EnumerateArray().Select(merge =>
            merge.ValueKind == JsonValueKind.String
                ? merge.GetString()!
                : string.Join(' ', merge.EnumerateArray().Select(token => token.GetString()))).ToArray();

        var options = new BpeOptions(vocabulary)
        {
            Merges = merges,
            SpecialTokens = specialTokens,
            UnknownToken = model.GetProperty("unk_token").GetString(), //e.g. "[UNK]" or "<unk>"
        };

        if (preTokenizerType == "ByteLevel")
        {
            options.PreTokenizer = RobertaPreTokenizer.Instance;
            options.ByteLevel = true;
        }
        else if (preTokenizerType == "Metaspace" && normalizerType == "Replace")
        {
            options.Normalizer = new SentencePieceNormalizer(
                removeExtraWhiteSpaces: true,
                addDummyPrefix: true,
                escapeWhiteSpaces: true,
                treatWhitespaceAsSuffix: false,
                specialTokens: specialTokens);
            options.PreTokenizer = PreTokenizer.CreateWhiteSpace(specialTokens);
        }
        else
        {
            throw new NotSupportedException($"Unsupported tokenizer: {normalizerType}/{preTokenizerType}.");
        }

        return (BpeTokenizer.Create(options), specialTokens, normalizerType == "NFC");
    }

    private double ResolveTemperature(QuestionType type, int optionCount)
    {
        var bucketSize = optionCount <= 2 ? "2" : optionCount <= 5 ? "3-5" : optionCount <= 10 ? "6-10" : "11+";
        var bucket = $"{type.ToString().ToLowerInvariant()}:{bucketSize}";
        // Prefer calibration for this question type and option count; otherwise use the type default.
        if (_temperaturesByOptions.TryGetValue(bucket, out var temperature))
            return temperature;
        return _temperatures[(int)type];
    }

    private static double ClampTemperature(double temperature) => Math.Clamp(temperature, 0.5, 5.0);
}