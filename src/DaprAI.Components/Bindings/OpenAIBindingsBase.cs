using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapr.Client;
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

    protected Uri? Endpoint { get; private set; }

    protected string? Key { get; private set; }

    protected int? MaxTokens { get; private set; }

    protected string? StoreName { get; private set; }

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

    protected virtual void OnAttachHeaders(HttpRequestHeaders headers)
    {
    }

    protected abstract Uri GetCompletionsUrl(Uri baseEndpoint, bool chatCompletionsUrl);

    protected virtual Task<bool> IsChatCompletionModelAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    protected virtual ChatCompletionsRequest UpdateChatCompletionsRequest(ChatCompletionsRequest request)
    {
        return request;
    }

    protected virtual CompletionsRequest UpdateCompletionsRequest(CompletionsRequest request)
    {
        return request;
    }

    protected virtual Task OnInitAsync(MetadataRequest request, CancellationToken cancellationToken)
    {
        if (request.Properties.TryGetValue("endpoint", out var endpoint))
        {
            this.Endpoint = new Uri(endpoint);
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

        if (request.Properties.TryGetValue("storeName", out var storeName))
        {
            this.StoreName = storeName;
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

    private async Task<DaprCompletionResponse> OnCompleteAsync(DaprCompletionRequest completionRequest, CancellationToken cancellationToken)
    {
        if (await this.IsChatCompletionModelAsync(cancellationToken))
        {
            var messages = new List<ChatCompletionMessage>(completionRequest.History?.Items.Select(item => new ChatCompletionMessage(item.Role, item.Message)) ?? Enumerable.Empty<ChatCompletionMessage>());

            messages.Add(new ChatCompletionMessage("user", completionRequest.UserPrompt));

            var request = this.UpdateChatCompletionsRequest(
                new ChatCompletionsRequest(messages.ToArray())
                {
                    Temperature = this.Temperature,
                    MaxTokens = this.MaxTokens,
                    TopP = this.TopP,
                });

            var response = await this.SendRequestAsync<ChatCompletionsRequest, ChatCompletionsResponse>(
                request,
                this.GetCompletionsUrl(this.Endpoint!, true),
                cancellationToken);

            var content = response.Choices.FirstOrDefault()?.Message?.Content;

            if (content == null)
            {
                throw new InvalidOperationException("No chat content was returned.");
            }

            return new DaprCompletionResponse(content);
        }
        else
        {
            var request = this.UpdateCompletionsRequest(
                new CompletionsRequest(completionRequest.UserPrompt)
                {
                    Temperature = this.Temperature,
                    MaxTokens = this.MaxTokens,
                    TopP = this.TopP
                });

            var response = await this.SendRequestAsync<CompletionsRequest, CompletionsResponse>(
                request,
                this.GetCompletionsUrl(this.Endpoint!, false),
                cancellationToken);

            var text = response.Choices.FirstOrDefault()?.Text;

            if (text == null)
            {
                throw new InvalidOperationException("No text was returned.");
            }

            return new DaprCompletionResponse(text);
        }
    }

    private async Task<DaprSummarizationResponse> OnSummarizeAsync(DaprSummarizationRequest summarizationRequest, CancellationToken cancellationToken)
    {
        string documentText = await SummarizationUtilities.GetDocumentText(summarizationRequest, cancellationToken);

        string summarizationInstructions = String.Format(
            CultureInfo.CurrentCulture,
            this.SummarizationInstructions ?? throw new InvalidOperationException("Missing required metadata property 'summarizationInstructions'."),
            documentText);

        string? summary;

        if (await this.IsChatCompletionModelAsync(cancellationToken))
        {
            var request = this.UpdateChatCompletionsRequest(
                new ChatCompletionsRequest(
                    new[]
                    {
                        new ChatCompletionMessage("system", summarizationInstructions),
                        new ChatCompletionMessage("user", documentText)
                    })
                {
                    Temperature = this.Temperature,
                    MaxTokens = this.MaxTokens,
                    TopP = this.TopP,
                });

            var response = await this.SendRequestAsync<ChatCompletionsRequest, ChatCompletionsResponse>(
                request,
                this.GetCompletionsUrl(this.Endpoint!, true),
                cancellationToken);

            summary = response.Choices.FirstOrDefault()?.Message?.Content;
        }
        else
        {
            var request = this.UpdateCompletionsRequest(
                new CompletionsRequest(summarizationInstructions)
                {
                    Temperature = this.Temperature,
                    MaxTokens = this.MaxTokens,
                    TopP = this.TopP
                });

            var response = await this.SendRequestAsync<CompletionsRequest, CompletionsResponse>(
                request,
                this.GetCompletionsUrl(this.Endpoint!, false),
                cancellationToken);

            summary = response.Choices.FirstOrDefault()?.Text;
        }

        if (summary == null)
        {
            throw new InvalidOperationException("No summary was returned.");
        }

        return new DaprSummarizationResponse(summary);
    }
    
    private async Task<TResponse> SendRequestAsync<TRequest, TResponse>(TRequest request, Uri url, CancellationToken cancellationToken)
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
