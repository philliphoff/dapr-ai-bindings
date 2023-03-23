using System.Text.Json.Serialization;

namespace DaprAI;

public sealed record DaprAIEngineCompletionResponse(
    [property: JsonPropertyName("assistant")]
    string AssistantResponse);
