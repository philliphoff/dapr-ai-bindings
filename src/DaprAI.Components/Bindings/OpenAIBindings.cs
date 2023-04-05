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

    protected override Uri GetCompletionsUrl(bool chatCompletionsUrl)
    {
        return new Uri(
            this.Endpoint!,
            chatCompletionsUrl ? "v1/chat/completions" : "v1/completions");
    }

    protected override Task<bool> IsChatCompletionModelAsync(CancellationToken cancellationToken)
    {
        if (this.model == null)
        {
            throw new InvalidOperationException("Missing required metadata property 'model'.");
        }
        else if (ChatCompletionModels.Contains(this.model))
        {
            return Task.FromResult(true);
        }
        else if (CompletionModels.Contains(this.model))
        {
            return Task.FromResult(false);
        }
        else
        {
            throw new InvalidOperationException($"Unrecognized model '{this.model}'.");
        }
    }

    protected override void OnAttachHeaders(HttpRequestHeaders headers)
    {
        base.OnAttachHeaders(headers);

        headers.Authorization = new AuthenticationHeaderValue("Bearer", this.Key);
    }

    protected override async Task OnInitAsync(MetadataRequest request, CancellationToken cancellationToken)
    {
        await base.OnInitAsync(request, cancellationToken);

        if (!request.Properties.TryGetValue("model", out this.model))
        {
            throw new InvalidOperationException("Missing required metadata property 'model'.");
        }
    }

    protected override ChatCompletionsRequest UpdateChatCompletionsRequest(ChatCompletionsRequest request)
    {
        return request with { Model = this.model };
    }

    protected override CompletionsRequest UpdateCompletionsRequest(CompletionsRequest request)
    {
        return request with { Model = this.model };
    }
}
