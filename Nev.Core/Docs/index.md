# Nev.Core Documentation

Welcome to the Nev documentation. This guide covers the public API and normal application workflow. 

## Contents

- [What is Nev?](#what-is-nev)
- [Why choose Nev?](#why-choose-nev)
- [How it works](#how-it-works)
- [Installing Nev](#installing-nev)
- [Getting started](#getting-started)
- [States and question sets](#states-and-question-sets)
- [Question types](#question-types)
- [Reading results](#reading-results)
- [Checkpoint selection](#checkpoint-selection)
- [Input and length limits](#input-and-length-limits)
- [Common issues](#common-issues)
- [Further examples](#further-examples)
- [Important Testing Considerations](#important-testing-considerations)
- [Technical notes](#technical-notes)
- [Licensing and responsible use](#licensing-and-responsible-use)
- [Contact](#contact)

For more information, see these resources:

- [Nev-AI.com](https://nev-ai.com/), the Nev project website.
- The [project README](../README.md), with an introduction and quick start.
- [Nev.Core on NuGet](https://www.nuget.org/packages/Nev.Core/).
- [Examples and tests](../../Nev.Test/Examples/).
- The [typed-decision model guide](typed-decision.md).

## What is Nev?

Nev is a .NET library for structured decision inference. It evaluates an input (a **state**) against predefined questions and returns typed answers. It is designed for decisions such as classifying a request, assigning a category, or estimating a score, rather than generating free-form text.

## Why choose Nev?

Nev is free and open source. It's not constrained by restrictive commercial terms or conditions of use. It is a standalone .NET package requiring no other installations or dependencies (such as Python) behind the scenes. It is small and efficient and so suitable for running on generic hardware.

Nev enables developers to build various integration and deployment options for applications that need structured decisions close to their data. Nev itself does not require data to be sent to any third-party host(s).

## How it works

An interaction with Nev has two parts:

- **State:** The text or structured data to evaluate, such as an email, log, or JSON object.
- **Typed questions:** The decisions you want the model to make about that state.

Nev supports three question types: 

- **Noul:** Estimates the probability that a yes-or-no statement is true. The answer also exposes a boolean value using a default threshold of 0.5. For example, given the state "The customer was charged twice," ask "Is this a billing issue?" A probability of 0.9 would correspond to `Value == true`.
- **Choice:** Selects one label from a set of options and returns probabilities for the options. For example, ask "Which team should handle this?" with `billing`, `technical`, and `other` as options; Nev returns the selected label and its probability distribution.
- **Score:** Rates the state against ordered levels and returns an expected, zero-based score, which can be fractional. For example, with levels `low` (0), `medium` (1), and `high` (2), a score of 1.6 falls between `medium` (1) and `high` (2).

You can include multiple questions in one prediction call. See [Question types](#question-types) for details and examples.

## Installing Nev

Nev.Core targets .NET 10. Add the package to your application:

```shell
dotnet add package Nev.Core
```

You can also install it through your IDE's NuGet package manager. To build from source, clone the repository and build `Nev.slnx` with the .NET 10 (or newer) SDK.

### Model files

The NuGet package includes the model graph, tokenizer files, checkpoint configuration, and an MSBuild target. It does not include the large `model.onnx.data` files. During build and publish, the target copies model assets into the application output directory. If the English checkpoint's data file is missing, the target downloads it over HTTPS and checks its SHA-256 hash. The initial build therefore requires network access and enough disk space for the model data; later builds reuse the downloaded file.

The English checkpoint is downloaded automatically. The Multilingual and Typed-Decision checkpoint data files are not. Download the required `model.onnx.data` file and place it beside that checkpoint's `model.onnx` in the output folder:

| Checkpoint | Data file download |
| --- | --- |
| English | [Download](https://cdn.nev-ai.com/models/int8-quant/english/model.onnx.data) |
| Multilingual | [Download](https://cdn.nev-ai.com/models/int8-quant/multilingual/model.onnx.data) |
| Typed-Decision | [Download](https://cdn.nev-ai.com/models/int8-quant/typed-decision/model.onnx.data) |

The expected layout is `Model/{Checkpoint}/model.onnx` and `Model/{Checkpoint}/model.onnx.data`. The runtime looks for model files under the application base directory and its parents. A missing checkpoint directory raises `DirectoryNotFoundException`; a missing `.data` file raises `FileNotFoundException` with the expected path.

## Getting started


This example classifies a customer message and estimates whether it needs urgent attention.

Create a `QuestionSet`, run a prediction, then cast each answer to its known type:

```csharp
using Nev.Core;

var state = new Dictionary<string, object?>
{
	["subject"] = "Duplicate charge",
	["body"] = "We were charged twice for March. Please refund the duplicate or we will cancel.",
};

var questions = new QuestionSet
{
	["department"] = Question.Choice(
		"Which department should handle this?",
		new Dictionary<string, string?>
		{
			["billing"] = "invoices, payments, and refunds",
			["technical"] = "bugs, outages, and system errors",
			["other"] = "all other requests",
		}),
	["urgency"] = Question.Score(
		"How urgent is this request?",
		"not urgent", "soon", "blocking"),
	["churn_risk"] = Question.Noul(
		"Is the customer threatening to cancel or leave?"),
};

using var model = new DecisionModel();
var result = model.Predict(state, questions);

var department = result["department"].AsChoice();
var urgency = result["urgency"].AsScore();
var churnRisk = result["churn_risk"].AsNoul();

Console.WriteLine($"Department: {department.Choice}");
Console.WriteLine($"Urgency score: {urgency.Score}");
Console.WriteLine($"Cancellation risk: {churnRisk.Probability:P0}");
```

`DecisionModel()` loads the default English checkpoint.`DecisionModel` implements `IDisposable`; dispose it when finished, as with `using` above.

## States and question sets

### State

`Predict` accepts `object?` as its state:

- A `string` is used directly as the state text.
- Other values are serialized as compact JSON. Anonymous objects, records, dictionaries, and arrays are convenient ways to provide structured context.
- `null` is formatted as JSON `null`.

Use clear field names and include only information relevant to the questions. State data is combined with each question and its options for inference. Keep inputs within the checkpoint's sequence limits; excess text is truncated (see [Input and length limits](#input-and-length-limits)).

### QuestionSet

`QuestionSet` is an insertion-ordered collection of questions keyed by non-empty string IDs. IDs are used to retrieve results and should be stable, descriptive, and unique within the set.

```csharp
var questions = new QuestionSet();
questions.Add("priority", Question.Score("How urgent is this?", "low", "normal", "high"));

// The indexer can add a question or replace the question for an existing ID.
questions["priority"] = Question.Score("Rate urgency", "low", "normal", "high");
```

Adding a duplicate ID with `Add` throws `ArgumentException`. Assigning an existing ID with the indexer replaces its question without adding a second result entry. `Ids` returns IDs in insertion order. An empty set is allowed; `Predict` returns an empty result without running inference.

## Question types

Create questions with the static factory methods on `Question`. Instructions should ask for one clear decision, and options or levels should describe the distinctions the model must make.

### Choice

Use `Question.Choice` when exactly one label should be selected from a finite set. Options can be labels alone, label/description pairs, or an enumerable of key/value pairs:

```csharp
var channel = Question.Choice(
	"Which team should handle the request?",
	("billing", "payments, invoices, and refunds"),
	("technical", "bugs, outages, and integrations"),
	("other", "requests not covered by the other teams"));
```

Labels must be non-empty and unique. Each optional description gives the model additional context for distinguishing an option; it is not shown as a separate output. A choice answer contains the selected label and probabilities for every label.

### Score

Use `Question.Score` for an ordered rubric, such as severity or urgency:

```csharp
var severity = Question.Score(
	"How severe is the incident?",
	"minor inconvenience",
	"degraded but usable",
	"major outage",
	"service unavailable");
```

Levels are ordered from lowest to highest. The score is the expected, potentially fractional, zero-based position in that list. With four levels, scores range from 0 to 3; `MostLikelyLevel` is also zero-based. The current constructor requires at least one level, but a useful rubric normally has multiple clearly distinguished levels. Keep the number of levels within the model's supported range and validate the behavior for your task.

### Noul (yes/no)

Use `Question.Noul` to estimate whether a statement is true. Optional arguments can supply wording for the false and true outcomes, respectively:

```csharp
var needsReview = Question.Noul(
	"Does this case need review by a person?",
	ifFalse: "routine case that can proceed without review",
	ifTrue: "a person should review this case");
```

Without custom wording, the false and true outcomes use generic no/yes descriptions. The returned `Probability` is the probability assigned to true. `Value` is `true` when `Probability >= 0.5`; choose a different application threshold when the task's costs require it.

## Reading results

`PredictionResult` exposes:

| Member | Meaning |
| --- | --- |
| `Answers` | Read-only map from question ID to `Answer`. |
| `result[id]` | Retrieves one answer by ID; an unknown ID throws `KeyNotFoundException`. |
| `Ids` | Result IDs in question insertion order. |
| `Usage` | Input/output token counts. |
| `InputTokens`, `OutputTokens` | Convenience accessors for the corresponding usage values. |

Call `AsChoice()`, `AsScore()`, or `AsNoul()` to access a typed answer. Calling a cast method for the wrong answer type throws `InvalidOperationException`. The question type is available as `Answer.Type`.

| Answer type | Useful properties |
| --- | --- |
| `ChoiceAnswer` | `Choice` is the selected label; `Probabilities` is the distribution as label/probability pairs in option order; `answer[label]` looks up one label's probability. |
| `ScoreAnswer` | `Score` is the expected zero-based level; `MostLikelyLevel` is the index of the most probable level; `Probabilities` follows level order; `Legend` contains the level labels. |
| `NoulAnswer` | `Probability` is the probability that the statement is true; `Value` applies the 0.5 threshold. |

For example, given the `result` from the quick start, read the selected choice, inspect its distribution, map a score back to its most likely level, and read a yes/no probability like this:

```csharp
var departmentAnswer = result["department"].AsChoice();
Console.WriteLine($"Selected department: {departmentAnswer.Choice}");
foreach (var option in departmentAnswer.Probabilities)
{
	Console.WriteLine($"{option.Key}: {option.Value:P1}");
}

var urgencyAnswer = result["urgency"].AsScore();
var likelyUrgency = urgencyAnswer.Legend[urgencyAnswer.MostLikelyLevel];
Console.WriteLine($"Expected urgency: {urgencyAnswer.Score:F2}");
Console.WriteLine($"Most likely level: {likelyUrgency}");

var churnAnswer = result["churn_risk"].AsNoul();
Console.WriteLine($"Probability of churn: {churnAnswer.Probability:P1}");
Console.WriteLine($"At the default 0.5 threshold: {churnAnswer.Value}");
```

To inspect an individual choice probability, use `departmentAnswer["billing"]`. To inspect the probability of a particular score level, use its zero-based index, for example `urgencyAnswer.Probabilities[2]`. The `Legend` and `MostLikelyLevel` properties let you avoid hard-coding the level text.

All answer types also expose `Confidence`, `AnswerConfidence`, and `ActProbability`. These values describe different things:

- `AnswerConfidence` is the largest probability assigned to any outcome.
- `Confidence` measures distribution concentration. For choice and score answers it is derived from normalized entropy; for a Noul answer it is the larger of the true and false probabilities.
- `ActProbability` is the model action head's estimate that the answer can be acted on rather than escalated.

None of these values is a guarantee that an individual answer is correct. In particular, `Confidence` is not the probability of correctness. Validate any threshold using representative data, and keep human review for consequential uses. See [Responsible AI Use](responsible-ai-use.md).

`InputTokens` is the sum of the encoded model sequence lengths for all questions in the prediction, including question/option tokens and sequence markers; it is not just the token count of the state. `OutputTokens` is currently zero because the model returns structured outputs rather than generated text.

## Checkpoint selection

The constructor accepts a checkpoint name. The default is English:

```csharp
using var english = new DecisionModel();
using var multilingual = new DecisionModel(ModelCheckpoint.Multilingual);
```

`ModelCheckpoint` exposes `English`, `Multilingual`, and `TypedDecision`. The runtime does not choose or load a checkpoint automatically when you call `Predict`. 

`ModelRouter` is a helper that returns a checkpoint name; pass that name to the constructor:

```csharp
var checkpoint = ModelRouter.SelectModel(state, questions.Ids);
using var model = new DecisionModel(checkpoint);
var result = model.Predict(state, questions);
```

The router prioritises an exact match against the "Typed-Decision" question ID sets. Otherwise, it selects "Multilingual" for detected non-Latin scripts and several supported Latin-script languages; it falls back to "English" when it cannot identify another language.

This is a lightweight heuristic, not general-purpose language identification. For predictable behavior, choose the checkpoint explicitly when you know the input language or workflow.

Typed-Decision selection requires an exact, case-sensitive, order-independent set of IDs matching one of the supported workflows:

| IDs |
| --- |
| `action`, `category`, `churn_risk`, `needs_human`, `urgency` |
| `discrepancy_severity`, `disposition`, `duplicate`, `matches_order`, `urgency` |
| `credential_compromise`, `disposition`, `severity`, `true_positive`, `urgency` |
| `action`, `needs_review`, `outcome`, `risk`, `urgency` |

The matching IDs select the checkpoint; they do not supply the questions, wording, or answer options. See the [typed-decision model guide](typed-decision.md) for details.

## Input and length limits

Each question is encoded as its own input sequence. The complete sequence, including question instructions, option text and markers, separator tokens, and state, is capped at 512 tokens for English and 1024 tokens for Multilingual and Typed-Decision. These limits apply to the entire sequence, not just the state. The instructions and all option fragments share a separate head budget of 192 tokens for English and 256 tokens for Multilingual and Typed-Decision.

Each choice or score option fragment is additionally limited to 48 encoded tokens, including its marker. After the question and options are encoded, only the remaining sequence capacity is available for the state. If the state is too long, its trailing tokens are truncated; Nev does not split it into multiple predictions. Because each question has its own instructions and options, the available state capacity can differ by question.

You should keep instructions and option descriptions concise, prioritise relevant state, and test long inputs. `DecisionModel.EncodeAsTokenIDs(string)` is available when you need the checkpoint tokenizer's token IDs for diagnostics; do not substitute a different tokenizer when preparing model inputs.

## Common issues

| Symptom | Likely cause and resolution |
| --- | --- |
| Build cannot download English model data | Check network access, HTTPS access to the configured model URL, and available disk space. The build target exposes `NevEnglishModelUrl` and `NevEnglishModelSha256` MSBuild properties for controlled mirrors; the downloaded file must match the configured SHA-256. |
| Checkpoint directory not found | Ensure model files are present under `Model/{Checkpoint}` in the application output or a searched parent directory. Check the checkpoint name and deployment content. |
| Missing `.data` file at runtime | Download the required `model.onnx.data` file and place it beside the checkpoint's `model.onnx`. Non-English data files are not fetched by the build process. |
| Wrong answer type cast | Match `AsChoice`, `AsScore`, or `AsNoul` to the question type used for that ID. |
| Unexpected router selection | The language detector is heuristic, and Typed-Decision routing depends on an exact ID set. Select `ModelCheckpoint` explicitly if automatic selection is not appropriate. |
| Long input has incomplete context | The sequence is bounded and state text can be truncated. Reduce or summarize the state before prediction and keep question and option text concise. |

For deployment, privacy, evaluation, and human-review considerations, see [Responsible AI Use](responsible-ai-use.md).

## Further examples

The `Nev.Test` project contains further examples (packaged as runnable unit tests). See [Examples](../../Nev.Test/Examples/).

The tests illustrate how to use Nev's features to make structured decisions.

| Test | Question Type / Feature | Description |
| -- | -- | -- |
| [Simple Yes or No](/Nev.Test/Examples/01_first_call_minimal.cs) | Noul | Estimates whether a customer explicitly requests a refund. |
| [Making a Choice](/Nev.Test/Examples/02_hello_world_decision.cs) | Choice | Routes a duplicate-charge refund request to the billing department. |
| [Reading the Result](/Nev.Test/Examples/03_reading_the_result.cs) | Multiple | Reads typed Choice, Score, and Noul answers, their labels, and token usage from one prediction. |
| [Choice Question](/Nev.Test/Examples/04_choice_question.cs) | Choice | Selects a department for a duplicate-charge request. |
| [Choice Criteria Design](/Nev.Test/Examples/05_choice_criteria_design.cs) | Choice | Compares concise and descriptive option criteria when routing a production outage. |
| [Score Question](/Nev.Test/Examples/06_score_question.cs) | Score | Scores the urgency of a production checkout outage against an ordered rubric. |
| [Score Levels](/Nev.Test/Examples/07_score_levels.cs) | Score | Shows how rubric size defines the score scale for an urgent outage. |
| [Noul Question](/Nev.Test/Examples/08_noul_question.cs) | Noul | Returns a probability and boolean for a direct refund request. |
| [Noul Criteria Wording](/Nev.Test/Examples/09_noul_criteria_wording.cs) | Noul | Supplies custom wording for the true and false outcomes of a refund question. |
| [Mixed Primitives in One Pass](/Nev.Test/Examples/10_mixed_primitives_one_pass.cs) | Multiple | Combines department choice, urgency score, and cancellation-risk assessment in one prediction. |
| [JSON State](/Nev.Test/Examples/11_json_state.cs) | Multiple | Queries delivery and refund details from a structured order dictionary. |
| [State Wording Matters](/Nev.Test/Examples/12_state_wording_matters.cs) | Noul | Passes the same refund and cancellation facts as prose, email fields, and JSON. |
| [Conversation Turns](/Nev.Test/Examples/13_conversation_turns.cs) | Multiple | Uses a sequence of conversation turns to assess frustration, refund intent, cancellation, and escalation. |
| [Many Questions in One Pass](/Nev.Test/Examples/14_many_questions_one_pass.cs) | Multiple | Answers seven questions in one prediction and compares batch token usage with separate calls. |
| [Confidence Gating](/Nev.Test/Examples/15_confidence_gating.cs) | Choice | Applies an answer-confidence threshold to decide whether to automate or escalate a vague account request. |
| [Many States in a Loop](/Nev.Test/Examples/16_many_states_loop.cs) | Multiple | Processes separate billing, technical, and pricing tickets with the same question set. |
| [Model Routing](/Nev.Test/Examples/17_routing_without_running.cs) | Model routing | Selects English or Multilingual checkpoints based on the input script without running inference. |
| [Preset Email Phishing](/Nev.Test/Examples/18_preset_email_phishing.cs) | Multiple | Classifies a suspicious account-verification email and assesses phishing, spam, urgency, and reply expectations. |
| [Preset Triage](/Nev.Test/Examples/19_presets_triage.cs) | Multiple | Extracts intent, urgency, frustration, refund request, and cancellation risk from a billing ticket. |
| [Preset Guardrails](/Nev.Test/Examples/20_presets_guardrails.cs) | Multiple | Flags an instruction-override prompt and classifies its topic and severity. |
| [Preset Moderation](/Nev.Test/Examples/21_presets_moderation.cs) | Multiple | Assesses insulting content for toxicity, harassment, threats, spam, and severity. |
| [Preset Model Router](/Nev.Test/Examples/22_presets_model_router.cs) | Multiple | Uses domain, sensitivity, tool need, and difficulty answers to select a handling tier. |
| [Custom Schema Design](/Nev.Test/Examples/23_custom_schema_design.cs) | Multiple | Applies a custom bug-triage schema to a reproducible crash with possible data loss. |
| [Typed Decisions Workflow](/Nev.Test/Examples/24_typed_decisions_workflow.cs) | Model routing | Selects the Typed-Decision checkpoint for an exact supported set of question IDs. |
| [High-Cardinality Choice](/Nev.Test/Examples/25_high_cardinality_choice.cs) | Choice | Checks probability distributions for choice questions with large numbers of options. |
| [Batch Throughput](/Nev.Test/Examples/26_batch_throughput.cs) | Multiple | Requests department, urgency, and refund decisions together and checks result usage. |
| [Long Document](/Nev.Test/Examples/27_long_document.cs) | Noul | Demonstrates bounded input on a long case history where the cancellation detail is near the end. |
| [Error Handling](/Nev.Test/Examples/28_error_handling.cs) | Validation | Verifies that empty choice options and option blocks beyond the sequence limit are rejected. |
| [Production Triage Service](/Nev.Test/Examples/29_production_triage_service.cs) | Multiple | Maps structured triage answers for a refund and cancellation threat to a policy decision. |

## Important testing considerations

The tests verify behavior and result structure; they **DO NOT** guarantee that a model's predictions are accurate. Inference is a probabilistic process, so evaluate Nev with representative inputs for your task. Consider the consequences of errors: a misclassified document may be inconvenient, while an error in hospital triage could be catastrophic. Assess the system carefully before using it in any high-consequence process. See [Responsible AI Use](responsible-ai-use.md) for details.

For example, the request shown below could reasonably be handled by delivery, billing, or returns: it describes a late shipment, a damaged product, and a request to refund the shipping fee. Technical support and sales are less relevant. Nonetheless, a `ChoiceQuestion` will return a single `Choice` if requested and so could be any one of the three *likely* choices, even though none is an *exact* fit. 

```json
State    : "My order arrived late, the box is torn, so I'd like the shipping refunded."
Question : "Which team should handle this request?"
Choices  : "delivery", "billing", "returns", "technical", "sales"
```
## Technical notes

Nev is an open source library (.dll) for .NET inspired by Jev and uses a model adapted from the open-weight Laya model. It uses a "*quantized non-auto-regressive system-one neural network with a tiny decision head based on the open-weight models.*" to make decisions.

There's a lot in this sentence! Let's unpack it...

Nev uses...

- **a neural network** — A neural network is a computer program modeled after the human brain. It uses layers of connected digital "neurons" connected with "weights" to spot patterns in data. This is the underlying structure used to encode text, map complex relationships between words and make predictions based on these relationships.
- **with a tiny decision head** — A highly specialised, lightweight output layer tacked onto the network that strictly computes probability scores for predefined choices. Rather than an LLM's massive text-generation head capable of outputting a vocabulary of 100,000 words, this head only has to return a tiny set of specific structured numbers, distributions, or booleans.

It is...

- **quantized** — The model's numerical weights have been compressed (e.g., from 16-bit to 8-bit or 4-bit precision). This massive reduction in memory footprint allows it to run incredibly fast, use less power, and fit onto cheaper, standard hardware without a meaningful drop in accuracy.
- **non-auto-regressive** — It processes the entire prompt and outputs all answers simultaneously in a single forward pass, rather than generating tokens iteratively that in turn produce text word-by-word. Traditional LLMs are "auto-regressive," meaning they must loop repeatedly to guess each next token. By removing this loop, "non-autoregressive" models cut this inference time down to milliseconds.
- **a system-one model** — An architectural design focused entirely on fast, instinctive pattern-matching and discrete choice selection. Named after psychologist Daniel Kahneman's concept of immediate human intuition, it trades slow, multi-step "System 2" logical reasoning for instant, bounded decision-making.


Nev returns probabilities and confidence-related values with its answers. These are model estimates, not guarantees of correctness or statistical calibration for your task. Evaluate the model with representative data, set thresholds according to the consequences of errors, and use human review where appropriate. See [Responsible AI Use](responsible-ai-use.md).

## Licensing and responsible use

Nev is an independent, personal, open-source research project inspired by TypeSafe AI's Jev model. It is neither affiliated with, endorsed by, nor associated with TypeSafe AI. Nev has its own independently developed implementation and architecture.

You must review both the MIT and Apache 2.0 licenses when using or redistributing the software and related models.

### MIT Licence (SPDX-ID: MIT)

#### Nev.Core 

Source code is provided under the terms of the [MIT License](../LICENCE.md).

### Apache Licence 2.0 (SPDX-ID: Apache-2.0)

Nev uses and adapts the open weights model [Laya](https://huggingface.co/convaiinnovations/laya), (Exported  commit [7b928d828b7b0e022f929d9bd2e44165aa270148](https://huggingface.co/convaiinnovations/laya/tree/7b928d828b7b0e022f929d9bd2e44165aa270148) -- parent --> [55cf4c4ebb4ebe31b2550e8bdf3bd21b99753851](https://huggingface.co/convaiinnovations/laya/tree/55cf4c4ebb4ebe31b2550e8bdf3bd21b99753851)) The snapshot was verified as safe by [PaloAlto Networks ProtectAI](https://insights-db.paloaltonetworks.com/models/convaiinnovations/laya/7b928d828b7b0e022f929d9bd2e44165aa270148/files?blob-id=9bef25a12dcb807627a798837cfbb0ea73c0e38d) at time of export.

The model files are distributed under the [Apache License 2.0](../Model/LICENSE).

The models data have subsequently been converted to the [Open Neural Network Exchange Format \(ONNX\)](https://onnx.ai/) and weights have been quantized to 8-bit integers.The ONNX model exports used by Nev were generated using GitHub Copilot/OpenAI GPT-6. See: [AI Disclosure](ai-disclosure.md).

You can download the quantized model data here:

- [English model data](https://cdn.nev-ai.com/models/int8-quant/english/model.onnx.data)
- [Multilingual model data](https://cdn.nev-ai.com/models/int8-quant/multilingual/model.onnx.data)
- [Typed-Decision model data](https://cdn.nev-ai.com/models/int8-quant/typed-decision/model.onnx.data)

Sincere thanks to:

- **Diogo Almeida** of TypeSafe AI, creator of the orginal Jev System One Model.
- **Nandakishor Mukkunnoth**, creator of the Laya open-weight model.

### Responsible Use

Please also read the [community code of conduct](code-of-conduct.md) and the [responsible use guidance](responsible-ai-use.md).

## Contact

Please send any thoughts, feedback or questions to [hello@nev-ai.com](mailto:hello@nev-ai.com).
