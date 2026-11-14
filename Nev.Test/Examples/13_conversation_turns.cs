using Nev.Core;

[TestFixture]
public sealed class ConversationTurnsExampleTests : ExampleFixture
{
    [Test]
    public void Conversation_turn_list_can_be_used_as_state()
    {
        var turns = new[]
        {
            new Dictionary<string, string> { ["role"] = "user", ["content"] = "This is the third time I have contacted you." },
            new Dictionary<string, string> { ["role"] = "agent", ["content"] = "I am sorry for the delay." },
            new Dictionary<string, string> { ["role"] = "user", ["content"] = "I want a full refund today or I am cancelling." },
        };
        var result = Model.Predict(turns, new QuestionSet
        {
            ["upset"] = Question.Noul("Is the customer upset or frustrated?"),
            ["refund"] = Question.Noul("Does the customer ask for a refund?"),
            ["cancel"] = Question.Noul("Does the customer threaten to cancel?"),
            ["escalation"] = Question.Score("How heated is this exchange?", "calm", "mild friction", "frustrated", "angry or threatening"),
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result["upset"].AsNoul().Probability, Is.InRange(0.5, 1));
            Assert.That(result["refund"].AsNoul().Probability, Is.InRange(0.5, 1));
            Assert.That(result["cancel"].AsNoul().Probability, Is.InRange(0.5, 1));
            Assert.That(result["escalation"].AsScore().Score, Is.InRange(1.5, 3));
        }
    }
}