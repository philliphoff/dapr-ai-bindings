using System.Text.Json.Serialization;

namespace DaprAI;

public sealed record DaprAIEngineGetChatsRequest
{
    [JsonPropertyName("withHistory")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? WithHistory { get; init; }

    [JsonPropertyName("limit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Limit { get; init; }
}
