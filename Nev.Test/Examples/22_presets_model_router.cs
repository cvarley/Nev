using Nev.Core;

[TestFixture]
public sealed class PresetsModelRouterExampleTests : ExampleFixture
{
    [Test]
    public void Model_tier_policy_can_be_applied_to_typed_answers()
    {
        var result = Model.Predict(
            new Dictionary<string, object?> { ["request"] = "Should I sue my employer for withholding my paycheck?" },
            new QuestionSet
            {
                ["difficulty"] = Question.Score("How difficult is this request?", "trivial", "moderate", "hard", "expert"),
                ["domain"] = Question.Choice("What domain is this?", "legal", "technical", "general"),
                ["needs_tools"] = Question.Noul("Does answering require external data or tools?"),
                ["is_sensitive"] = Question.Noul("Does the request involve sensitive legal, financial, medical, or safety matters?"),
            });
        var difficulty = result["difficulty"].AsScore().Score;
        var isSensitive = result["is_sensitive"].AsNoul().Probability;
        var needsTools = result["needs_tools"].AsNoul().Probability;
        var tier = isSensitive >= 0.5
            ? "frontier"
            : needsTools >= 0.5
                ? "frontier+tools"
                : difficulty >= 1.5 ? "frontier" : "small";

        Assert.That(result["domain"].AsChoice().Choice, Is.EqualTo("legal"));
        Assert.That(isSensitive, Is.GreaterThanOrEqualTo(0.5));
        Assert.That(tier, Is.EqualTo("frontier"));
    }
}