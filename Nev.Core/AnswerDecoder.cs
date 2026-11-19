// SPDX-License-Identifier: MIT
namespace Nev.Core;

/// <summary>
/// Converts ONNX logits into typed answers with calibrated probabilities and confidence values.
/// </summary>
internal static class AnswerDecoder
{
    public static Answer Decode(Question question, float[] logits, double temperature, float[] actionLogits)
    {
        var probabilities = Softmax(logits, temperature);
        var actionProbability = RoundToFourDecimalPlaces(Softmax(actionLogits, 1.0)[0]);
        var answerConfidence = RoundToFourDecimalPlaces(probabilities.Max());
        var confidence = question.Type == QuestionType.Noul
            ? RoundToFourDecimalPlaces(Math.Max(probabilities[1], 1 - probabilities[1]))
            : RoundToFourDecimalPlaces(EntropyConfidence(probabilities));

        return question switch
        {
            ChoiceQuestion choice => new ChoiceAnswer(
                choice.Options[ArgMax(probabilities)].Key,
                choice.Options.Select((option, index) => new KeyValuePair<string, double>(option.Key, RoundToFourDecimalPlaces(probabilities[index]))).ToArray(),
                confidence,
                answerConfidence,
                actionProbability),
            ScoreQuestion score => new ScoreAnswer(
                RoundToFourDecimalPlaces(probabilities.Select((probability, index) => probability * index).Sum()),
                ArgMax(probabilities),
                probabilities.Select(RoundToFourDecimalPlaces).ToArray(),
                score.Levels,
                confidence,
                answerConfidence,
                actionProbability),
            NoulQuestion => new NoulAnswer(
                RoundToFourDecimalPlaces(probabilities[1]),
                confidence,
                answerConfidence,
                actionProbability),
            _ => throw new ArgumentException("Unknown question type.", nameof(question)),
        };
    }

    /// <summary>
    /// Converts logits into a normalised probability distribution using the softmax function.
    /// Subtracts the maximum scaled logit before exponentiation for numerical stability.
    /// </summary>
    /// <param name="logits">The unnormalised scores for the possible outcomes.</param>
    /// <param name="temperature">The value used to scale the logits.</param>
    /// <returns>The normalised probability for each outcome.</returns>
    private static double[] Softmax(float[] logits, double temperature)
    {
        var maximum = logits.Max(value => value / temperature);
        var values = logits.Select(value => Math.Exp(value / temperature - maximum)).ToArray();
        var total = values.Sum();
        return values.Select(value => value / total).ToArray();
    }

    /// <summary>
    /// Converts the normalised entropy of a probability distribution into a confidence score.
    /// Lower entropy produces higher confidence; distributions with fewer than two values return 1.
    /// </summary>
    /// <param name="probabilities">The probabilities for the possible outcomes.</param>
    /// <returns>A confidence score between 0 and 1, inclusive.</returns>
    private static double EntropyConfidence(double[] probabilities)
    {
        if (probabilities.Length < 2)
            return 1;
        var entropy = probabilities.Sum(value => -value * Math.Log(Math.Clamp(value, 1e-12, 1)));
        return Math.Clamp(1 - entropy / Math.Log(probabilities.Length), 0, 1);
    }

    /// <summary>
    /// Returns the index of the largest value in the array.
    /// If multiple values are equal to the maximum, returns the first matching index.
    /// </summary>
    /// <param name="values">The values to compare.</param>
    /// <returns>The index of the first largest value.</returns>
    private static int ArgMax(double[] values)
    {
        var best = 0;
        for (var index = 1; index < values.Length; index++)
            if (values[index] > values[best])
                best = index;
        return best;
    }

    private static double RoundToFourDecimalPlaces(double value) => Math.Round(value, 4, MidpointRounding.ToEven);
}