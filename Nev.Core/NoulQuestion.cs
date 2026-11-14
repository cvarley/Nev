// SPDX-License-Identifier: MIT

namespace Nev.Core;

/// <summary>
/// A yes/no question about whether a statement holds true, with optional wording for each outcome.
/// </summary>
public sealed class NoulQuestion : Question
{
    internal NoulQuestion(string instructions, string? ifFalse, string? ifTrue)
        : base(QuestionType.Noul, instructions)
    {
        IfFalse = ifFalse;
        IfTrue = ifTrue;
    }

    public string? IfFalse { get; }

    public string? IfTrue { get; }
}