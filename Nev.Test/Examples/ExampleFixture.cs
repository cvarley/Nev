using Nev.Core;

public abstract class ExampleFixture
{
    protected DecisionModel Model => ExamplesSetUpFixture.Model;
}

[SetUpFixture]
public sealed class ExamplesSetUpFixture
{
    private static readonly Lazy<DecisionModel> _model = new(() => new DecisionModel());

    public static DecisionModel Model => _model.Value;

    [OneTimeTearDown]
    public void DisposeModel()
    {
        if (_model.IsValueCreated)
            _model.Value.Dispose();
    }
}