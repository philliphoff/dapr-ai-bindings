using System.Text.Json.Serialization;

namespace DaprAI;

public sealed record DaprAIEngineCompletionRequest(
    [property: JsonPropertyName("user")]
    string UserPrompt)
{
    [JsonPropertyName("instanceId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? InstanceId { get; init; }
}
