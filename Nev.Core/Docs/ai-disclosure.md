---
title: "AI Disclosure"
disclosure-default: ai-assisted
models-used:
 - openai/gpt-6-luna
 - github/copilot-auto
providers:
 - OpenAI
 - GitHub (Microsoft Corporation)
scope: code-refactoring, linting-fixes, boilerplate-generation, autocompletion, documentation drafting and review and model-data mapping.
version: 1.0.0
published: true
---

## AI Disclosure

The project author(s) use AI tools to maintain code quality and expedite development.

## How AI is Used

* **Autocompletion:** GitHub Copilot (using Auto model routing) is used for minor syntax assistance and boilerplate code.
* **Documentation Review:** GitHub Copilot is used for documentation drafting and review, such as checking for accurate field names, misspellings or missing parameter names.
* **Code Refactoring:**  GPT-6 Luna is used for code review, to suggest performance improvements and remove redundant code comments.
* **Model Data Mapping:** GPT-6 Luna is used to map data for inference and tests where manually mapping tokens and IDs would be error-prone and laborious.
* **DevOps Scripts:** GPT-6 Luna is used to write and verify build scripts.

## Human Oversight

AI-assisted changes are manually reviewed for accuracy, clarity, conciseness and compliance with security and professional coding practices.
Where tests include generated data, these have been manually reviewed and verified as correct prior to being used for automated unit testing.
