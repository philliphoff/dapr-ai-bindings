using System.Text.Json.Serialization;

namespace DaprAI;

public sealed record DaprCompletionResponse(
    [property: JsonPropertyName("assistant")]
    string AssistantResponse);
