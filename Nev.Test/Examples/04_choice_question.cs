using Nev.Core;

[TestFixture]
public sealed class ChoiceQuestionExampleTests : ExampleFixture
{
    [Test]
    public void Choice_question_returns_a_label_and_full_distribution()
    {
        var result = Model.Predict(
            new Dictionary<string, object?> { ["body"] = "Our invoice has a duplicate charge; please refund it." },
            new QuestionSet
            {
                ["department"] = Question.Choice(
                    "Which department should handle this request?",
                    ("billing", "invoices, payments, refunds"),
                    ("technical", "bugs, outages, system errors"),
                    ("sales", "pricing, new contracts"),
                    ("other", "everything else")),
            });
        var answer = result["department"].AsChoice();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(answer.Choice, Is.EqualTo("billing"));
            Assert.That(answer.Probabilities.Select(option => option.Key), Is.EqualTo(["billing", "technical", "sales", "other"]));
            Assert.That(answer.Probabilities.Sum(option => option.Value), Is.EqualTo(1).Within(0.001));
        }
    }
}