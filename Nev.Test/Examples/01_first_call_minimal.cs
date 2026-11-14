using System.Diagnostics;
using Nev.Core;

[TestFixture]
public sealed class FirstCallMinimalExampleTests : ExampleFixture
{
    [Test]
    public void Predict_returns_probability_for_a_yes_no_question()
    {
        var result = Model.Predict(
            new Dictionary<string, object?>
            {
                ["body"] = "We were billed twice for March. Please refund the duplicate.",
            },
            new QuestionSet
            {
                ["refund_requested"] = Question.Noul("Does the user explicitly request a refund?"),
            });
        var answer = result["refund_requested"].AsNoul();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(answer.Value, Is.True);// support for "Yes"
            Assert.That(answer.Probability, Is.InRange(0.5, 1)); 
            Assert.That(result.OutputTokens, Is.Zero);
        }
    }
}