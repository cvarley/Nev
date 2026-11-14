// SPDX-License-Identifier: MIT

namespace Nev.Core;

/// <summary>
/// Provides identifiers for model checkpoint folders. Add identifiers for additional checkpoints
/// in another partial declaration within the Nev.Core namespace; each identifier must match a folder under <c>Model</c>.
/// </summary>
public static partial class ModelCheckpoint
{
    public static string English { get; } = nameof(English);
    public static string Multilingual { get; } = nameof(Multilingual);
    public static string TypedDecision { get; } = nameof(TypedDecision);
}