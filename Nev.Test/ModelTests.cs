using Nev.Core;
using Microsoft.ML.OnnxRuntime.Tensors;

public class ModelTests
{
    [Test]
    public void ModelPathResolver_should_prefer_model_assets_in_application_output()
    {
        var resolver = new ModelPathResolver(ModelCheckpoint.English);

        Assert.That(
            resolver.GetModelPath(),
            Is.EqualTo(Path.Combine(AppContext.BaseDirectory, "Model", ModelCheckpoint.English, "model.onnx")));
    }

    [Test]
    public void SessionProvider_should_share_one_provider_when_created_concurrently()
    {
        var providers = new SessionProvider[8];

        Parallel.For(0, providers.Length, index =>
            providers[index] = SessionProvider.Create(ModelCheckpoint.English));

        Assert.That(providers.All(provider => ReferenceEquals(provider, providers[0])), Is.True);
        Assert.That(providers.Select(provider => provider.GetSession()).Distinct().Count(), Is.EqualTo(1));
    }

    [Test]
    public void Model_should_expose_model_inputs_and_encodes_text()
    {
        using var model = new DecisionModel();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.InputMetadata.Keys, Is.EquivalentTo([
                    "input_ids", "attention_mask", "marker_pos", "marker_mask", "qtype"
                ]));

            Assert.That(model.OutputMetadata.Keys, Is.EquivalentTo([
                    "logits", "act_logits"
                ]));
                
