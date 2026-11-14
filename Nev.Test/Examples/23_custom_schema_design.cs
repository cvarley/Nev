using Nev.Core;

[TestFixture]
public sealed class CustomSchemaDesignExampleTests : ExampleFixture
{
    [Test]
    public void Custom_bug_triage_schema_returns_four_typed_decisions()
    {
        var result = Model.Predict(
            new Dictionary<string, object?>
            {
                ["message"] = "Saving an invoice with more than 200 lines crashes and corrupts the data. Steps: open the invoice, add a line, press Save. Reproduced three times.",
            },
            new QuestionSet
            {
                ["category"] = Question.Choice("What kind of bug is this?", ("crash", "crash, exception or freeze"), ("performance", "slow or high resource use"), ("ui", "layout or usability"), ("data_loss", "lost, corrupted or missing data"), ("other", "none of the above")),
                ["severity"] = Question.Score("How severe is this bug?", "cosmetic", "annoying but workable", "blocks a workflow", "data loss or security impact"),
                ["reproducible"] = Question.Noul("Does the report give reproducible steps?"),
                ["data_loss_risk"] = Question.Noul("Does the report suggest data may be lost or corrupted?"),
            });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result["category"].AsChoice().Choice, Is.AnyOf("crash", "data_loss"));
            Assert.That(result["severity"].AsScore().Score, Is.InRange(1.5, 3));
            Assert.That(result["reproducible"].AsNoul().Probability, Is.InRange(0.5, 1));
            Assert.That(result["data_loss_risk"].AsNoul().Probability, Is.InRange(0.5, 1));
        }
    }
}