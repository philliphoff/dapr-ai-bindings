using System.Text.Json.Serialization;

namespace DaprAI;

public sealed record DaprAIEngineTerminateChatRequest(
    [property: JsonPropertyName("instanceId")]
    string InstanceId);
