using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DaprAI.Bindings;

internal sealed record PromptResponse(
    [property: JsonPropertyName("response")]
    string Response)
{
    public byte[] ToBytes()
    {
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this));
    }
}
