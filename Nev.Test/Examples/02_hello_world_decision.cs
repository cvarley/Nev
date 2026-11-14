using Nev.Core;

[TestFixture]
public sealed class HelloWorldDecisionExampleTests : ExampleFixture
{
    [Test]
    public void Predict_returns_a_typed_department_decision()
    {
        var result = Model.Predict(
            "We were billed twice for March. Please refund the duplicate.",
            new QuestionSet
            {
                ["department"] = Question.Choice(
                    "Which department should handle this request?",
                    ("billing", "invoices, payments, refunds"),
                    ("technical", "bugs, outages, system errors"),
                    ("other", "everything else")),
            });
        var answer = result["department"].AsChoice();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(answer.Choice, Is.EqualTo("billing"));
            Assert.That(answer.Probabilities.Sum(option => option.Value), Is.EqualTo(1).Within(0.001));
        }
    }
}