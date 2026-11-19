// SPDX-License-Identifier: MIT
using Microsoft.ML.OnnxRuntime;
using System.Collections.Concurrent;

namespace Nev.Core;

public sealed class SessionProvider : ISessionProvider
{
    private static readonly ConcurrentDictionary<string, Lazy<SessionProvider>> Providers = new(StringComparer.OrdinalIgnoreCase);
    private readonly InferenceSession _session;
    private readonly ModelPathResolver _pathResolver;

    private SessionProvider(string modelCheckpointName)
    {
        _pathResolver = new ModelPathResolver(modelCheckpointName);

        var modelPath = _pathResolver.GetModelPath();
        var externalDataPath = $"{modelPath}.data";
        if (!File.Exists(externalDataPath))
        {
            throw new FileNotFoundException(
                $"The {modelCheckpointName} checkpoint requires its 'model.onnx.data' file beside 'model.onnx'. " +
                $"Place it at '{externalDataPath}' and rebuild the application.",
                externalDataPath);
        }

        var options = new SessionOptions()
        {
            EnableCpuMemArena = true,
            ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
            IntraOpNumThreads = 4,
            InterOpNumThreads = 1,
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
            EnableMemoryPattern = true
        };
        _session = new InferenceSession(modelPath, options);
    }

    public static SessionProvider Create(string modelType) => Providers.GetOrAdd(
        modelType,
        static type => new Lazy<SessionProvider>(
            () => new SessionProvider(type),
            LazyThreadSafetyMode.ExecutionAndPublication)).Value;

    public InferenceSession GetSession()
    {
        return _session;
    }

    public string GetTokenizerPath() => _pathResolver.GetTokenizerPath();
    public string GetTokenizerConfigPath() => _pathResolver.GetTokenizerConfigPath();
    public string GetRLConfigPath() => _pathResolver.GetRLCongigPath();

}