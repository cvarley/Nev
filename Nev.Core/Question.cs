// SPDX-License-Identifier: MIT

namespace Nev.Core;

/// <summary>
/// Base type for questions, with factories for choice, score, and yes/no questions.
/// </summary>
public abstract class Question
{
    protected Question(QuestionType type, string instructions)
    {
        ArgumentNullException.ThrowIfNull(instructions);
        Type = type;
        Instructions = instructions;
    }

    public QuestionType Type { get; }

    public string Instructions { get; }

    public static ChoiceQuestion Choice(string instructions, params string[] labels)
        => new(instructions, labels.Select(label => new KeyValuePair<string, string?>(label, null)));

    public static ChoiceQuestion Choice(string instructions, IEnumerable<KeyValuePair<string, string?>> options)
        => new(instructions, options);

    public static ChoiceQuestion Choice(string instructions, params (string Label, string? Description)[] options)
        => new(instructions, options.Select(option => new KeyValuePair<string, string?>(option.Label, option.Description)));

    public static ScoreQuestion Score(string instructions, params string[] levels)
        => new(instructions, levels);

    public static NoulQuestion Noul(string instructions, string? ifFalse = null, string? ifTrue = null)
        => new(instructions, ifFalse, ifTrue);
}