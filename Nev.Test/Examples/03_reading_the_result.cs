using Nev.Core;

[TestFixture]
public sealed class ReadingTheResultExampleTests : ExampleFixture
{
    [Test]
    public void Prediction_result_contains_typed_answers_and_usage()
    {
        var result = Model.Predict(
            new Dictionary<string, object?>
            {
                ["body"] = "We were billed twice. Please refund the duplicate or we will cancel.",
            },
            new QuestionSet
            {
                ["department"] = Question.Choice("Which team should handle this?", "billing", "technical", "other"),
                ["urgency"] = Question.Score("How urgent is this?", "not urgent", "soon", "blocking"),
                ["refund"] = Question.Noul("Does the user ask for a refund?"),
            });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Ids, Is.EqualTo(["department", "urgency", "refund"]));
          
            Assert.That(result["department"].AsChoice().Probabilities, Has.Count.EqualTo(3));
            Assert.That(result["department"].AsChoice().Choice, Is.EqualTo("billing"));
            
            Assert.That(result["urgency"].AsScore().Legend, Is.EqualTo(["not urgent", "soon", "blocking"]));
            Assert.That(result["urgency"].AsScore().MostLikelyLevel, Is.EqualTo(2)); //"blocking"
          
            Assert.That(result["refund"].AsNoul().Probability, Is.InRange(0.5, 1));
          
            Assert.That(result.InputTokens, Is.GreaterThan(0));
            Assert.That(result.OutputTokens, Is.Zero);
        }
    }
}