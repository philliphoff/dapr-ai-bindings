using Dapr.Client;

namespace DaprAI;

internal static class DaprClientExtensions
{
    public static Task<PromptResponse> PromptAIAsync(this DaprClient daprClient, string component, PromptRequest request) =>
        daprClient.InvokeBindingAsync<PromptRequest, PromptResponse>(component, "prompt", request);

    public static Task<PromptResponse> SummarizeAIAsync(this DaprClient daprClient, string component, PromptRequest request) =>
        daprClient.InvokeBindingAsync<PromptRequest, PromptResponse>(component, "summarize", request);
}
