// SPDX-License-Identifier: MIT
namespace Nev.Core;

public sealed class PredictionUsage
{
    internal PredictionUsage(int inputTokens, int outputTokens)
    {
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
    }

    public int InputTokens { get; }

    public int OutputTokens { get; }
}