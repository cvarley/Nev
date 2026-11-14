// SPDX-License-Identifier: MIT

namespace Nev.Core;

/// <summary>
/// A question scored against ordered rubric levels, from lowest to highest; for example, incident severity can range from
/// <c>minor inconvenience</c> and <c>degraded but usable</c> to <c>major outage</c> and <c>service unavailable</c>.
/// </summary>
public sealed class ScoreQuestion : Question
{
    internal ScoreQuestion(string instructions, IEnumerable<string> levels)
        : base(QuestionType.Score, instructions)
    {
        ArgumentNullException.ThrowIfNull(levels);
        Levels = levels.ToArray();
        if (Levels.Count == 0)
            throw new ArgumentException("A score question needs at least one level.", nameof(levels));
    }

    public IReadOnlyList<string> Levels { get; }
}