// SPDX-License-Identifier: MIT

using System.Text;
using System.Text.RegularExpressions;

namespace Nev.Core;

public partial class ModelRouter
{
    public static string SelectModel(object? input) => SelectModel(input, []);

    public static string SelectModel(object? input, IEnumerable<string> questionIds)
    {
        ArgumentNullException.ThrowIfNull(questionIds);

        // Match typed-decision workflows before falling back to language-based routing.
        if (IsTypedWorkflow(questionIds))
            return ModelCheckpoint.TypedDecision;

        if (input is null)
            return ModelCheckpoint.English;

        var text = JsonInputFormatter.State(input);

        // Non-Latin scripts
        foreach (var rune in text.EnumerateRunes())
        {
            if (!Rune.IsLetter(rune))
                continue;

            var c = rune.Value;

            if (c > 0x024F) /// 0x024F used as a Unicode code-point threshold (Very crude but fast)
                return ModelCheckpoint.Multilingual;
        }

        var language = Detect(text);
        if (language is not null && language != "en")
            return ModelCheckpoint.Multilingual;

        return ModelCheckpoint.English;
    }
    
    //Result of CoPilot prompt "Find me minimally shared letter combinations for languages that use the Latin character set."
    private static readonly Dictionary<string, string[]> Words = new()
    {
        ["en"] = ["the", "and", "that", "this", "with", "you", "have", "for", "what", "are", "from"],
        ["fr"] = ["les", "des", "une", "est", "dans", "pour", "avec", "vous", "pas", "que"],
        ["de"] = ["der", "die", "das", "und", "ist", "mit", "nicht", "ein", "eine", "ich"],
        ["es"] = ["los", "las", "una", "que", "por", "para", "con", "está", "pero", "como"],
        ["it"] = ["che", "gli", "della", "una", "per", "con", "sono", "non", "questo"],
        ["pt"] = ["que", "uma", "não", "com", "para", "por", "está", "são", "dos", "das"],
        ["nl"] = ["het", "een", "van", "dat", "niet", "met", "voor", "zijn", "maar"]        
    };

    public static string? Detect(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var tokens = WordTokenRegex().Matches(input.ToLowerInvariant()).Select(m => m.Value).ToArray();

        // Short inputs do not provide enough signal for this word-list detector.
        if (tokens.Length < 4)
            return null;

        var scores = Words.ToDictionary(
            language => language.Key,
            language => tokens.Count(
                token => language.Value.Contains(token))
        );

        var ranked = scores.OrderByDescending(x => x.Value).ToArray();

        // Require at least two distinctive word matches
        if (ranked[0].Value < 2)
            return null;

        // Require a meaningful lead over the runner-up
        if (ranked[0].Value - ranked[1].Value < 2)
            return null;

        return ranked[0].Key;
    }

    /// <summary>
    /// Creates a regular expression that matches contiguous sequences of Unicode letters.
    /// Used to extract word tokens for language detection.
    /// unctuation, spaces, and digits split the matches. For example, "co-op 42" yields "co" and "op"; it doesn’t return "co-op" as one token.
    /// </summary>
    /// <returns>The generated Unicode-letter matching regular expression.</returns>
    [GeneratedRegex(@"[\p{L}]+")]
    private static partial Regex WordTokenRegex();

    // Exact question-ID sets supported by the TypedDecision model; order is irrelevant.
    private static readonly HashSet<string>[] TypedWorkflows =
    [
        ["action", "category", "churn_risk", "needs_human", "urgency"],
        ["discrepancy_severity", "disposition", "duplicate", "matches_order", "urgency"],
        ["credential_compromise", "disposition", "severity", "true_positive", "urgency"],
        ["action", "needs_review", "outcome", "risk", "urgency"]
    ];

    private static bool IsTypedWorkflow(IEnumerable<string> questionIds)
    {
        var ids = questionIds.ToHashSet(StringComparer.Ordinal);
        return TypedWorkflows.Any(workflow => workflow.SetEquals(ids));
    }
}