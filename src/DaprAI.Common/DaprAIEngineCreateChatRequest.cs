using System.Text.Json.Serialization;

namespace DaprAI;

public sealed record DaprAIEngineCreateChatRequest(
    [property: JsonPropertyName("instanceId")]
    string InstanceId)
{
    [property: JsonPropertyName("system")]
    public string? SystemInstructions { get; init; }
}
