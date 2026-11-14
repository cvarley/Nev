using Nev.Core;

[TestFixture]
public sealed class ErrorHandlingExampleTests : ExampleFixture
{
    [Test]
    public void Empty_choice_options_are_rejected_before_inference()
    {
        Assert.That(
            () => Question.Choice("Pick one", Array.Empty<string>()),
            Throws.ArgumentException);
    }

    [Test]
    public void Option_blocks_that_exceed_the_sequence_limit_are_rejected()
    {
        var options = Enumerable.Range(0, 160)
            .Select(index => new KeyValuePair<string, string?>("option_" + index, "label number " + index));
        var questions = new QuestionSet
        {
            ["choice"] = Question.Choice("Pick one label", options),
        };

        Assert.That(
            () => Model.Predict("A short state.", questions),
            Throws.ArgumentException);
    }
}