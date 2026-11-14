using Nev.Core;

public class ModelRouterTests
{
    [Test]
    public void SelectModel_routes_exact_typed_workflow_question_sets()
    {
        string[][] workflows =
        [
            ["action", "category", "churn_risk", "needs_human", "urgency"],
            ["discrepancy_severity", "disposition", "duplicate", "matches_order", "urgency"],
            ["credential_compromise", "disposition", "severity", "true_positive", "urgency"],
            ["action", "needs_review", "outcome", "risk", "urgency"],
        ];

        foreach (var workflow in workflows)
            Assert.That(ModelRouter.SelectModel(null, workflow), Is.EqualTo(ModelCheckpoint.TypedDecision));

        var nearMatch = workflows[0].Append("extra_id");
        Assert.That(
            ModelRouter.SelectModel("The customer and you have a problem with this account", nearMatch),
            Is.EqualTo(ModelCheckpoint.English));
    }

    [Test]
    public void SelectModel_detects_language_from_formatted_state()
    {
        var frenchState = new Dictionary<string, object?>
        {
            ["body"] = "Les clients sont dans une situation avec un problème.",
        };

        Assert.That(ModelRouter.SelectModel(frenchState), Is.EqualTo(ModelCheckpoint.Multilingual));
        Assert.That(ModelRouter.SelectModel(frenchState, []), Is.EqualTo(ModelCheckpoint.Multilingual));
        Assert.That(ModelRouter.SelectModel("The customer and you have a problem with this account"), Is.EqualTo(ModelCheckpoint.English));
    }
}