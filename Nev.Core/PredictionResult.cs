// SPDX-License-Identifier: MIT

namespace Nev.Core;

public sealed class PredictionResult
{
    internal PredictionResult(IReadOnlyDictionary<string, Answer> answers, int inputTokens)
    {
        Answers = answers;
        Usage = new PredictionUsage(inputTokens, 0);
    }

    public IReadOnlyDictionary<string, Answer> Answers { get; }

    public IReadOnlyList<string> Ids => Answers.Keys.ToArray();

    public PredictionUsage Usage { get; }

    public int InputTokens => Usage.InputTokens;

    public int OutputTokens => Usage.OutputTokens;

    public Answer this[string id] => Answers[id];
}