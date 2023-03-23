namespace DaprAI;

public static class Constants
{
    public static class EnvironmentVariables
    {
        public const string DaprGrpcPort = "DAPR_GRPC_PORT";
    }

    public static class Metadata{
        public const string DaprGrpcPort = "daprGrpcPort";
    }

    public static class Operations
    {
        public const string CompleteText = "completeText";

        public const string CreateChat = "createChat";

        public const string SummarizeText = "summarizeText";

        public const string TerminateChat = "terminateChat";
    }
}
