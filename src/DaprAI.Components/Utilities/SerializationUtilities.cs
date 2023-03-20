using System.Text;
using System.Text.Json;

namespace DaprAI.Utilities;

internal static class SerializationUtilities
{
    public static T FromBytes<T>(ReadOnlySpan<byte> bytes)
    {
        var requestJson = Encoding.UTF8.GetString(bytes);
        var request = JsonSerializer.Deserialize<T>(requestJson);

        if (request is null)
        {
            throw new InvalidOperationException("Unable to deserialize request.");
        }

        return request;
    }

    public static byte[] ToBytes<T>(T value)
    {
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
    }
}
