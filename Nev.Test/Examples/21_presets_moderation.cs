using Nev.Core;

[TestFixture]
public sealed class PresetsModerationExampleTests : ExampleFixture
{
    [Test]
    public void Moderation_style_question_set_returns_flags_and_ordered_severity()
    {
        var result = Model.Predict(
            new Dictionary<string, object?> { ["post"] = "Nobody asked for your opinion. You are useless." },
            new QuestionSet
            {
                ["toxic"] = Question.Noul("Is the post toxic or insulting?"),
                ["harassment"] = Question.Noul("Does the post target or harass someone?"),
                ["threat"] = Question.Noul("Does the post contain a threat?"),
                ["spam"] = Question.Noul("Is the post spam?"),
                ["severity"] = Question.Score("How severe is the content?", "none", "mild", "moderate", "severe"),
            });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result["toxic"].AsNoul().Probability, Is.InRange(0.5, 1));
            Assert.That(result["harassment"].AsNoul().Probability, Is.InRange(0.5, 1));
            Assert.That(result["threat"].AsNoul().Probability, Is.InRange(0, 0.5));
            Assert.That(result["spam"].AsNoul().Probability, Is.InRange(0, 0.5));
            Assert.That(result["severity"].AsScore().Score, Is.InRange(0, 3));
        }
    }
}