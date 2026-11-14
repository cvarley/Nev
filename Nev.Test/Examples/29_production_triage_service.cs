using Nev.Core;

[TestFixture]
public sealed class ProductionTriageServiceExampleTests : ExampleFixture
{
    [Test]
    public void Typed_triage_answers_can_be_mapped_to_a_structured_policy_decision()
    {
        var result = Model.Predict(
            new Dictionary<string, object?>
            {
                ["ticket_id"] = "TCK-1001",
                ["message"] = "We were billed twice and nobody has replied. We will cancel unless this is refunded today.",
            },
            new QuestionSet
            {
                ["intent"] = Question.Choice("What is the customer's intent?", "refund", "billing_question", "technical_help", "information", "cancellation", "other"),
                ["is_urgent"] = Question.Noul("Is immediate attention required?"),
                ["frustration"] = Question.Score("How frustrated is the customer?", "calm", "mild", "clearly frustrated", "very angry"),
                ["refund_requested"] = Question.Noul("Does the customer request a refund?"),
                ["churn_risk"] = Question.Noul("Does the customer threaten to leave?"),
            });
        var churn = result["churn_risk"].AsNoul().Probability;
        var intent = result["intent"].AsChoice().Choice;
        var decision = churn >= 0.5
            ? (Action: "escalate_to_human", Team: "retention")
            : intent is "refund" or "billing_question" || result["refund_requested"].AsNoul().Probability >= 0.5
                ? (Action: "route_to_team", Team: "billing")
                : (Action: "route_to_team", Team: "general_support");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Ids, Has.Count.EqualTo(5));
            Assert.That(intent, Is.AnyOf("refund", "cancellation"));
            Assert.That(result["is_urgent"].AsNoul().Probability, Is.InRange(0.5, 1));
            Assert.That(result["frustration"].AsScore().Score, Is.GreaterThan(1.5));
            Assert.That(result["refund_requested"].AsNoul().Probability, Is.InRange(0.5, 1));
            Assert.That(churn, Is.InRange(0.5, 1));
            Assert.That(decision.Action, Is.EqualTo("escalate_to_human"));
            Assert.That(decision.Team, Is.EqualTo("retention"));
        }
    }
}