            Assert.That(model.EncodeAsTokenIDs("We were charged twice."), Is.EqualTo([1231, 497, 6636, 7019, 15]));
        }
    }

    [Test]
    public void Run_should_return_logits_for_model_tensors()
    {
        using var model = new DecisionModel();
        var inputIds = new DenseTensor<long>([1, 4]);
        inputIds[0, 0] = 50281;
        inputIds[0, 1] = 50284;
        inputIds[0, 2] = 50284;
        inputIds[0, 3] = 50282;

        var attentionMask = new DenseTensor<long>([1, 4]);
        for (var index = 0; index < 4; index++)
        {
            attentionMask[0, index] = 1;
        }

        var markerPositions = new DenseTensor<long>([1, 2]);
        markerPositions[0, 0] = 1;
        markerPositions[0, 1] = 2;
        
        var markerMask = new DenseTensor<bool>([1, 2]);
        markerMask[0, 0] = true;
        markerMask[0, 1] = true;
        
        var questionTypes = new DenseTensor<long>([1]);

        using var outputs = model.Run(inputIds, attentionMask, markerPositions, markerMask, questionTypes);

        Assert.That(outputs.Single(output => output.Name == "logits").AsTensor<float>().Dimensions.ToArray(),
            Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void Predict_should_return_typed_answers_for_choice_score_and_noul()
    {
        using var model = new DecisionModel();
        var state = new Dictionary<string, object?>
        {
            ["body"] = "We were charged twice for March. Please refund the duplicate or we will cancel.",
        };
        var questions = new QuestionSet
        {
            ["department"] = Question.Choice(
                "Which department should handle this?",
                new Dictionary<string, string?>
                {
                    ["billing"] = "invoices, payments, refunds",
                    ["technical"] = "bugs, outages, system errors",
                    ["other"] = "everything else",
                }),
            ["urgency"] = Question.Score("How urgent is this request?", "not urgent", "soon", "blocking"),
            ["churn_risk"] = Question.Noul("Does the user threaten to cancel or leave?"),
        };

        var result = model.Predict(state, questions);
        var department = result["department"].AsChoice();
        var urgency = result["urgency"].AsScore();
        var churnRisk = result["churn_risk"].AsNoul();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Ids, Is.EqualTo(["department", "urgency", "churn_risk"]));
            Assert.That(department.Choice, Is.AnyOf("billing", "technical", "other"));
            Assert.That(department.Probabilities.Sum(option => option.Value), Is.EqualTo(1).Within(0.001));
            Assert.That(urgency.Probabilities, Has.Count.EqualTo(3));
            Assert.That(urgency.Score, Is.InRange(0, 2));
            Assert.That(urgency.Probabilities.Sum(), Is.EqualTo(1).Within(0.001));
            Assert.That(churnRisk.Probability, Is.InRange(0, 1));
            Assert.That(churnRisk.Value, Is.EqualTo(churnRisk.Probability >= 0.5));
            Assert.That(result.InputTokens, Is.GreaterThan(0));
            Assert.That(result.OutputTokens, Is.Zero);
        }
    }

    [Test]
    public void Predict_should_match_laya_english_quickstart_fixture()
    {
        using var model = new DecisionModel();
        const string state = "Subject: Double charged on invoice 88213\n\nHi, I was billed twice this month for the Pro plan and the second charge has not been refunded. This is the third time I have written in. If it is not fixed today I am cancelling the account.";
        var questions = new QuestionSet
        {
            ["department"] = Question.Choice(
                "Which department should handle this?",
                new Dictionary<string, string?>
                {
                    ["billing"] = "invoices, payments, double charges, refunds",
                    ["technical"] = "bugs, outages, integration errors",
                    ["account"] = "logins, seats, plan changes",
                }),
            ["urgency"] = Question.Score("How urgent is this request?", "not urgent", "this week", "today", "drop everything"),
            ["churn_risk"] = Question.Noul("The customer threatens to cancel the account."),
            ["needs_human"] = Question.Noul(
                "This requires a human agent rather than an automated reply.",
                "a template reply resolves it",
                "a person must intervene"),
        };

        var result = model.Predict(state, questions);

        using (Assert.EnterMultipleScope())
        {
/*
        ==============================
        Note.
        The commented-out asserions below are eqivalent to the tests for the originally exported Laya model weights.
        Thier tolerances are extremely tight (0.02%) and the actual tests are well with this range.
        The default model shipped with Nev has been quantised and therefore, loses some accuracy, so the acceptabel tollerances have been adjusted.
        As the model is probablistic, a key consideration here is not that the model is now "less accurate" (as the original unquantised version was also probabilistic)
        - So could be either the original expectations could be adjusted to represent the given output, or the tolerances adjusted.
        Regardless, I've adjusted the tolerances to give an easier comparison of the differneces.
        Ideally, you should test your real-world use case, before deploying your chosen model.  
        ==============================

                Assert.That(result.InputTokens, Is.EqualTo(381));
                Assert.That(result["department"].AsChoice().Choice, Is.EqualTo("billing")); //
                Assert.That(result["department"].AsChoice()["billing"], Is.EqualTo(0.9653).Within(0.0002));
                Assert.That(result["urgency"].AsScore().Score, Is.EqualTo(1.9821).Within(0.0002)); //
                Assert.That(result["urgency"].Confidence, Is.EqualTo(0.4809).Within(0.0002));
                Assert.That(result["churn_risk"].AsNoul().Probability, Is.EqualTo(0.968).Within(0.0002));
                Assert.That(result["needs_human"].AsNoul().Probability, Is.EqualTo(0.4579).Within(0.0002));
*/
                Assert.That(result.InputTokens, Is.EqualTo(381));
                Assert.That(result["department"].AsChoice().Choice, Is.EqualTo("billing")); 
                Assert.That(result["department"].AsChoice()["billing"], Is.EqualTo(0.9653).Within(0.005));
                Assert.That(result["urgency"].AsScore().Score, Is.EqualTo(1.9821).Within(0.005));
                Assert.That(result["urgency"].Confidence, Is.EqualTo(0.4809).Within(0.021)); //This is the largest discrepency / adjustment in tolderance required of c.2.1%
                Assert.That(result["churn_risk"].AsNoul().Probability, Is.EqualTo(0.968).Within(0.008));
                Assert.That(result["needs_human"].AsNoul().Probability, Is.EqualTo(0.4579).Within(0.011));
        }
    }

    [Test]
    public void SequenceBuilder_should_match_laya_example_department_prompt()
    {
        using var model = new DecisionModel();
        var question = Question.Choice(
            "Which department should handle this?",
            new Dictionary<string, string?>
            {
                ["billing"] = "invoices, payments, double charges, refunds",
                ["technical"] = "bugs, outages, integration errors",
                ["account"] = "logins, seats, plan changes",
            });
        const string state = "Subject: Double charged on invoice 88213\n\nHi, I was billed twice this month for the Pro plan and the second charge has not been refunded. This is the third time I have written in. If it is not fixed today I am cancelling the account.";

        var sequence = SequenceBuilder.Build(question, model.EncodeAsTokenIDs(state), model.EncodeAsTokenIDs, 50281, 50282, 50284, 512, 192);

        Assert.That(sequence.Ids.Take(45), Is.EqualTo(new[]
        {
            50281, 22122, 1953, 27, 6758, 7811, 943, 6016, 436, 32, 50282,
            50284, 33484, 27, 29838, 1271, 13, 10762, 13, 4021, 7260, 13, 1275, 41748,
            50284, 7681, 27, 19775, 13, 562, 1131, 13, 9554, 6332,
            50284, 2395, 27, 2412, 968, 13, 13512, 13, 2098, 2544, 50282,
        }));
        Assert.That(sequence.Ids.Skip(45), Is.EqualTo(new[]
        {
            18961, 27, 18603, 6636, 327, 45156, 854, 3507, 1012, 187, 187, 12764, 13, 309,
            369, 47045, 7019, 436, 1770, 323, 253, 1294, 2098, 285, 253, 1273, 4179, 556,
            417, 644, 23005, 264, 15, 831, 310, 253, 2626, 673, 309, 452, 3542, 275, 15,
            1310, 352, 310, 417, 4229, 3063, 309, 717, 476, 68, 3485, 253, 2395, 15, 50282,
        }), string.Join(",", sequence.Ids.Skip(45)));
        Assert.That(sequence.Markers, Is.EqualTo(new[] { 11, 24, 34 }));
    }

    [Test]
    public void MultilingualModel_should_encode_hindi_and_run_inference()
    {
        EnsureCheckpointDataAvailable(
            ModelCheckpoint.Multilingual,
            "https://cdn.nev-ai.com/models/int8-quant/multilingual/model.onnx.data");

        using var model = new DecisionModel(ModelCheckpoint.Multilingual);
        const string state = "मुझसे दो बार शुल्क लिया गया, कृपया पैसे वापस करें।"; //"mujhse do baar shulk liya gaya, kripya paise wapas karen." -> "I have been charged twice; please provide a refund." (thanks to Google Transalate)
        var questions = new QuestionSet
        {
            ["department"] = Question.Choice(
                "Which department should handle this?",
                new Dictionary<string, string?>
                {
                    ["billing"] = "invoices, payments, refunds",
                    ["technical"] = "bugs, outages, system errors",
                    ["other"] = "everything else",
                }),
        };

        var ids = model.EncodeAsTokenIDs(state);
        var result = model.Predict(state, questions);
        var answer = result["department"].AsChoice();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(answer.Choice, Is.EqualTo("billing"));
            Assert.That(ids, Is.EqualTo(new[]
            {
                39728, 238385, 20579, 51484, 99744, 144905, 228834, 158850, 44370,
                235269, 232499, 4587, 54026, 195109, 235533, 38607, 235940,
            }), string.Join(",", ids));
            Assert.That(result.InputTokens, Is.GreaterThan(0));
            Assert.That(answer.Probabilities.Sum(option => option.Value), Is.EqualTo(1).Within(0.001));
        }
    }

    [Test]
    public void TypedDecisionModel_should_load_and_predict()
    {
        EnsureCheckpointDataAvailable(
            ModelCheckpoint.TypedDecision,
            "https://cdn.nev-ai.com/models/int8-quant/typed-decision/model.onnx.data");

        //note this test does not test for a sensible answer - just that it runs.
        using var model = new DecisionModel(ModelCheckpoint.TypedDecision);
        var questions = new QuestionSet
        {
            ["workflow"] = Question.Choice(
                "Which workflow does this request belong to?",
                new Dictionary<string, string?>
                {
                    ["invoice_processing"] = "invoices, payments, and billing corrections",
                    ["security_incident"] = "security alerts and suspicious activity",
                    ["customer_service"] = "customer support and account questions",
                    ["agent_observability"] = "traces, logs, and agent behavior",
                }),
        };

        var result = model.Predict(
            "The customer was charged twice for an invoice and requests a refund.",
            questions);
        var answer = result["workflow"].AsChoice();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(answer.Choice, Is.AnyOf("invoice_processing", "security_incident", "customer_service", "agent_observability"));
            Assert.That(answer.Probabilities.Sum(option => option.Value), Is.EqualTo(1).Within(0.001));
        }
    }

    private static void EnsureCheckpointDataAvailable(string checkpoint, string downloadUrl)
    {
        var modelPath = new ModelPathResolver(checkpoint).GetModelPath();
        var dataPath = Path.Combine(Path.GetDirectoryName(modelPath)!, "model.onnx.data");
        if (File.Exists(dataPath))
            return;

        var message = $"⭕ WARNING: {checkpoint} model test did not run to completion because its ONNX data file is missing. " +
            $"Download it from {downloadUrl} and place it at '{dataPath}', beside model.onnx, then rerun the tests.{Environment.NewLine}";
        Assert.Ignore(message);
    }
}
