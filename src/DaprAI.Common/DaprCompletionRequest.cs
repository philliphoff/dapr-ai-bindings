using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DaprAI;

public sealed record DaprCompletionRequest(
    [property: JsonPropertyName("prompt")]
    string Prompt)
{
    public static DaprCompletionRequest FromBytes(ReadOnlySpan<byte> bytes)
    {
        var requestJson = Encoding.UTF8.GetString(bytes);
        var request = JsonSerializer.Deserialize<DaprCompletionRequest>(requestJson);

        if (request is null)
        {
            throw new InvalidOperationException("Unable to deserialize request.");
        }

        return request;
    }
}
