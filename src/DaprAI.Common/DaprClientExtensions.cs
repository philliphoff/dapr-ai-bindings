using Dapr.Client;

namespace DaprAI;

public static class DaprClientExtensions
{
    public static Task AIEngineCreateChatAsync(this DaprClient daprClient, DaprAIEngineCreateChatRequest request, CancellationToken cancellationToken = default) =>
        AIEngineCreateChatAsync(daprClient, request, null, cancellationToken);

    public static Task AIEngineCreateChatAsync(this DaprClient daprClient, DaprAIEngineCreateChatRequest request, DaprAIMetadata? metadata, CancellationToken cancellationToken = default) =>
        daprClient.InvokeBindingAsync<DaprAIEngineCreateChatRequest>(GetAIEngineName(metadata), Constants.Operations.CreateChat, request, GetBindingMetadata(metadata), cancellationToken);

    public static Task<DaprAIEngineCompletionResponse> AIEngineCompleteTextAsync(this DaprClient daprClient, DaprAIEngineCompletionRequest request, CancellationToken cancellationToken = default) =>
        AIEngineCompleteTextAsync(daprClient, request, null, cancellationToken);

    public static Task<DaprAIEngineCompletionResponse> AIEngineCompleteTextAsync(this DaprClient daprClient, DaprAIEngineCompletionRequest request, DaprAIMetadata? metadata, CancellationToken cancellationToken = default) =>
        daprClient.InvokeBindingAsync<DaprAIEngineCompletionRequest, DaprAIEngineCompletionResponse>(GetAIEngineName(metadata), Constants.Operations.CompleteText, request, GetBindingMetadata(metadata), cancellationToken);

    public static Task<DaprSummarizationResponse> AIEngineSummarizeTextAsync(this DaprClient daprClient, DaprSummarizationRequest request, CancellationToken cancellationToken = default) =>
        AIEngineSummarizeTextAsync(daprClient, request, null, cancellationToken);

    public static Task<DaprSummarizationResponse> AIEngineSummarizeTextAsync(this DaprClient daprClient, DaprSummarizationRequest request, DaprAIMetadata? metadata, CancellationToken cancellationToken = default) =>
        daprClient.InvokeBindingAsync<DaprSummarizationRequest, DaprSummarizationResponse>(GetAIEngineName(metadata), Constants.Operations.SummarizeText, request, GetBindingMetadata(metadata), cancellationToken);

    public static Task AIEngineTerminateChatAsync(this DaprClient daprClient, DaprAIEngineTerminateChatRequest request, CancellationToken cancellationToken = default) =>
        AIEngineTerminateChatAsync(daprClient, request, null, cancellationToken);

    public static Task AIEngineTerminateChatAsync(this DaprClient daprClient, DaprAIEngineTerminateChatRequest request, DaprAIMetadata? metadata, CancellationToken cancellationToken = default) =>
        daprClient.InvokeBindingAsync<DaprAIEngineTerminateChatRequest>(GetAIEngineName(metadata), Constants.Operations.TerminateChat, request, GetBindingMetadata(metadata), cancellationToken);

    public static Task<DaprCompletionResponse> CompleteTextAsync(this DaprClient daprClient, string component, DaprCompletionRequest request, CancellationToken cancellationToken = default) =>
        daprClient.InvokeBindingAsync<DaprCompletionRequest, DaprCompletionResponse>(component, Constants.Operations.CompleteText, request, cancellationToken: cancellationToken);

    public static Task<DaprSummarizationResponse> SummarizeTextAsync(this DaprClient daprClient, string component, DaprSummarizationRequest request, CancellationToken cancellationToken = default) =>
        daprClient.InvokeBindingAsync<DaprSummarizationRequest, DaprSummarizationResponse>(component, Constants.Operations.SummarizeText, request, cancellationToken: cancellationToken);

    private static IReadOnlyDictionary<string, string> GetBindingMetadata(DaprAIMetadata? metadata) =>
        new Dictionary<string, string>
        {
            { Constants.Metadata.DaprGrpcPort, metadata?.DaprGrpcPort.ToString() ?? Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.DaprGrpcPort) ?? String.Empty }
        };

    private static string GetAIEngineName(DaprAIMetadata? metadata) =>
        metadata?.AIEngineName ?? "ai-engine";
}
