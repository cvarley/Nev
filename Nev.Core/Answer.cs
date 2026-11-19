// SPDX-License-Identifier: MIT
namespace Nev.Core;

/// <summary>
/// Base class for a typed answer returned by the model, with confidence and action estimates.
/// </summary>
public abstract class Answer(QuestionType type, double confidence, double answerConfidence, double actProbability)
{
    /// <summary>The kind of question this answer corresponds to.</summary>
    public QuestionType Type { get; } = type;

    /// <summary>
    /// Measures how concentrated the answer distribution is; it is not the probability that the answer is correct.
    /// For choice and score answers, higher values indicate a more concentrated distribution. For a Noul answer,
    /// this is the larger of the true and false probabilities; for example, P(true) = 0.8 gives confidence 0.8.
    /// </summary>
    public double Confidence { get; } = confidence;

    /// <summary>
    /// The highest probability assigned to an answer. For example, a choice distribution of [0.8, 0.1, 0.1]
    /// has answer confidence 0.8.
    /// </summary>
    public double AnswerConfidence { get; } = answerConfidence;

    /// <summary>
    /// The action head's estimate that the answer can be acted on rather than escalated. For example, 0.9
    /// represents an estimated 90% probability of acting directly.
    /// </summary>
    public double ActProbability { get; } = actProbability;

#region "Concrete sub-type castings."

    public ChoiceAnswer AsChoice() => this as ChoiceAnswer
        ?? throw new InvalidOperationException($"Answer is {Type}, not Choice.");

    public ScoreAnswer AsScore() => this as ScoreAnswer
        ?? throw new InvalidOperationException($"Answer is {Type}, not Score.");

    public NoulAnswer AsNoul() => this as NoulAnswer
        ?? throw new InvalidOperationException($"Answer is {Type}, not Noul.");

#endregion

}