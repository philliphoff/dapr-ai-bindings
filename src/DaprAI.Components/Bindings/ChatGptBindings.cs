using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapr.PluggableComponents.Components;
using Dapr.PluggableComponents.Components.Bindings;

namespace DaprAI.Bindings;

internal sealed class ChatGptBindings : IInputBinding, IOutputBinding
{
    private string? openApiEndpoint;
    private string? openApiKey;

    #region IInputBinding Members

    public Task InitAsync(MetadataRequest request, CancellationToken cancellationToken = default)
    {
        request.Properties.TryGetValue("open-api-endpoint", out this.openApiEndpoint);
        request.Properties.TryGetValue("open-api-key", out this.openApiKey);

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
            "prompt" => this.PromptAsync(request),
            _ => throw new NotImplementedException(),
        };
    }

    public Task<string[]> ListOperationsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new[] { "prompt" });
    }

    #endregion

    private async Task<OutputBindingInvokeResponse> PromptAsync(OutputBindingInvokeRequest request)
    {
        var promptRequest = PromptRequest.FromBytes(request.Data.Span);

        var completion = await this.OpenAICreateCompletion("text-davinci-003", promptRequest.Prompt, 0.9m, 64);

        var choice = completion.choices[0];

        return new OutputBindingInvokeResponse { Data = new PromptResponse(choice.text).ToBytes() };
    }

    async Task<CompletionResponse> OpenAICreateCompletion(string model, string prompt, decimal temperature, int max_tokens)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        // Adding app id as part of the header
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {this.openApiKey}");

        var completion = CompletionRequest.CreateDefaultCompletionRequest(model, prompt, temperature, max_tokens);
        
        var completionJson = JsonSerializer.Serialize<CompletionRequest>(completion);

        var content = new StringContent(completionJson, Encoding.UTF8, "application/json");

        var baseUrl = $"{this.openApiEndpoint}/v1/completions";
        var response = await client.PostAsync(baseUrl, content);

        response.EnsureSuccessStatusCode();

        var completionResponse = JsonSerializer.Deserialize<CompletionResponse>(response.Content.ReadAsStream());

        return completionResponse;
    }

    public record CompletionRequest(string model, 
                                    string prompt,
                                    decimal temperature,
                                    int max_tokens,
                                    decimal top_p,
                                    decimal frequency_penalty,
                                    decimal presence_penalty
                                    )
    {

        public static CompletionRequest CreateDefaultCompletionRequest(string model, string prompt, decimal temperature, int max_tokens) {
            return new CompletionRequest(model, prompt, temperature, max_tokens, 1.0m, 0.0m, 0.0m);
        }

        public static CompletionRequest CreateDefaultCompletionRequest(string prompt) {
            return new CompletionRequest("text-davinci-003", prompt, 0.9m, 64, 1.0m, 0.0m, 0.0m);
        }

        public static CompletionRequest CreateDefaultCompletionRequest() {
            return CompletionRequest.CreateDefaultCompletionRequest(prompt: "");
        }
    }

    public record CompletionResponse(string id, 
                                    [property: JsonPropertyName("object")] string _object,
                                    int created,    
                                    string model,
                                    Choice[] choices,
                                    Usage usage
                                    );

    public record Choice(string text, int index, string logprobs, string finish_reason);

    public record Usage(int prompt_tokens, int completion_tokens, int total_tokens);
}
