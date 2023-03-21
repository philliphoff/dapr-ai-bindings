using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapr.PluggableComponents.Components;
using Dapr.PluggableComponents.Components.Bindings;
using DaprAI.Utilities;

namespace DaprAI.Bindings;

internal abstract class OpenAIBindingsBase : IOutputBinding
{
    private sealed class HeadersProcessingHandler : MessageProcessingHandler
    {
        private readonly Action<HttpRequestHeaders> headers;

        public HeadersProcessingHandler(Action<HttpRequestHeaders> headers, HttpMessageHandler? innerHandler = null)
            : base(innerHandler ?? new HttpClientHandler())
        {
            this.headers = headers;
        }

        protected override HttpRequestMessage ProcessRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.headers(request.Headers);

            return request;
        }

        protected override HttpResponseMessage ProcessResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            return response;
        }
    }

    protected HttpClient HttpClient { get; private set; }

    protected OpenAIBindingsBase()
    {
        this.HttpClient = new HttpClient(new HeadersProcessingHandler(this.OnAttachHeaders));
    }

    protected string? Endpoint { get; private set; }

    protected string? Key { get; private set; }

    protected int? MaxTokens { get; private set; }

    protected string? SummarizationInstructions { get; private set; }

    protected decimal? Temperature { get; private set; }

    protected decimal? TopP { get; private set; }

    #region IOutputBinding Members

    public Task InitAsync(MetadataRequest request, CancellationToken cancellationToken = default)
    {
        return this.OnInitAsync(request, cancellationToken);
    }

    public Task<OutputBindingInvokeResponse> InvokeAsync(OutputBindingInvokeRequest request, CancellationToken cancellationToken = default)
    {
        return request.Operation switch
        {
            Constants.Operations.CompleteText => this.CompleteAsync(request, cancellationToken),
            Constants.Operations.SummarizeText => this.SummarizeAsync(request, cancellationToken),
            _ => throw new NotImplementedException(),
        };
    }

    public Task<string[]> ListOperationsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new[] { Constants.Operations.CompleteText, Constants.Operations.SummarizeText });
    }

    #endregion

    protected virtual Task OnInitAsync(MetadataRequest request, CancellationToken cancellationToken)
    {
        if (request.Properties.TryGetValue("endpoint", out var endpoint))
        {
            this.Endpoint = endpoint;
        }
        else
        {
            throw new InvalidOperationException("Missing required metadata property 'endpoint'.");
        }
        
        if (request.Properties.TryGetValue("key", out var key))
        {
            this.Key = key;
        }
        else 
        {
            throw new InvalidOperationException("Missing required metadata property 'key'.");
        }

        if (request.Properties.TryGetValue("maxTokens", out var maxTokens))
        {
            this.MaxTokens = Int32.Parse(maxTokens);
        }

        if (request.Properties.TryGetValue("summarizationInstructions", out var summarizationInstructions))
        {
            this.SummarizationInstructions = summarizationInstructions;
        }

        if (request.Properties.TryGetValue("temperature", out var temperature))
        {
            this.Temperature = Decimal.Parse(temperature);
        }

        if (request.Properties.TryGetValue("topP", out var topP))
        {
            this.TopP = Decimal.Parse(topP);
        }

        return Task.CompletedTask;
    }

    protected abstract Task<DaprCompletionResponse> OnCompleteAsync(DaprCompletionRequest completionRequest, CancellationToken cancellationToken);
    protected abstract Task<DaprSummarizationResponse> OnSummarizeAsync(DaprSummarizationRequest summarizeRequest, CancellationToken cancellationToken);
    
    protected virtual void OnAttachHeaders(HttpRequestHeaders headers)
    {
    }

    protected async Task<TResponse> SendRequestAsync<TRequest, TResponse>(TRequest request, Uri url, CancellationToken cancellationToken)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json")
        };

        var response = await this.HttpClient.SendAsync(message, cancellationToken);

        response.EnsureSuccessStatusCode();

        var promptResponse = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);

        if (promptResponse == null)
        {
            throw new InvalidOperationException("No response returned from completion.");
        }

        return promptResponse;
    }

    private async Task<OutputBindingInvokeResponse> CompleteAsync(OutputBindingInvokeRequest request, CancellationToken cancellationToken)
    {
        var completionRequest = SerializationUtilities.FromBytes<DaprCompletionRequest>(request.Data.Span);

        var completionResponse = await this.OnCompleteAsync(completionRequest, cancellationToken);

        return new OutputBindingInvokeResponse { Data = SerializationUtilities.ToBytes(completionResponse) };
    }

    private async Task<OutputBindingInvokeResponse> SummarizeAsync(OutputBindingInvokeRequest request, CancellationToken cancellationToken)
    {
        var summarizationRequest = SerializationUtilities.FromBytes<DaprSummarizationRequest>(request.Data.Span);

        var summarizationResponse = await this.OnSummarizeAsync(summarizationRequest, cancellationToken);

        return new OutputBindingInvokeResponse { Data = SerializationUtilities.ToBytes(summarizationResponse) };
    }

    protected abstract record CompletionsRequestBase
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

        [JsonPropertyName("stop")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Stop { get; init; }
    }

    protected sealed record CompletionsRequest(
        [property: JsonPropertyName("prompt")]
        string Prompt) : CompletionsRequestBase;

    protected sealed record ChatCompletionsRequest(
        [property: JsonPropertyName("messages")]
        ChatCompletionMessage[] Messages) : CompletionsRequestBase;

    protected sealed record ChatCompletionMessage(
        [property: JsonPropertyName("role")]
        string Role,

        [property: JsonPropertyName("content")]
        string Content);

    protected abstract record CompletionsResponseChoiceBase
    {
        [JsonPropertyName("index")]
        public int? Index { get; init; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; init; }

        [JsonPropertyName("logprobs")]
        public string? Logprobs { get; init; }
    }

    protected sealed record CompletionsResponseChoice : CompletionsResponseChoiceBase
    {
        [JsonPropertyName("text")]
        public string? Text { get; init; }
    }

    protected sealed record ChatCompletionsResponseChoice : CompletionsResponseChoiceBase
    {
        [JsonPropertyName("message")]
        public ChatCompletionsResponseMessage? Message { get; init; }
    }

    protected sealed record ChatCompletionsResponseMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; init; }

        [JsonPropertyName("role")]
        public string? Role { get; init; }
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

    protected sealed record CompletionsResponseBase
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("object")]
        public string? Object { get; init; }

        [JsonPropertyName("created")]
        public int? Created { get; init; }

        [JsonPropertyName("model")]
        public string? Model { get; init; }

        [JsonPropertyName("usage")]
        public CompletionsResponseUsage? Usage  { get; init; }
    }

    protected sealed record CompletionsResponse : CompletionsRequestBase
    {
        [JsonPropertyName("choices")]
        public CompletionsResponseChoice[] Choices { get; init; } = Array.Empty<CompletionsResponseChoice>();
    }

    protected sealed record ChatCompletionsResponse : CompletionsRequestBase
    {
        [JsonPropertyName("choices")]
        public ChatCompletionsResponseChoice[] Choices { get; init; } = Array.Empty<ChatCompletionsResponseChoice>();
    }
}
