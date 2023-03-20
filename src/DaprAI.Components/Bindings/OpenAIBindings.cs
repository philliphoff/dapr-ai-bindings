using System.Globalization;
using System.Net.Http.Headers;
using Dapr.PluggableComponents.Components;
using DaprAI.Utilities;

namespace DaprAI.Bindings;

internal sealed class OpenAIBindings : OpenAIBindingsBase
{
    private static readonly IReadOnlySet<string> ChatCompletionModels = new HashSet<string>
    {
        "gpt-4",
        "gpt-4-0314",
        "gpt-4-32k",
        "gpt-4-32k-0314",
        "gpt-3.5-turbo",
        "gpt-3.5-turbo-0301"
    };

    private static readonly IReadOnlySet<string> CompletionModels = new HashSet<string>
    {
        "text-davinci-003",
        "text-davinci-002",
        "text-curie-001",
        "text-babbage-001",
        "text-ada-001",
        "davinci",
        "curie",
        "babbage",
        "ada"
    };

    private string? model;

    protected override void OnAttachHeaders(HttpRequestHeaders headers)
    {
        base.OnAttachHeaders(headers);

        headers.Authorization = new AuthenticationHeaderValue("Bearer", this.Key);
    }

    protected override async Task<DaprCompletionResponse> OnCompleteAsync(DaprCompletionRequest completionRequest, CancellationToken cancellationToken)
    {
        if (this.IsChatCompletion())
        {
            var userMessage = new ChatCompletionMessage("user", completionRequest.Prompt);

            var response = await this.SendRequestAsync<ChatCompletionsRequest, ChatCompletionsResponse>(
                new ChatCompletionsRequest(
                    !String.IsNullOrEmpty(completionRequest.System)
                        ? new[] { new ChatCompletionMessage("system", completionRequest.System), userMessage }
                        : new[] { userMessage })
                {
                    Model = this.model,
                    Temperature = this.Temperature,
                    MaxTokens = this.MaxTokens,
                    TopP = this.TopP,
                },
                new Uri($"{this.Endpoint}/v1/chat/completions"),
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
            var response = await this.SendRequestAsync<CompletionsRequest, CompletionsResponse>(
                new CompletionsRequest(completionRequest.Prompt)
                {
                    Model = this.model,
                    Temperature = this.Temperature,
                    MaxTokens = this.MaxTokens,
                    TopP = this.TopP
                },
                new Uri($"{this.Endpoint}/v1/completions"),
                cancellationToken);

            var text = response.Choices.FirstOrDefault()?.Text;

            if (text == null)
            {
                throw new InvalidOperationException("No text was returned.");
            }

            return new DaprCompletionResponse(text);
        }
    }

    protected override async Task<DaprSummarizationResponse> OnSummarizeAsync(DaprSummarizationRequest summarizationRequest, CancellationToken cancellationToken)
    {
        string documentText = await SummarizationUtilities.GetDocumentText(summarizationRequest, cancellationToken);

        string summarizationInstructions = String.Format(
            CultureInfo.CurrentCulture,
            this.SummarizationInstructions ?? throw new InvalidOperationException("Missing required metadata property 'summarizationInstructions'."),
            documentText);

        string? summary;

        if (this.IsChatCompletion())
        {
            var response = await this.SendRequestAsync<ChatCompletionsRequest, ChatCompletionsResponse>(
                new ChatCompletionsRequest(
                    new[]
                    {
                        new ChatCompletionMessage("system", summarizationInstructions),
                        new ChatCompletionMessage("user", documentText)
                    })
                {
                    Model = this.model,
                    Temperature = this.Temperature,
                    MaxTokens = this.MaxTokens,
                    TopP = this.TopP,
                },
                new Uri($"{this.Endpoint}/v1/chat/completions"),
                cancellationToken);

            summary = response.Choices.FirstOrDefault()?.Message?.Content;
        }
        else
        {
            string prompt = $"Summarize the following text: {documentText}";

            var response = await this.SendRequestAsync<CompletionsRequest, CompletionsResponse>(
                new CompletionsRequest(summarizationInstructions)
                {
                    Model = this.model,
                    Temperature = this.Temperature,
                    MaxTokens = this.MaxTokens,
                    TopP = this.TopP
                },
                new Uri($"{this.Endpoint}/v1/completions"),
                cancellationToken);

            summary = response.Choices.FirstOrDefault()?.Text;
        }

        if (summary == null)
        {
            throw new InvalidOperationException("No summary was returned.");
        }

        return new DaprSummarizationResponse(summary);
    }

    protected override async Task OnInitAsync(MetadataRequest request, CancellationToken cancellationToken)
    {
        await base.OnInitAsync(request, cancellationToken);

        if (!request.Properties.TryGetValue("model", out this.model))
        {
            throw new InvalidOperationException("Missing required metadata property 'model'.");
        }
    }

    private bool IsChatCompletion()
    {
        if (this.model == null)
        {
            throw new InvalidOperationException("Missing required metadata property 'model'.");
        }
        else if (ChatCompletionModels.Contains(this.model))
        {
            return true;
        }
        else if (CompletionModels.Contains(this.model))
        {
            return false;
        }
        else
        {
            throw new InvalidOperationException($"Unrecognized model '{this.model}'.");
        }
    }
}
