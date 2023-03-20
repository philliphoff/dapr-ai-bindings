using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Dapr.PluggableComponents.Components;
using DaprAI.Utilities;

namespace DaprAI.Bindings;

internal sealed class AzureOpenAIBindings : OpenAIBindingsBase
{
    private static readonly ISet<string> ChatCompletionModels = new HashSet<string>
    {
        "gpt-35-turbo"
    };

    private string? azureOpenAIDeployment;
    private Lazy<Task<bool>> isChatCompletion;

    public AzureOpenAIBindings()
    {
        this.isChatCompletion = new Lazy<Task<bool>>(this.IsDeploymentChatCompletionModel);
    }

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

    protected override async Task<DaprCompletionResponse> OnCompleteAsync(DaprCompletionRequest promptRequest, CancellationToken cancellationToken)
    {
        var isChatCompletion = await this.isChatCompletion.Value;

        CompletionsRequest azureRequest;

        if (isChatCompletion)
        {
            string system = promptRequest.System != null ? $"<|im_start|>system\n{promptRequest.System}\n<|im_end|>\n" : String.Empty;
            string user = $"<|im_start|>user\n{promptRequest.Prompt}\n<|im_end|>\n";
            string prompt = $"{system}{user}<|im_start|>assistant\n";

            azureRequest = new CompletionsRequest(prompt)
            {
                Stop = new[] { "<|im_end|>" },
            };
        }
        else
        {
            azureRequest = new CompletionsRequest(promptRequest.Prompt);
        }

        azureRequest = azureRequest with
            {
                MaxTokens = this.MaxTokens,
                Temperature = this.Temperature,
                TopP = this.TopP
            };

        var response = await this.SendRequestAsync<CompletionsRequest, CompletionsResponse>(
            azureRequest,
            new Uri($"{this.Endpoint}/openai/deployments/{this.azureOpenAIDeployment}/completions?api-version=2022-12-01"),
            cancellationToken);

        var text = response.Choices.FirstOrDefault()?.Text;

        if (text == null)
        {
            throw new InvalidOperationException("No text was returned.");
        }

        return new DaprCompletionResponse(text);
    }

    protected override async Task<DaprSummarizationResponse> OnSummarizeAsync(DaprSummarizationRequest summarizationRequest, CancellationToken cancellationToken)
    {
        string documentText = await SummarizationUtilities.GetDocumentText(summarizationRequest, cancellationToken);

        string summarizationInstructions = String.Format(
            CultureInfo.CurrentCulture,
            this.SummarizationInstructions ?? throw new InvalidOperationException("Missing required metadata property 'summarizationInstructions'."),
            documentText);

        var isChatCompletion = await this.isChatCompletion.Value;

        CompletionsRequest azureRequest;

        if (isChatCompletion)
        {
            string system = $"<|im_start|>system\n{summarizationInstructions}\n<|im_end|>\n";
            string user = $"<|im_start|>user\n{documentText}\n<|im_end|>\n";
            string prompt = $"{system}{user}<|im_start|>assistant\n";

            azureRequest = new CompletionsRequest(prompt)
            {
                Stop = new[] { "<|im_end|>" },
            };
        }
        else
        {
            azureRequest = new CompletionsRequest(summarizationInstructions);
        }

        azureRequest = azureRequest with
            {
                MaxTokens = this.MaxTokens,
                Temperature = this.Temperature,
                TopP = this.TopP
            };

        var response = await this.SendRequestAsync<CompletionsRequest, CompletionsResponse>(
            azureRequest,
            new Uri($"{this.Endpoint}/openai/deployments/{this.azureOpenAIDeployment}/completions?api-version=2022-12-01"),
            cancellationToken);

        var summary = response.Choices.FirstOrDefault()?.Text;

        if (summary == null)
        {
            throw new InvalidOperationException("No summary was returned.");
        }

        return new DaprSummarizationResponse(summary);
    }

    private async Task<bool> IsDeploymentChatCompletionModel()
    {
        var response = await this.HttpClient.GetFromJsonAsync<GetDeploymentResponse>(
            new Uri($"{this.Endpoint}/openai/deployments/{this.azureOpenAIDeployment}?api-version=2022-12-01"));

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
