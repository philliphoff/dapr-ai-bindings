using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapr.PluggableComponents.Components;
using Dapr.PluggableComponents.Components.Bindings;

namespace DaprAI.Bindings;

internal sealed class AzureOpenAIBindings : IInputBinding, IOutputBinding
{
    private static readonly HttpClient HttpClient = new();

    private string? azureOpenAIDeployment;
    private string? azureOpenAIEndpoint;
    private string? azureOpenAIKey;

    #region IInputBinding Members

    public Task InitAsync(MetadataRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.Properties.TryGetValue("deployment", out this.azureOpenAIDeployment))
        {
            throw new InvalidOperationException("Missing required metadata property 'deployment'.");
        }

        if (!request.Properties.TryGetValue("endpoint", out this.azureOpenAIEndpoint))
        {
            throw new InvalidOperationException("Missing required metadata property 'endpoint'.");
        }
        
        if (!request.Properties.TryGetValue("key", out this.azureOpenAIKey))
        {
            throw new InvalidOperationException("Missing required metadata property 'key'.");
        }

        return Task.CompletedTask;
    }

    public Task ReadAsync(MessageDeliveryHandler<InputBindingReadRequest, InputBindingReadResponse> deliveryHandler, CancellationToken cancellationToken = default)
    {
        return Task.Delay(-1, cancellationToken);
    }

    #endregion

    #region IOutputBinding Members

    public Task<OutputBindingInvokeResponse> InvokeAsync(OutputBindingInvokeRequest request, CancellationToken cancellationToken = default)
    {
        return request.Operation switch
        {
            "prompt" => this.PromptAsync(request, cancellationToken),
            _ => throw new NotImplementedException(),
        };
    }

    public Task<string[]> ListOperationsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new[] { "prompt" });
    }

    #endregion

    private async Task<OutputBindingInvokeResponse> PromptAsync(OutputBindingInvokeRequest request, CancellationToken cancellationToken)
    {
        var url = new Uri($"{this.azureOpenAIEndpoint}/openai/deployments/{this.azureOpenAIDeployment}/completions?api-version=2022-12-01");

        var message = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(
                    new CompletionsPayload(
                        PromptRequest.FromBytes(request.Data.Span).Prompt
                    )
                ),
                Encoding.UTF8,
                "application/json"),
            Headers = {
                { "api-key", this.azureOpenAIKey }
            }
        };

        var response = await HttpClient.SendAsync(message, cancellationToken);

        response.EnsureSuccessStatusCode();

        var promptResponse = await response.Content.ReadFromJsonAsync<CompletionsResponse>(cancellationToken: cancellationToken);

        string? text = promptResponse?.Choices.FirstOrDefault()?.Text;

        if (String.IsNullOrEmpty(text))
        {
            throw new InvalidOperationException("No text returned from completion.");
        }

        return new OutputBindingInvokeResponse { Data = new PromptResponse(String.Join(' ', text)).ToBytes() };
    }

    private sealed record CompletionsPayload(
        [property: JsonPropertyName("prompt")]
        string Prompt
    );

    private sealed record CompletionsResponseChoice
    {
        [JsonPropertyName("text")]
        public string? Text { get; init; }

        [JsonPropertyName("index")]
        public int? Index { get; init; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; init; }

        [JsonPropertyName("logprobs")]
        public string? Logprobs { get; init; }
    }

    private sealed record CompletionsResponseUsage
    {
        [JsonPropertyName("completion_tokens")]
        public int? CompletionTokens { get; init; }

        [JsonPropertyName("prompt_tokens")]
        public int? PromptTokens { get; init; }

        [JsonPropertyName("total_tokens")]
        public int? TotalTokens { get; init; }
    }

    private sealed record CompletionsResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("object")]
        public string? Object { get; init; }

        [JsonPropertyName("created")]
        public int? Created { get; init; }

        [JsonPropertyName("model")]
        public string? Model { get; init; }

        [JsonPropertyName("choices")]
        public CompletionsResponseChoice[] Choices { get; init; } = Array.Empty<CompletionsResponseChoice>();

        [JsonPropertyName("usage")]
        public CompletionsResponseUsage? Usage  { get; init; }
    }
}
