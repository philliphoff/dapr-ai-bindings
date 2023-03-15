using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapr.PluggableComponents.Components;
using Dapr.PluggableComponents.Components.Bindings;

namespace DaprAI.Bindings;

internal abstract class OpenAIBindingsBase : IOutputBinding
{
    private static readonly HttpClient HttpClient = new();

    private string? azureOpenAIEndpoint;
    private string? azureOpenAIKey;

    protected string? Endpoint => this.azureOpenAIEndpoint;

    protected string? Key => this.azureOpenAIKey;

    #region IOutputBinding Members

    public Task InitAsync(MetadataRequest request, CancellationToken cancellationToken = default)
    {
        return this.OnInitAsync(request, cancellationToken);
    }

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

    protected virtual Task OnInitAsync(MetadataRequest request, CancellationToken cancellationToken)
    {
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

    protected abstract Uri GetUrl();
    protected abstract void AttachHeaders(HttpRequestHeaders headers);
    protected abstract CompletionsRequest GetCompletionRequest(PromptRequest promptRequest);

    private async Task<OutputBindingInvokeResponse> PromptAsync(OutputBindingInvokeRequest request, CancellationToken cancellationToken)
    {
        var promptRequest = PromptRequest.FromBytes(request.Data.Span);

        var message = new HttpRequestMessage(HttpMethod.Post, this.GetUrl())
        {
            Content = new StringContent(
                JsonSerializer.Serialize(this.GetCompletionRequest(promptRequest)),
                Encoding.UTF8,
                "application/json")
        };

        this.AttachHeaders(message.Headers);

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

    protected sealed record CompletionsRequest(
        [property: JsonPropertyName("prompt")]
        string Prompt)
    {
        [JsonPropertyName("model")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Model { get; init; }

        [JsonPropertyName("temperature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? Temperature { get; init; }

        [JsonPropertyName("max_tokens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxTokens { get; init; }

        [JsonPropertyName("top_p")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? TopP { get; init; }

        [JsonPropertyName("frequency_penalty")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? FrequencyPenalty { get; init; }

        [JsonPropertyName("presence_penalty")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? PresencePenalty { get; init; }
    }

    protected sealed record CompletionsResponseChoice
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

    protected sealed record CompletionsResponseUsage
    {
        [JsonPropertyName("completion_tokens")]
        public int? CompletionTokens { get; init; }

        [JsonPropertyName("prompt_tokens")]
        public int? PromptTokens { get; init; }

        [JsonPropertyName("total_tokens")]
        public int? TotalTokens { get; init; }
    }

    protected sealed record CompletionsResponse
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
