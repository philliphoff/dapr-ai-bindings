using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DaprAI;

public sealed record DaprCompletionResponse(
    [property: JsonPropertyName("response")]
    string Response);