using System.Net.Http.Headers;
using Dapr.PluggableComponents.Components;

namespace DaprAI.Bindings;

internal sealed class AzureOpenAIBindings : OpenAIBindingsBase
{
    private static readonly HttpClient HttpClient = new();

    private string? azureOpenAIDeployment;

    protected override async Task OnInitAsync(MetadataRequest request, CancellationToken cancellationToken = default)
    {
        await base.OnInitAsync(request, cancellationToken);

        if (!request.Properties.TryGetValue("deployment", out this.azureOpenAIDeployment))
        {
            throw new InvalidOperationException("Missing required metadata property 'deployment'.");
        }
    }

    protected override Uri GetUrl()
    {
        return new Uri($"{this.Endpoint}/openai/deployments/{this.azureOpenAIDeployment}/completions?api-version=2022-12-01");
    }

    protected override void AttachHeaders(HttpRequestHeaders headers)
    {
        headers.Add("api-key", this.Key);
    }

    protected override CompletionsRequest GetCompletionRequest(PromptRequest promptRequest)
    {
        return new CompletionsRequest(promptRequest.Prompt);
    }
}
