---
SPDX-License-Identifier: MIT
---
# Nev - A fast semantic decision engine for .NET

Nev is a free, open source AI library for .NET and C# developers that makes fast, structured decisions instead of generating text.

You can use it for tasks such as risk scoring, customer support, email triage and many more. Read the [full documentation](Docs/index.md) for details.

## Example

```csharp
using Nev.Core;

using var model = new DecisionModel();
var result = model.Predict(
    "This product is awful and I regret buying it.",
    new QuestionSet
    {
        ["negative"] = Question.Noul("Is the review negative?"),
    });

var answer = result["negative"].AsNoul();
Console.WriteLine($"{answer.Value}: {answer.Probability:P0}");
```

Results in output such as:

```output
True: 96%
```
