using Nev.Core;

[TestFixture]
public sealed class RoutingWithoutRunningExampleTests
{
    [Test]
    public void Model_router_selects_english_or_multilingual_by_script()
    {
        var cases = new[]
        {
            ("We were charged twice for March and need a refund.", ModelCheckpoint.English),
            ("मुझसे दो बार शुल्क लिया गया, कृपया पैसे वापस करें।", ModelCheckpoint.Multilingual),
        };

        foreach (var (state, expected) in cases)
            Assert.That(ModelRouter.SelectModel(state), Is.EqualTo(expected));
    }
}