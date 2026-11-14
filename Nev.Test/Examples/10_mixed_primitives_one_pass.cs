using Nev.Core;

[TestFixture]
public sealed class MixedPrimitivesOnePassExampleTests : ExampleFixture
{
    [Test]
    public void Choice_score_and_noul_are_answered_in_one_prediction()
    {
        var result = Model.Predict(
            "We were billed twice for March. Please refund the duplicate or we will cancel.",
            new QuestionSet
            {
                ["department"] = Question.Choice("Which department?", "billing", "technical", "other"),
                ["urgency"] = Question.Score("How urgent?", "not urgent", "soon", "blocking"),
                ["churn_risk"] = Question.Noul("Does the user threaten to cancel?"),
            });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Ids, Is.EqualTo(["department", "urgency", "churn_risk"]));
            Assert.That(result["department"].AsChoice().Choice, Is.EqualTo("billing"));
            Assert.That(result["urgency"].AsScore().Score, Is.InRange(1, 2));
            Assert.That(result["churn_risk"].AsNoul().Probability, Is.InRange(0.5, 1));
            Assert.That(result.OutputTokens, Is.Zero);
        }
    }
}