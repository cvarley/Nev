using Nev.Core;

[TestFixture]
public sealed class LongDocumentExampleTests : ExampleFixture
{
    [Test]
    public void Long_state_is_bounded_by_the_model_sequence_limit()
    {
        var document = "CASE HEADER: invoice 4411 was charged twice. "
            + string.Concat(Enumerable.Repeat("Routine status update; no new issue was reported. ", 200))
            + "CLOSING NOTE: the customer threatens to cancel unless the duplicate is refunded.";
        var questions = new QuestionSet
        {
            ["duplicate"] = Question.Noul("Was invoice 4411 charged twice?"),
            ["cancel"] = Question.Noul("Does the customer threaten to cancel?"),
        };

        var result = Model.Predict(document, questions);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.InputTokens, Is.LessThanOrEqualTo(questions.Count * 512));
            Assert.That(result["duplicate"].AsNoul().Probability, Is.InRange(0.5, 1));
            Assert.That(result["cancel"].AsNoul().Probability, Is.InRange(0, 0.5)); //The cutomer DOES threaten to cancel, but NOT until the end of the document which is passed the end of the sequnce.
        }
    }
}