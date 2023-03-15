using System.Net.Http.Headers;

namespace DaprAI.Bindings;

internal sealed class ChatGptBindings : OpenAIBindingsBase
{
    protected override Uri GetUrl()
    {
        return new Uri($"{this.Endpoint}/v1/completions");
    }

    protected override void AttachHeaders(HttpRequestHeaders headers)
    {
        headers.Add("Authorization", $"Bearer {this.Key}");
    }

    override protected CompletionsRequest GetCompletionRequest(PromptRequest promptRequest)
    {
        return new CompletionsRequest(promptRequest.Prompt)
        {
            Model = "text-davinci-003",
            Temperature = 0.9m,
            MaxTokens = 64,
            TopP = 1.0m,
            FrequencyPenalty = 0.0m,
            PresencePenalty = 0.0m
        };
    }
}
