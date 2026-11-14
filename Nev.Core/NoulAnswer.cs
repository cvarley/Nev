// SPDX-License-Identifier: MIT

namespace Nev.Core;

/// <summary>
/// The probability that a yes/no statement is true and its corresponding boolean value.
/// </summary>
public sealed class NoulAnswer : Answer
{
    internal NoulAnswer(double probability, double confidence, double answerConfidence, double actProbability)
        : base(QuestionType.Noul, confidence, answerConfidence, actProbability)
    {
        Probability = probability;
    }

    public double Probability { get; }

    public bool Value => Probability >= 0.5;
}