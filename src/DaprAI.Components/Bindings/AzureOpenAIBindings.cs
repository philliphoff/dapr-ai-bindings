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

    protected override void OnAttachHeaders(HttpRequestHeaders headers)
    {
        base.OnAttachHeaders(headers);

        headers.Add("api-key", this.Key);
    }

    protected override async Task<DaprCompletionResponse> OnPromptAsync(DaprCompletionRequest promptRequest, CancellationToken cancellationToken)
    {
        var response = await this.SendRequestAsync<CompletionsRequest, CompletionsResponse>(
            new CompletionsRequest(promptRequest.Prompt),
            new Uri($"{this.Endpoint}/openai/deployments/{this.azureOpenAIDeployment}/completions?api-version=2022-12-01"),
            cancellationToken);

        var text = response.Choices.FirstOrDefault()?.Text;

        if (text == null)
        {
            throw new InvalidOperationException("No text was returned.");
        }

        return new DaprCompletionResponse(text);
    }
}
