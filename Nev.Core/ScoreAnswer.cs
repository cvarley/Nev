// SPDX-License-Identifier: MIT

namespace Nev.Core;

/// <summary>
/// The expected score, most likely level, and probability distribution for a score question.
/// For example, with levels <c>minor inconvenience</c>, <c>degraded but usable</c>,
/// <c>major outage</c>, and <c>service unavailable</c>, an illustrative result could be
/// <c>Score = 2.69</c>, <c>MostLikelyLevel = 3</c>, and <c>Probabilities = [0.01, 0.05, 0.18, 0.76]</c>.
/// </summary>
public sealed class ScoreAnswer : Answer
{
    internal ScoreAnswer(double score, int mostLikelyLevel, IReadOnlyList<double> probabilities,
        IReadOnlyList<string> legend, double confidence, double answerConfidence, double actProbability)
        : base(QuestionType.Score, confidence, answerConfidence, actProbability)
    {
        Score = score;
        MostLikelyLevel = mostLikelyLevel;
        Probabilities = probabilities;
        Legend = legend;
    }

    public double Score { get; }

    public int MostLikelyLevel { get; }

    public IReadOnlyList<double> Probabilities { get; }

    public IReadOnlyList<string> Legend { get; }
}