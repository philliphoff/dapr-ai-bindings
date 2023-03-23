using System.Text.Json.Serialization;

namespace DaprAI;

public sealed record DaprCompletionRequest(
    [property: JsonPropertyName("prompt")]
    string Prompt)
{
    [JsonPropertyName("instanceId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? InstanceId { get; init; }

    [JsonPropertyName("system")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? System { get; init; }
}
