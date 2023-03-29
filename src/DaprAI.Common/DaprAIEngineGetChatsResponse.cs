using System.Text.Json.Serialization;

namespace DaprAI;

public sealed record DaprAIEngineGetChatsChat(
    [property: JsonPropertyName("instanceId")]
    string InstanceId)
{
    [JsonPropertyName("history")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DaprChatHistory? History { get; init; }
}

public sealed record DaprAIEngineGetChatsResponse
{
    [JsonPropertyName("chats")]
    public DaprAIEngineGetChatsChat[] Chats { get; init; } = Array.Empty<DaprAIEngineGetChatsChat>();
}
