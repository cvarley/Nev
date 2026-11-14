# Typed-Decision Model

The Typed-Decision checkpoint is a fine-tuned model for a small set of predefined, multi-question workflows. It uses the same `DecisionModel` prediction API and question types as the English and Multilingual checkpoints.

## When it is selected

`ModelRouter.SelectModel(state, questionIds)` returns the Typed-Decision checkpoint only when the question IDs match one of these sets exactly:

| Workflow ID set |
| --- |
| `action`, `category`, `churn_risk`, `needs_human`, `urgency` |
| `discrepancy_severity`, `disposition`, `duplicate`, `matches_order`, `urgency` |
| `credential_compromise`, `disposition`, `severity`, `true_positive`, `urgency` |
| `action`, `needs_review`, `outcome`, `risk`, `urgency` |

Matching is case-sensitive and ignores the order of IDs, but adding or omitting an ID prevents a match. If no typed workflow matches, the router selects English or Multilingual based on the state text.

The router is a selection helper; `DecisionModel.Predict` does not call it automatically. Select the checkpoint and pass it to `DecisionModel` yourself:

```csharp
using Nev.Core;

var state = new Dictionary<string, object?>
{
    ["body"] = "The state or document to evaluate goes here.",
};

var questions = new QuestionSet
{
    // Populate the questions for one of the exact workflow ID sets above.
};

var checkpoint = ModelRouter.SelectModel(state, questions.Ids);
using var model = new DecisionModel(checkpoint);
var result = model.Predict(state, questions);
```

Define the question instructions, answer choices, or score levels in your `QuestionSet` as usual. The IDs determine checkpoint selection; they do not create questions, supply question wording, or constrain answer options. Answers are returned by ID using the standard `Noul`, `Choice`, and `Score` answer types. See the [main documentation](index.md#question-types) for question and result details.

## Model files

The Typed-Decision checkpoint files are placed under `Model/TypedDecision` in the application output. The large `model.onnx.data` file is not downloaded automatically. To use this checkpoint, place that file beside `model.onnx`; the project documentation lists the download location under [getting the model files](index.md#model-files).
