using Nev.Core;

[TestFixture]
public sealed class TypedDecisionsWorkflowExampleTests
{
    [Test]
    public void Invoice_processing_question_ids_select_typed_decision_checkpoint()
    {
        string[] ids = ["disposition", "matches_order", "duplicate", "discrepancy_severity", "urgency"];

        Assert.That(ModelRouter.SelectModel(null, ids), Is.EqualTo(ModelCheckpoint.TypedDecision));
    }
}