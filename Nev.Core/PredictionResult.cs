// SPDX-License-Identifier: MIT
namespace Nev.Core;

/// <summary>
/// Represents the typed answers and token usage produced by a prediction.
/// </summary>
public sealed class PredictionResult
{
    internal PredictionResult(IReadOnlyDictionary<string, Answer> answers, int inputTokens)
    {
        Answers = answers;
        Usage = new PredictionUsage(inputTokens, 0);
    }

    /// <summary>
    /// Gets the predicted answers, keyed by their corresponding question IDs.
    /// </summary>
    public IReadOnlyDictionary<string, Answer> Answers { get; }
    
    /// <summary>
    /// Gets the question IDs contained in <see cref="Answers"/>.
    /// </summary>
    public IReadOnlyList<string> Ids => Answers.Keys.ToArray();

    public PredictionUsage Usage { get; }

    public int InputTokens => Usage.InputTokens;

    public int OutputTokens => Usage.OutputTokens;

    /// <summary>
    /// Gets the answer associated with the specified question ID.
    /// </summary>
    /// <param name="id">The question ID.</param>
    /// <returns>The corresponding predicted answer.</returns>
    /// <exception cref="KeyNotFoundException">
    /// No answer exists for <paramref name="id"/>.
    /// </exception>
    public Answer this[string id] => Answers[id];
}