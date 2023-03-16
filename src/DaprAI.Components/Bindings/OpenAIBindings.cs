using System.Net.Http.Headers;
using Dapr.PluggableComponents.Components;

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

    protected override async Task<DaprCompletionResponse> OnPromptAsync(DaprCompletionRequest promptRequest, CancellationToken cancellationToken)
    {
        if (this.IsChatCompletion())
        {
            var userMessage = new ChatCompletionMessage("user", promptRequest.Prompt);

            var response = await this.SendRequestAsync<ChatCompletionsRequest, ChatCompletionsResponse>(
                new ChatCompletionsRequest(
                    !String.IsNullOrEmpty(promptRequest.System)
                        ? new[] { new ChatCompletionMessage("system", promptRequest.System), userMessage }
                        : new[] { userMessage })
                {
                    Model = this.model,
                    Temperature = 0.9m,
                    MaxTokens = 64,
                    TopP = 1.0m,
                    FrequencyPenalty = 0.0m,
                    PresencePenalty = 0.0m
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
                new CompletionsRequest(promptRequest.Prompt)
                {
                    Model = this.model,
                    Temperature = 0.9m,
                    MaxTokens = 64,
                    TopP = 1.0m,
                    FrequencyPenalty = 0.0m,
                    PresencePenalty = 0.0m
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
