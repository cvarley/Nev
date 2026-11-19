// SPDX-License-Identifier: MIT
namespace Nev.Core;

/// <summary>
/// A question that asks the model to select one of several ordered labels and descriptions.
/// </summary>
public sealed class ChoiceQuestion : Question
{
    internal ChoiceQuestion(string instructions, IEnumerable<KeyValuePair<string, string?>> options)
        : base(QuestionType.Choice, instructions)
    {
        ArgumentNullException.ThrowIfNull(options);

        var items = options.ToArray();
        
        if (items.Length == 0)
            throw new ArgumentException("A choice question needs at least one option.", nameof(options));

        if (items.Any(option => string.IsNullOrEmpty(option.Key)))
            throw new ArgumentException("Choice labels must not be empty.", nameof(options));

        if (items.Select(option => option.Key).Distinct(StringComparer.Ordinal).Count() != items.Length)
            throw new ArgumentException("Choice labels must be unique.", nameof(options));

        Options = items;
    }
    
    /// <summary>
    /// Gets the options for this question. Each pair's key is the option label,
    /// and its nullable value is an optional description that provides context for the model.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, string?>> Options { get; }

}