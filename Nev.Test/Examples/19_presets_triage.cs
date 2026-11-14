using Nev.Core;

[TestFixture]
public sealed class PresetsTriageExampleTests : ExampleFixture
{
    [Test]
    public void Triage_question_set_returns_a_structured_triage_record()
    {
        var ticket = new Dictionary<string, object?>
        {
            ["message"] = "We have been charged twice for three months. Refund the duplicate charges immediately or we will cancel.",
        };
        var result = Model.Predict(ticket, new QuestionSet
        {
            ["intent"] = Question.Choice("What is the customer asking for?", "refund", "technical help", "information", "cancellation", "other"),
            ["is_urgent"] = Question.Noul("Does this need immediate attention?"),
            ["frustration"] = Question.Score("How frustrated is the customer?", "calm", "mildly annoyed", "clearly frustrated", "very angry"),
            ["refund_requested"] = Question.Noul("Does the customer request a refund?"),
            ["churn_risk"] = Question.Noul("Does the customer threaten to cancel or leave?"),
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result["intent"].AsChoice().Choice, Is.EqualTo("refund"));
            Assert.That(result["is_urgent"].AsNoul().Probability, Is.InRange(0.5, 1));
            Assert.That(result["frustration"].AsScore().Score, Is.InRange(1.5, 3));
            Assert.That(result["refund_requested"].AsNoul().Probability, Is.InRange(0.5, 1));
            Assert.That(result["churn_risk"].AsNoul().Probability, Is.InRange(0.5, 1));
        }
    }
}