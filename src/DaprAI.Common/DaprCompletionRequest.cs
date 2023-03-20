using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DaprAI;

public sealed record DaprCompletionRequest(
    [property: JsonPropertyName("prompt")]
    string Prompt)
{
    [JsonPropertyName("system")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? System { get; init; }
}
