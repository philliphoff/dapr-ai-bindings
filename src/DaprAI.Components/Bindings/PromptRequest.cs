using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DaprAI.Bindings;

internal sealed record PromptRequest(
    [property: JsonPropertyName("prompt")]
    string Prompt)
{
    public static PromptRequest FromBytes(ReadOnlySpan<byte> bytes)
    {
        var requestJson = Encoding.UTF8.GetString(bytes);
        var request = JsonSerializer.Deserialize<PromptRequest>(requestJson);

        if (request is null)
        {
            throw new InvalidOperationException("Unable to deserialize request.");
        }

        return request;
    }
}
