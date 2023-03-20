using System.Text.Json.Serialization;

namespace DaprAI;

public sealed record DaprSummarizationResponse(
    [property: JsonPropertyName("summary")]
    string Summary);
