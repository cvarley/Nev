// SPDX-License-Identifier: MIT

namespace Nev.Core;

internal static class SequenceBuilder
{
    private const int MaxOptionTokens = 48;
    private const int MinHeadTokens = 8;

    public static (int[] Ids, int[] Markers) Build(Question question, int[] stateIds, Func<string, int[]> encode,
        int clsId, int sepId, int maskId, int maxLen, int headMaxLen)
    {
        var instructions = Scrub(question.Instructions);
        var headIds = encode($"{TypeName(question.Type)} question: {instructions}").ToList();

        var optionFragments = RenderOptions(question).Select(option =>
        {
            var fragment = new List<int> { maskId };
            fragment.AddRange(encode(" " + Scrub(option)).Take(MaxOptionTokens));
            return fragment;
        }).ToArray();

        // Reserve space for the question after accounting for every option fragment.
        var optionBudget = headMaxLen - optionFragments.Sum(fragment => fragment.Count);
        if (optionBudget < 16)
        {
            // Trim option text before letting it consume the question's minimum budget.
            var perOption = Math.Max(4, (headMaxLen - 16) / Math.Max(1, optionFragments.Length));
            for (var index = 0; index < optionFragments.Length; index++)
                optionFragments[index] = optionFragments[index].Take(perOption).ToList();
            optionBudget = headMaxLen - optionFragments.Sum(fragment => fragment.Count);
        }

        headIds = headIds.Take(Math.Max(MinHeadTokens, optionBudget)).ToList();
        
        var ids = new List<int> { clsId };
        ids.AddRange(headIds);
        ids.Add(sepId);

        var markers = new List<int>(optionFragments.Length);
        foreach (var fragment in optionFragments)
        {
            // The model reads each option's logits from its mask-token position.
            markers.Add(ids.Count);
            ids.AddRange(fragment);
        }
        ids.Add(sepId);

        // Use the remaining context for state tokens, preserving the question and options first.
        var stateRoom = Math.Max(0, maxLen - ids.Count - 1);
        ids.AddRange(stateIds.Take(stateRoom));
        ids.Add(sepId);
        if (ids.Count > maxLen)
            ids = ids.Take(maxLen).ToList();

        markers.RemoveAll(marker => marker >= ids.Count);
        if (markers.Count != optionFragments.Length)
            throw new ArgumentException("The configured sequence limit leaves too few option markers.", nameof(question));

        return (ids.ToArray(), markers.ToArray());
    }

    private static IReadOnlyList<string> RenderOptions(Question question) => question switch
    {
        ChoiceQuestion choice => choice.Options.Select(option => string.IsNullOrEmpty(option.Value)
            ? option.Key
            : $"{option.Key}: {JsonInputFormatter.Criterion(option.Value)}").ToArray(),
        ScoreQuestion score => score.Levels.Select((level, index) => $"level {index}: {level}").ToArray(),
        NoulQuestion noul =>
        [
            "false: " + (string.IsNullOrEmpty(noul.IfFalse) ? "no, the statement does not hold true" : noul.IfFalse),
            "true: " + (string.IsNullOrEmpty(noul.IfTrue) ? "yes, the statement holds true" : noul.IfTrue),
        ],
        _ => throw new ArgumentException("Unknown question type.", nameof(question)),
    };

    private static string TypeName(QuestionType type) => type switch
    {
        QuestionType.Choice => "choice",
        QuestionType.Score => "score",
        QuestionType.Noul => "noul",
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    private static string Scrub(string value) => value.Replace("[MASK]", " ", StringComparison.Ordinal);
}