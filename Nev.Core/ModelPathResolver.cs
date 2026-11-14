// SPDX-License-Identifier: MIT

namespace Nev.Core;

public class ModelPathResolver
{
    private readonly string _modelDirectory;

    /// <summary>
    /// Resolves the path of the Model directory. Starts at the running apps BaseDirectory location and works it's way up the parents.
    /// Searching for a directry that contains a the path "...\Model\{Checkpoint}\model.onnx". 
    /// </summary>
    /// <param name="appBaseDirectory">Optional application base directory used to find the Model folder.</param>
    public ModelPathResolver(string modelType, string? appBaseDirectory = null)
    {
        _modelDirectory = FindModelDirectory(modelType, appBaseDirectory ?? string.Empty);
    }

    /// <summary>
    /// Where RL = "Reinforcement Learning". rl_agent_config.json stores model-specific runtime settings. 
    /// In DecisionModel.cs it reads:
    /// max_len and head_max_len, which control sequence construction, temperature defaults by question type, 
    /// and optional temperature_by_options overrides by question type and number of choices. 
    /// These values calibrate the logits before they’re converted to probabilities.
    /// </summary>
    public string GetRLCongigPath() => Path.Combine(_modelDirectory, "rl_agent_config.json");
    public string GetModelPath() => Path.Combine(_modelDirectory, "model.onnx");
    public string GetTokenizerPath() => Path.Combine(_modelDirectory, "tokenizer", "tokenizer.json");
    public string GetTokenizerConfigPath() => Path.Combine(_modelDirectory, "tokenizer", "tokenizer_config.json");

    private static string FindModelDirectory(string checkpoint, string appBaseDirectory)
    {
        var hasAppBaseOverride = !string.IsNullOrEmpty(appBaseDirectory);
        var pathName = hasAppBaseOverride ? appBaseDirectory : AppContext.BaseDirectory;
        var current = new DirectoryInfo(pathName);

        while (current is not null)
        {
            var outputCandidate = Path.Combine(current.FullName, "Model", checkpoint);
            if (File.Exists(Path.Combine(outputCandidate, "model.onnx")))
                return outputCandidate;

            if (!hasAppBaseOverride)
            {
                var sourceCandidate = Path.Combine(current.FullName, "Nev.Core", "Model", checkpoint);
                if (File.Exists(Path.Combine(sourceCandidate, "model.onnx")))
                    return sourceCandidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException($"Could not find the {checkpoint} checkpoint in {pathName}.");
    }

}
