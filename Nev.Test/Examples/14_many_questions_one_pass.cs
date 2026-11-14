using Nev.Core;

[TestFixture]
public sealed class ManyQuestionsOnePassExampleTests : ExampleFixture
{
    [Test]
    public void Multiple_questions_share_one_prediction_and_usage_is_the_batch_total()
    {
        var questions = new QuestionSet
        {
            ["department"] = Question.Choice("Which team?", "billing", "technical", "sales", "other"),
            ["issue_type"] = Question.Choice("What is the issue?", "duplicate charge", "outage", "login", "other"),
            ["severity"] = Question.Choice("How serious?", "cosmetic", "costly", "blocking"),
            ["urgency"] = Question.Score("How urgent?", "not urgent", "soon", "critical"),
            ["sentiment"] = Question.Score("How negative is the tone?", "positive", "neutral", "negative"),
            ["churn"] = Question.Noul("Does the sender threaten to leave?"),
            ["refund"] = Question.Noul("Does the sender request a refund?"),
        };
        const string state = "We were charged twice, production is blocked, and we need the duplicate refunded today or we will leave.";

        var combined = Model.Predict(state, questions);
        var splitTokens = questions.Sum(entry => Model.Predict(state, new QuestionSet { [entry.Key] = entry.Value }).InputTokens);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(combined.Ids, Is.EqualTo(questions.Ids));
            Assert.That(combined["department"].AsChoice().Choice, Is.EqualTo("billing"));
            Assert.That(combined["issue_type"].AsChoice().Choice, Is.EqualTo("duplicate charge"));
            Assert.That(combined["severity"].AsChoice().Choice, Is.EqualTo("blocking"));
            Assert.That(combined["urgency"].AsScore().Score, Is.GreaterThan(1));
            Assert.That(combined["sentiment"].AsScore().Score, Is.GreaterThan(1));
            Assert.That(combined["churn"].AsNoul().Probability, Is.InRange(0.5, 1));
            Assert.That(combined["refund"].AsNoul().Probability, Is.InRange(0.5, 1));
            Assert.That(combined.InputTokens, Is.EqualTo(splitTokens));
            Assert.That(combined.OutputTokens, Is.Zero);
        }
    }
}