namespace DaprAI;

public sealed record DaprAIMetadata(int DaprGrpcPort)
{
    public string? AIEngineName { get; init; }
}
