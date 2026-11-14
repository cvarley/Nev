using Nev.Core;

[TestFixture]
public sealed class PresetEmailPhishingExampleTests : ExampleFixture
{
    [Test]
    public void Email_state_can_be_triaged_with_a_caller_defined_question_set()
    {
        var email = new Dictionary<string, object?>
        {
            ["from"] = "security@example.test",
            ["subject"] = "Verify your account within 24 hours",
            ["body"] = "Confirm your password at the link or your account will be suspended.",
        };
        var result = Model.Predict(email, new QuestionSet
        {
            ["category"] = Question.Choice("What kind of email is this?", "phishing", "billing", "technical", "newsletter", "other"),
            ["is_spam"] = Question.Noul("Is this unsolicited spam?"),
            ["is_phishing"] = Question.Noul("Is this email a phishing attempt?"),
            ["urgency"] = Question.Score("How urgent is this email?", "not urgent", "soon", "immediate"),
            ["needs_reply"] = Question.Noul("Does the sender expect a reply?"),
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result["category"].AsChoice().Choice, Is.EqualTo("phishing"));
            Assert.That(result["is_spam"].AsNoul().Probability, Is.InRange(0.5, 1));
            Assert.That(result["is_phishing"].AsNoul().Probability, Is.InRange(0.5, 1));
            Assert.That(result["urgency"].AsScore().Score, Is.InRange(1, 2));
            Assert.That(result["needs_reply"].AsNoul().Probability, Is.InRange(0, 1));
        }
    }
}