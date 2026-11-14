using Nev.Core;

[TestFixture]
public sealed class BatchThroughputExampleTests : ExampleFixture
{
    [Test]
    public void Asking_more_questions_in_one_call_returns_all_decisions()
    {
        var questions = new QuestionSet
        {
            ["department"] = Question.Choice("Which department?", "billing", "technical", "sales", "other"),
            ["urgency"] = Question.Score("How urgent?", "not urgent", "soon", "critical"),
            ["refund"] = Question.Noul("Does the customer request a refund?"),
        };
        const string state = "We were charged twice and need the duplicate refunded today.";

        var result = Model.Predict(state, questions);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Answers.Keys, Is.EqualTo(questions.Ids));
            Assert.That(result["department"].AsChoice().Choice, Is.EqualTo("billing"));
            Assert.That(result["refund"].AsNoul().Probability, Is.InRange(0.5, 1));
            Assert.That(result.InputTokens, Is.GreaterThan(0));
            Assert.That(result.OutputTokens, Is.Zero);
        }
    }
}