using Nev.Core;

[TestFixture]
public sealed class HighCardinalityChoiceExampleTests : ExampleFixture
{
    [TestCase(20)]
    [TestCase(77)]
    public void Choice_question_returns_a_distribution_for_many_options(int optionCount)
    {
        var options = Enumerable.Range(0, optionCount)
            .Select(index => new KeyValuePair<string, string?>("intent_" + index, "customer asks about intent " + index));
        var questions = new QuestionSet
        {
            ["intent"] = Question.Choice("Which intent best matches the message?", options),
        };

        var result = Model.Predict("My card was swallowed by the ATM this morning.", questions);
        var answer = result["intent"].AsChoice();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(answer.Probabilities, Has.Count.EqualTo(optionCount));
            Assert.That(answer.Probabilities.Sum(option => option.Value), Is.EqualTo(1).Within(0.001));
            Assert.That(answer.Choice, Is.EqualTo(answer.Probabilities.MaxBy(option => option.Value).Key));
        }
    }
}