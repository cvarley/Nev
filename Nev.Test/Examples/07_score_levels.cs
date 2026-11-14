using Nev.Core;

[TestFixture]
public sealed class ScoreLevelsExampleTests : ExampleFixture
{
    [Test]
    public void Score_rubric_granularity_controls_the_answer_scale()
    {
        var rubrics = new[]
        {
            new[] { "no rush", "normal", "blocking" },
            new[] { "no rush", "low", "normal", "high", "blocking" },
            new[] { "no rush", "very low", "low", "normal", "high", "very high", "blocking" },
        };

        foreach (var rubric in rubrics)
        {
            var result = Model.Predict(
                "Our production checkout is down and we are losing orders every minute.",
                new QuestionSet
                {
                    ["urgency"] = Question.Score("How urgent is this request?", rubric),
                });
            var answer = result["urgency"].AsScore();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.Legend, Is.EqualTo(rubric));
                Assert.That(answer.Score, Is.InRange((rubric.Length - 1) / 2.0, rubric.Length - 1));
                Assert.That(answer.Probabilities, Has.Count.EqualTo(rubric.Length));
            }
        }
    }
}