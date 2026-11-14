using Nev.Core;

[TestFixture]
public sealed class ConfidenceGatingExampleTests : ExampleFixture
{
    [Test]
    public void Application_can_apply_a_threshold_to_answer_confidence()
    {
        const double threshold = 0.85;
        var result = Model.Predict(
            "Hi, I need some help with my account.",
            new QuestionSet
            {
                ["department"] = Question.Choice("Which team should handle this?", "billing", "technical", "sales", "other"),
            });
        var answer = result["department"].AsChoice();
        var action = answer.AnswerConfidence >= threshold ? "automate" : "escalate";

        Assert.That(answer.Choice, Is.EqualTo("billing"));
        Assert.That(action, Is.EqualTo("escalate"));
    }
}