using System.Text.Json.Serialization;

namespace DaprAI;

public sealed record DaprChatHistoryItem(
    [property: JsonPropertyName("role")]
    string Role,

    [property: JsonPropertyName("message")]
    string Message);

public sealed record DaprChatHistory(
    [property: JsonPropertyName("items")]
    DaprChatHistoryItem[] Items);
