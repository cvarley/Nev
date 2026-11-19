// SPDX-License-Identifier: MIT
namespace Nev.Core;

/// <summary>
/// The selected label and probability distribution for a choice question.
/// </summary>
public sealed class ChoiceAnswer : Answer
{
    internal ChoiceAnswer(string choice, IReadOnlyList<KeyValuePair<string, double>> probabilities,
        double confidence, double answerConfidence, double actProbability)
        : base(QuestionType.Choice, confidence, answerConfidence, actProbability)
    {
        Choice = choice;
        Probabilities = probabilities;
    }

    public string Choice { get; }

    public IReadOnlyList<KeyValuePair<string, double>> Probabilities { get; }

    public double this[string label] => Probabilities.First(item => item.Key == label).Value;
}