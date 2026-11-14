// SPDX-License-Identifier: MIT

namespace Nev.Core;

/// <summary>Identifies the kind of typed decision requested from the model.</summary>
public enum QuestionType
{
    ///<summary>Selects one label from a set of choices.</summary>
    Choice = 0,

    ///<summary>Estimates a position on ordered levels, such as a request's urgency from "not urgent" to "critical".</summary>
    Score = 1,

    ///<summary>Estimates the probability that a yes/no statement is true.</summary>
    Noul = 2,
}