using Dapr.Client;

namespace DaprAI;

internal static class DaprClientExtensions
{
    public static Task<DaprCompletionResponse> CompleteTextAsync(this DaprClient daprClient, string component, DaprCompletionRequest request, DaprAIMetadata? metadata = null, CancellationToken cancellationToken = default)
    {
        var bindingMetadata = new Dictionary<string, string>
        {
            { Constants.Metadata.DaprPort, metadata?.DaprPort.ToString() ?? Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.GrpcPort) ?? String.Empty }
        };

        return daprClient.InvokeBindingAsync<DaprCompletionRequest, DaprCompletionResponse>(component, Constants.Operations.CompleteText, request, bindingMetadata, cancellationToken);
    }

    public static Task<DaprSummarizationResponse> SummarizeTextAsync(this DaprClient daprClient, string component, DaprSummarizationRequest request) =>
        daprClient.InvokeBindingAsync<DaprSummarizationRequest, DaprSummarizationResponse>(component, Constants.Operations.SummarizeText, request);
}
