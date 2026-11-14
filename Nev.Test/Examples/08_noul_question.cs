using Nev.Core;

[TestFixture]
public sealed class NoulQuestionExampleTests : ExampleFixture
{
    [Test]
    public void Noul_returns_probability_and_boolean_value()
    {
        var result = Model.Predict(
            "We were billed twice. Please refund the duplicate today.",
            new QuestionSet
            {
                ["refund_requested"] = Question.Noul("Does the user explicitly request a refund?"),
            });
        var answer = result["refund_requested"].AsNoul();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(answer.Probability, Is.InRange(0.5, 1));
            Assert.That(answer.Value, Is.True);
        }
    }
}