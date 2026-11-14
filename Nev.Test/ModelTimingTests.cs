using System.Diagnostics;
using Nev.Core;

public class ModelTimingTests
{
    [Test]
    public void Check_And_Writes_Timings()
    {
            using var model = new DecisionModel();
            var sw = new Stopwatch();

        for (int i = 0; i <10; i++){ 

            sw.Start();
            var state = new Dictionary<string, object?>
            {
                ["body"] = "We were charged twice for March. Please refund the duplicate or we will cancel.",
            };
            var questions = new QuestionSet
            {
                ["department"] = Question.Choice(
                    "Which department should handle this?",
                    new Dictionary<string, string?>
                    {
                        ["billing"] = "invoices, payments, refunds",
                        ["technical"] = "bugs, outages, system errors",
                        ["other"] = "everything else",
                    }),
                ["urgency"] = Question.Score("How urgent is this request?", "not urgent", "soon", "blocking"),
                ["churn_risk"] = Question.Noul("Does the user threaten to cancel or leave?"),
            };

            var result = model.Predict(state, questions);
            var department = result["department"].AsChoice();
            var urgency = result["urgency"].AsScore();
            var churnRisk = result["churn_risk"].AsNoul();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Ids, Is.EqualTo(["department", "urgency", "churn_risk"]));
                Assert.That(department.Choice, Is.AnyOf("billing", "technical", "other"));
                Assert.That(department.Probabilities.Sum(option => option.Value), Is.EqualTo(1).Within(0.001));
                Assert.That(urgency.Probabilities, Has.Count.EqualTo(3));
                Assert.That(urgency.Score, Is.InRange(0, 2));
                Assert.That(urgency.Probabilities.Sum(), Is.EqualTo(1).Within(0.001));
                Assert.That(churnRisk.Probability, Is.InRange(0, 1));
                Assert.That(churnRisk.Value, Is.EqualTo(churnRisk.Probability >= 0.5));
                Assert.That(result.InputTokens, Is.GreaterThan(0));
                Assert.That(result.OutputTokens, Is.Zero);
            }
            Debug.WriteLine($"Elapsed: {sw.ElapsedMilliseconds}");
            sw.Reset();
        }
    }
}