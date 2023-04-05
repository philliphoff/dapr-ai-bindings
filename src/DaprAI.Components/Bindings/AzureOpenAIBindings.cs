using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Dapr.PluggableComponents.Components;

namespace DaprAI.Bindings;

internal sealed class AzureOpenAIBindings : OpenAIBindingsBase
{
    private const string ApiVersion = "?api-version=2023-03-15-preview";

    private static readonly ISet<string> ChatCompletionModels = new HashSet<string>
    {
        "gpt-35-turbo",
        "gpt-4",
        "gpt-4-32k"
    };

    private string? deployment;
    private Lazy<Task<bool>> isChatCompletion;

    public AzureOpenAIBindings()
    {
        this.isChatCompletion = new Lazy<Task<bool>>(this.IsDeploymentChatCompletionModel);
    }

    protected override Uri GetCompletionsUrl(bool chatCompletionsUrl)
    {
        return new Uri(
            this.Endpoint!,
            $"openai/deployments/{this.deployment}/{(chatCompletionsUrl ? "chat/" : "")}completions{ApiVersion}");
    }

    protected override Task<bool> IsChatCompletionModelAsync(CancellationToken cancellationToken)
    {
        return this.isChatCompletion.Value;
    }

    protected override async Task OnInitAsync(MetadataRequest request, CancellationToken cancellationToken = default)
    {
        await base.OnInitAsync(request, cancellationToken);

        if (!request.Properties.TryGetValue("deployment", out this.deployment))
        {
            throw new InvalidOperationException("Missing required metadata property 'deployment'.");
        }
    }

    protected override void OnAttachHeaders(HttpRequestHeaders headers)
    {
        base.OnAttachHeaders(headers);

        headers.Add("api-key", this.Key);
    }

    private async Task<bool> IsDeploymentChatCompletionModel()
    {
        var response = await this.HttpClient.GetFromJsonAsync<GetDeploymentResponse>(new Uri(this.Endpoint!, $"openai/deployments/{this.deployment}{ApiVersion}"));

        return response?.Model != null && ChatCompletionModels.Contains(response.Model);
    }

    private sealed record DeploymentScaleSettings
    {
        [JsonPropertyName("scale_type")]
        public string? ScaleType { get; init; }
    }

    private sealed record GetDeploymentResponse
    {
        [JsonPropertyName("scale_settings")]
        public DeploymentScaleSettings? ScaleSettings { get; init; }

        [JsonPropertyName("model")]
        public string? Model { get; init; }

        [JsonPropertyName("owner")]
        public string? Owner { get; init; }

        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("created_at")]
        public int? CreatedAt { get; init; }

        [JsonPropertyName("updated_at")]
        public int? UpdatedAt { get; init; }

        [JsonPropertyName("object")]
        public string? Object { get; init; }
    }
}
