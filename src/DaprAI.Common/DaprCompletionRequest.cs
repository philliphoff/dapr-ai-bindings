using System.Text.Json.Serialization;

namespace DaprAI;

public sealed record DaprCompletionRequest(
    [property: JsonPropertyName("user")]
    string UserPrompt)
{
    [JsonPropertyName("history")]
    public DaprChatHistory? History { get; init; }
}
