using System.Text.Json.Serialization;

namespace DaprAI;

public sealed record DaprAIEngineGetChatResponse
{
    [JsonPropertyName("history")]
    public DaprChatHistory? History { get; init; }
}
