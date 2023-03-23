namespace DaprAI;

public static class Constants
{
    public static class EnvironmentVariables
    {
        public const string GrpcPort = "DAPR_GRPC_PORT";
    }

    public static class Metadata{
        public const string DaprPort = "daprPort";
    }

    public static class Operations
    {
        public const string CompleteText = "completeText";

        public const string SummarizeText = "summarizeText";
    }
}
