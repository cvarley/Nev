# Responsible AI Use

AI can cause real harm when it is used without appropriate safeguards. Nev produces structured predictions to support decision-making; it does not understand the full context of a situation, provide a dependable explanation for its predictions, or replace professional judgment.

A prediction can be wrong, including when its probability or confidence appears high.

Organisations integrating Nev are responsible for deciding whether a use is appropriate, testing it in its intended setting, and protecting the people affected by its outputs. The guidance below is a starting point, not a substitute for applicable laws, regulations, or professional standards.

## Human Oversight

Do not use Nev as the sole or final decision-maker for consequential decisions about people. This includes decisions that can significantly affect a person's health, safety, education, employment, finances, housing, legal position, or access to essential services.

In these settings, a qualified and accountable person should make the final decision. They should be able to review the relevant underlying information, understand that the output is a prediction, question or override it, and escalate uncertain or unusual cases. Human review should be meaningful: do not ask people to approve outputs automatically or hold them responsible without giving them enough time, information, and authority to intervene.

Nev may be appropriate as decision support for tasks such as organising a queue or suggesting a category, provided the impact is assessed and suitable review and escalation are in place. Keep the system's role and the human decision-maker's role clear to both staff and potentially affected people.

## Validate Before Use

Test the complete application workflow on representative data from the setting where it will be used. Check both overall performance and errors that could cause harm, including differences across relevant groups. Include edge cases, missing or ambiguous information, and examples that may be poorly represented in the evaluation data.

Choose thresholds and escalation rules based on the consequences of false positives and false negatives. A model probability is an estimate, not a guarantee about an individual case; validate its behavior for your task and population rather than assuming that a displayed score is reliable enough to automate an action.

If the system cannot handle a case safely, route it for review instead of forcing an automated outcome.

## Fairness and Monitoring

Consider whether the input data, question wording, available answer choices, or thresholds could produce unfair outcomes. Avoid using sensitive or proxy attributes to make decisions unless there is a lawful, justified, and carefully assessed reason to do so. Review outcomes for disparate error patterns where appropriate and lawful.

After deployment, monitor errors, overrides, escalation rates, and changes in the data or workflow. Provide a way for staff and affected people to report problems. Reassess the system when the model, questions, thresholds, input sources, or intended use changes, and pause or roll back use if it is producing unacceptable harm.

## Privacy and Security

Send the model only the information needed for the task. Determine whether inputs or outputs are stored, logged, or shared by the surrounding application and infrastructure; local inference alone does not guarantee that data is private. Apply appropriate access controls, retention limits, security protections, and legal requirements to both inputs and results.

## Clear Communication and Accountability

Tell relevant users and staff when AI contributes to a decision, what role its output plays, and how a person can request review where applicable. Keep records sufficient to investigate significant outcomes, including the model/checkpoint, question definitions, thresholds, and human actions, while following privacy and retention requirements.

Assign an owner who is responsible for evaluating the system, responding to incidents, and reviewing whether its continued use is appropriate. Do not present Nev's outputs as facts or use the system for tasks it has not been evaluated to support.
