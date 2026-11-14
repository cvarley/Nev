# Nev - Fast A.I. decision support for .NET

Nev is a free, open source AI library for .NET and C# developers that makes fast, structured decisions instead of generating text.

You can use it for tasks such as risk scoring, customer support, email triage and many more. Read the [full documentation](Docs/index.md) for details.

It's named after the [Jev](https://en.wikipedia.org/wiki/Jev_(AI_model)) AI Model ... which is named after... [Jevons Paradox](https://en.wikipedia.org/wiki/Jevons_paradox)... which is named after... [William Jevons](https://en.wikipedia.org/wiki/William_Stanley_Jevons)

But it's *not* `Jev`... 

It's `Nev`!

**(n. proper noun)* [blend of Not and Jev].

## Quick Start

### Install Nev

Before you start, you'll need the [Microsoft .NET 10 or newer SDK](https://dotnet.microsoft.com/en-us/download) and [Visual Studio Code](https://code.visualstudio.com/) installed. 

1. Create or select a folder

1. Create a console app and install the Nev.Core package to its project directory with:

```powershell
dotnet new console
dotnet add package Nev.Core
```

Before the build completes, Nev copies the model data file to your application's output directory. If the `English` checkpoint's model data file (`Model/English/model.onnx.data`) is missing, the build downloads it. This requires network access the first time the file is needed in that output directory. 

Refreshments at the ready! The default data file is **402 MB** and so may take some time to download on a slow connection. 

*(N.B. This automatic download applies both when Nev is installed from NuGet and when building from source. Subsequent builds reuse the downloaded file. If you are having difficulty, you can [download the model data files](Docs/index.md#model-files) manually.)*

### Example 1 - A Yes/No Probability ("Noul") question - Sentiment Analysis

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

*(N.B. The exact result will depend on the model's prediction.)*

### Example 2 - A Choice question - Routing mail to the correct department.

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
*(N.B. once again, the actual result will depend upon the model's prediction.)*

You can read the [full documentation here](Docs/index.md).

Copyright © 2026 [Chris Varley](https://www.linkedin.com/in/chrisvarley). Nev is licenced under the terms of the [MIT Licence](LICENCE.md) 
