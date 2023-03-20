using Dapr.Client;

namespace DaprAI;

internal static class DaprClientExtensions
{
    public static Task<DaprCompletionResponse> CompleteTextAsync(this DaprClient daprClient, string component, DaprCompletionRequest request) =>
        daprClient.InvokeBindingAsync<DaprCompletionRequest, DaprCompletionResponse>(component, Constants.Operations.CompleteText, request);

    public static Task<DaprSummarizationResponse> SummarizeTextAsync(this DaprClient daprClient, string component, DaprSummarizationRequest request) =>
        daprClient.InvokeBindingAsync<DaprSummarizationRequest, DaprSummarizationResponse>(component, Constants.Operations.SummarizeText, request);
}
