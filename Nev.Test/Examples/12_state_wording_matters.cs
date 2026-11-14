using Nev.Core;

[TestFixture]
public sealed class StateWordingMattersExampleTests : ExampleFixture
{
    [Test]
    public void Same_facts_can_be_passed_as_text_email_or_json()
    {
        object[] states =
        [
            "Support ticket: invoice 4411 was billed twice. The customer asks for a refund and threatens to cancel.",
            new Dictionary<string, object?>
            {
                ["subject"] = "Duplicate charge on invoice 4411",
                ["body"] = "Please refund the duplicate charge today, otherwise we will cancel our plan.",
            },
            new Dictionary<string, object?>
            {
                ["duplicate_charge"] = true,
                ["refund_requested"] = true,
                ["account_action_if_unresolved"] = "cancel plan",
            },
        ];
        var questions = new QuestionSet
        {
            ["refund"] = Question.Noul("Does the customer explicitly request a refund?"),
            ["churn"] = Question.Noul("Does the customer threaten to cancel or leave?"),
        };

        foreach (var state in states)
        {
            var result = Model.Predict(state, questions);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result["refund"].AsNoul().Probability, Is.InRange(0.5, 1));
                Assert.That(result["churn"].AsNoul().Probability, Is.InRange(0.5, 1));
            }
        }
    }
}