// SPDX-License-Identifier: MIT

using System.Text.Encodings.Web;
using System.Text.Json;

namespace Nev.Core;

/// <summary>
/// Serialises prediction states and criteria as text for the model.
/// </summary>
internal static class JsonInputFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string State(object? state)
    {
        if (state is string text){
            return text;
        }
        var element = JsonSerializer.SerializeToElement(state, state?.GetType() ?? typeof(object), JsonOptions);
        
        return Format(element);
    }

    public static string Criterion(string? value)
    {
        return value ?? string.Empty;
    }

    private static string Format(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => "{" + string.Join(", ", element.EnumerateObject()
                .Select(property => $"{JsonSerializer.Serialize(property.Name, JsonOptions)}: {Format(property.Value)}")) + "}",
            JsonValueKind.Array => "[" + string.Join(", ", element.EnumerateArray().Select(Format)) + "]",
            JsonValueKind.String => JsonSerializer.Serialize(element.GetString(), JsonOptions),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => "null",
        };
    }
}