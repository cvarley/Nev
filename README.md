[![NEV](Nev.Core/Docs/banner-md.jpg)](Nev.Core/Docs/banner-md.jpg)

[![MIT license](Nev.Core/Docs/license-mit-blue.svg)](https://github.com/cvarley/Nev/blob/master/LICENCE.md) 
[![Made for .NET](Nev.Core/Docs/made-for-dotnet-green.svg)](https://dotnet.microsoft.com/) 
[![.NET](https://github.com/cvarley/Nev/actions/workflows/dotnet.yml/badge.svg)](https://github.com/cvarley/Nev/actions/workflows/dotnet.yml)

[<< HOME](https://www.nev-ai.com)

## Nev - A semantic decision engine for .NET

Nev is an open-source “semantic decision engine” for .NET that runs on a local CPU. It was inspired by the [Jev AI model](https://en.wikipedia.org/wiki/Jev_(AI_model)).

Read the [full documentation](Nev.Core/Docs/index.md) for details.

## Installing Nev

Before you start, you'll need the [Microsoft .NET 10 or newer SDK](https://dotnet.microsoft.com/en-us/download) and [Visual Studio Code](https://code.visualstudio.com/) installed. 

-  Install the Nev.Core package in to your project using:

```powershell
dotnet add package Nev.Core
```
- Build the project. 

The first time you build your project, you'll need network access as the default model data file is downloaded to your output directory.

(*N.B. Please be patient. The file is **402 MB**, so it may take some time to download on a slow connection. Refreshments at the ready!*)

## Example 1 - A Yes/No Probability ("Noul") question - Sentiment Analysis

This example uses a `Noul` question, which returns the probability that a yes/no statement is true, as well as a boolean value derived from that probability.

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

For example, the output might be:

```output
True: 96%
```

>*(N.B. The exact result will depend on the model's prediction.)*

## Example 2 - A Choice question - Routing mail to the correct department.

Another slightly more complex example is routing a customer email to a department. Provide:

- the **state** to analyse, such as the email content itself
- the **questions** the model should answer
- the possible **answers** or criteria for each question

Run the model to get a structured answer to each question.

For example:

```csharp
using Nev.Core;

using var model = new DecisionModel();
var result = model.Predict(
    "We were charged twice for March. Please refund the duplicate.",
    new QuestionSet
    {
        ["department"] = Question.Choice(
            "Which department should handle this request?",
            "billing",
            "technical",
            "other"),
    });

var answer = result["department"].AsChoice();
Console.WriteLine(answer.Choice);
```

For example, the selected department might be:

```output
billing
```
>*(N.B. once again, the actual result will depend upon the model's prediction.)*

---
## Notes

### Licensing and responsible use

Nev is an independent, personal, open-source research project inspired by TypeSafe AI's Jev model. It is neither affiliated with, endorsed by, nor associated with TypeSafe AI. Nev has its own independently developed implementation and architecture.

Nev is provided under the terms of the [MIT License](LICENCE.md). 
The model files are distributed under the [Apache License 2.0](Nev.Core/Model/LICENSE.md).

### Responsible Use

Please also read the [community code of conduct](code-of-conduct.md) and the [responsible use guidance](responsible-ai-use.md).

### Contact

Please send any thoughts, feedback or questions to [hello@nev-ai.com](mailto:hello@nev-ai.com).
