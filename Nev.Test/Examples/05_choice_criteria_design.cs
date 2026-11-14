using Nev.Core;

[TestFixture]
public sealed class ChoiceCriteriaDesignExampleTests : ExampleFixture
{
    [Test]
    public void Choice_criteria_wording_is_passed_through_to_the_decision_head()
    {
        const string state = "Our production checkout returns 500 errors and customers cannot pay.";
        var variants = new[]
        {
            new Dictionary<string, string?>
            {
                ["billing"] = "money, billing, payments", 
                ["technical"] = "tech, bugs, errors", 
                ["other"] = "other",
            },
            new Dictionary<string, string?>
            {
                ["billing"] = "invoices, payments, refunds",
                ["technical"] = "bugs, outages, system errors",
                ["other"] = "everything else",
            },
        };

        foreach (var criteria in variants)
        {
            var result = Model.Predict(state, new QuestionSet
            {
                ["department"] = Question.Choice("Which department should handle this?", criteria),
            });
            var answer = result["department"].AsChoice();

            Assert.That(answer.Probabilities, Has.Count.EqualTo(criteria.Count));
            Assert.That(answer.Choice, Is.EqualTo("technical"));
        }
    }
}