// SPDX-License-Identifier: MIT
using Microsoft.ML.OnnxRuntime;

namespace Nev.Core;

/// <summary>
/// Used by DecisionModel to locate and load the required model weights and create an Inference Session.
/// </summary>
public interface ISessionProvider
{
    public InferenceSession GetSession();
    public string GetTokenizerPath();
    public string GetTokenizerConfigPath();
    public string GetRLConfigPath();
}