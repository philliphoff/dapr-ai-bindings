using System.Text.Json.Serialization;

namespace DaprAI;

public sealed record DaprAIEngineGetChatRequest(
    [property: JsonPropertyName("instanceId")]
    string InstanceId);
