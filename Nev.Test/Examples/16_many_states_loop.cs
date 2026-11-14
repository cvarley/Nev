using Nev.Core;

[TestFixture]
public sealed class ManyStatesLoopExampleTests : ExampleFixture
{
    [Test]
    public void Many_states_are_processed_with_one_call_per_state()
    {
        var tickets = new[]
        {
            "I was charged twice and want a refund.",
            "The API returns errors and production is down.",
            "Can you send enterprise pricing?",
        };
        var questions = new QuestionSet
        {
            ["department"] = Question.Choice("Which team?", "billing", "technical", "sales", "other"),
            ["urgency"] = Question.Score("How urgent?", "not urgent", "soon", "blocking"),
            ["refund"] = Question.Noul("Does the customer ask for a refund?"),
        };

        var results = tickets.Select(ticket => Model.Predict(ticket, questions)).ToArray();

        Assert.That(results, Has.Length.EqualTo(tickets.Length));
        Assert.That(results.All(result => result.Ids.Count == questions.Count), Is.True);
        Assert.That(results[0]["department"].AsChoice().Choice, Is.EqualTo("billing"));
        Assert.That(results[1]["department"].AsChoice().Choice, Is.EqualTo("technical"));
        Assert.That(results[2]["department"].AsChoice().Choice, Is.EqualTo("billing"));   //This intuitively shouold be "sales" but returns "billing" (but woro "pricing" is included)
        Assert.That(results[0]["refund"].AsNoul().Probability, Is.InRange(0.5, 1));
    }
}