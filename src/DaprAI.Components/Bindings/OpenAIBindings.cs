using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
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

    private sealed record ChatHistoryItem(
        [property: JsonPropertyName("role")]
        string Role,

        [property: JsonPropertyName("message")]
        string Message);

    private sealed record ChatHistory(
        [property: JsonPropertyName("items")]
        ChatHistoryItem[] Items);

    protected override async Task<DaprCompletionResponse> OnCompleteAsync(DaprCompletionRequest completionRequest, CompletionContext context, CancellationToken cancellationToken)
    {
        if (this.IsChatCompletion())
        {
            string instanceId = completionRequest.InstanceId ?? Guid.NewGuid().ToString();
            string key = $"ai-chat-history-{instanceId}";

            var history = await context.DaprClient.GetStateAsync<ChatHistory>(this.StoreName, key, cancellationToken: cancellationToken);

            history ??= new ChatHistory(Array.Empty<ChatHistoryItem>());

            var messages = new List<ChatCompletionMessage>(history.Items.Select(item => new ChatCompletionMessage(item.Role, item.Message)));

            if (!messages.Any() && !String.IsNullOrEmpty(completionRequest.System))
            {
                messages.Add(new ChatCompletionMessage("system", completionRequest.System));
            }

            messages.Add(new ChatCompletionMessage("user", completionRequest.Prompt));

            var response = await this.SendRequestAsync<ChatCompletionsRequest, ChatCompletionsResponse>(
                new ChatCompletionsRequest(messages.ToArray())
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

            messages.Add(new ChatCompletionMessage("assistant", content));

            history = history with
            {
                Items = messages.Select(item => new ChatHistoryItem(item.Role, item.Content)).ToArray()
            };

            await context.DaprClient.SaveStateAsync(this.StoreName, key, history, cancellationToken: cancellationToken);

            return new DaprCompletionResponse(instanceId, content);
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

            return new DaprCompletionResponse(null, text);
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
