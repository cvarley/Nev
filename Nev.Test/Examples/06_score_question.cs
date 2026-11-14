using Nev.Core;

[TestFixture]
public sealed class ScoreQuestionExampleTests : ExampleFixture
{
    [Test]
    public void Score_question_returns_expected_level_and_legend()
    {
        var result = Model.Predict(
            "Production checkout is down and we are losing orders every minute.",
            new QuestionSet
            {
                ["urgency"] = Question.Score("How urgent is this request?", "not urgent", "soon", "critical deadline or blocking issue"),
            });
        var answer = result["urgency"].AsScore();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(answer.MostLikelyLevel, Is.EqualTo(2));
            Assert.That(answer.Score, Is.InRange(1.5, 2));
            Assert.That(answer.Legend, Is.EqualTo(["not urgent", "soon", "critical deadline or blocking issue"]));
            Assert.That(answer.Probabilities.Sum(), Is.EqualTo(1).Within(0.001));
        }
    }
}