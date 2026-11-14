using Nev.Core;

[TestFixture]
public sealed class JsonStateExampleTests : ExampleFixture
{
    [Test]
    public void Dictionary_state_is_serialized_and_can_be_queried()
    {
        var order = new Dictionary<string, object?>
        {
            ["order_id"] = "ORD-7781",
            ["payment_status"] = "captured",
            ["delivery_status"] = "delivered",
            ["refund_status"] = "requested",
            ["customer_note"] = "The keyboard arrived with a cracked case; I would like a refund.",
        };
        var result = Model.Predict(order, new QuestionSet
        {
            ["delivered"] = Question.Noul("Was this order delivered?"),
            ["refund_requested"] = Question.Noul("Has the customer asked for a refund?"),
            ["refund_state"] = Question.Choice("What is the refund state?", "none", "requested", "approved"),
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result["delivered"].AsNoul().Value, Is.True);
            Assert.That(result["refund_requested"].AsNoul().Probability, Is.InRange(0.5, 1));
            Assert.That(result["refund_state"].AsChoice().Choice, Is.EqualTo("requested"));
        }
    }
}