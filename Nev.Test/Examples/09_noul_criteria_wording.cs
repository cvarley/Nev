using Nev.Core;

[TestFixture]
public sealed class NoulCriteriaWordingExampleTests : ExampleFixture
{
    [Test]
    public void Noul_supports_explicit_false_and_true_criteria()
    {
        var result = Model.Predict(
            "We were billed twice. Please refund the duplicate today.",
            new QuestionSet
            {
                ["refund"] = Question.Noul(
                    "Does the user want a refund?",
                    ifFalse: "the user is not asking for money back",
                    ifTrue: "the user is asking for money back"),
            });

        Assert.That(result["refund"].AsNoul().Probability, Is.InRange(0.5, 1));
    }
}