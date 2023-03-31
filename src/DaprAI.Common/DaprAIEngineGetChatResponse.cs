using System.Text.Json.Serialization;

namespace DaprAI;

public sealed record DaprAIEngineGetChatResponse
{
    [JsonPropertyName("history")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DaprChatHistory? History { get; init; }
}
