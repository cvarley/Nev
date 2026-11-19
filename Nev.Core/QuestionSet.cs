// SPDX-License-Identifier: MIT
using System.Collections;

namespace Nev.Core;

/// <summary>
/// An insertion-ordered collection of questions keyed by the IDs used to retrieve their answers.
/// </summary>
public sealed class QuestionSet : IEnumerable<KeyValuePair<string, Question>>
{
    private readonly List<string> _ids = [];
    private readonly Dictionary<string, Question> _questions = new(StringComparer.Ordinal);

    public int Count => _ids.Count;

    public IReadOnlyList<string> Ids => _ids;

    public Question this[string id]
    {
        get => _questions[id];
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(id);
            ArgumentNullException.ThrowIfNull(value);
            
            if (!_questions.ContainsKey(id)) _ids.Add(id);
            
            _questions[id] = value;
        }
    }

    public void Add(string id, Question question)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(question);
        if (!_questions.TryAdd(id, question))
            throw new ArgumentException($"A question named '{id}' already exists.", nameof(id));
        _ids.Add(id);
    }

    public IEnumerator<KeyValuePair<string, Question>> GetEnumerator()
    {
        foreach (var id in _ids)
            yield return new KeyValuePair<string, Question>(id, _questions[id]);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}