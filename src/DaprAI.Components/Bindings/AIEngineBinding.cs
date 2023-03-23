using Dapr.Client;
using Dapr.PluggableComponents.Components;
using Dapr.PluggableComponents.Components.Bindings;
using DaprAI.Utilities;

namespace DaprAI.Bindings;

internal sealed class AiEngineBinding : IOutputBinding
{
    private string? aiName;
    private string? storeName;

    #region IOutputBinding Members

    public Task InitAsync(MetadataRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.Properties.TryGetValue("aiName", out this.aiName))
        {
            throw new InvalidOperationException("The aiName metadata property is required.");
        }

        if (!request.Properties.TryGetValue("storeName", out this.storeName))
        {
            throw new InvalidOperationException("The storeName metadata property is required.");
        }

        return Task.CompletedTask;
    }

    public Task<OutputBindingInvokeResponse> InvokeAsync(OutputBindingInvokeRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.Metadata.TryGetValue(Constants.Metadata.DaprGrpcPort, out var daprPort))
        {
            throw new InvalidOperationException("Missing required metadata property 'daprGrpcPort'.");
        }

        var daprClient = new DaprClientBuilder().UseGrpcEndpoint($"http://127.0.0.1:{daprPort}").Build();
        var context = new AIEngineContext(daprClient);

        return request.Operation switch
        {
            Constants.Operations.CompleteText => this.CompleteTextAsync(request, context, cancellationToken),
            Constants.Operations.CreateChat => this.CreateChatAsync(request, context, cancellationToken),
            Constants.Operations.SummarizeText => this.SummarizeTextAsync(request, context, cancellationToken),
            Constants.Operations.TerminateChat => this.TerminateChatAsync(request, context, cancellationToken),
            _ => throw new NotImplementedException(),
        };
    }

    public Task<string[]> ListOperationsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            new[]
            {
                Constants.Operations.CompleteText,
                Constants.Operations.CreateChat,
                Constants.Operations.SummarizeText,
                Constants.Operations.TerminateChat
            });
    }

    #endregion

    private async Task<OutputBindingInvokeResponse> CompleteTextAsync(OutputBindingInvokeRequest request, AIEngineContext context, CancellationToken cancellationToken)
    {
        var engineCompletionRequest = SerializationUtilities.FromBytes<DaprAIEngineCompletionRequest>(request.Data.Span);

        DaprChatHistory history;

        if (!String.IsNullOrEmpty(engineCompletionRequest.InstanceId))
        {
            string key = CreateKey(engineCompletionRequest.InstanceId);

            history = await context.DaprClient.GetStateAsync<DaprChatHistory>(this.storeName!, key, cancellationToken: cancellationToken);
        }
        else
        {
            history = new DaprChatHistory(Array.Empty<DaprChatHistoryItem>());
        }

        var completionRequest = new DaprCompletionRequest(engineCompletionRequest.UserPrompt)
        {
            History = history
        };

        var response = await context.DaprClient.CompleteTextAsync(this.aiName!, completionRequest, cancellationToken: cancellationToken);

        if (!String.IsNullOrEmpty(engineCompletionRequest.InstanceId))
        {
            history = history with
            {
                Items =
                    history
                        .Items
                        .Concat(
                        new[]
                        {
                            new DaprChatHistoryItem("user", engineCompletionRequest.UserPrompt),
                            new DaprChatHistoryItem("assistant", response.AssistantResponse)
                        })
                        .ToArray()
            };

            string key = CreateKey(engineCompletionRequest.InstanceId);

            await context.DaprClient.SaveStateAsync<DaprChatHistory>(this.storeName!, key, history, cancellationToken: cancellationToken);
        }

        return new OutputBindingInvokeResponse { Data = SerializationUtilities.ToBytes(response) };
    }

    private async Task<OutputBindingInvokeResponse> CreateChatAsync(OutputBindingInvokeRequest request, AIEngineContext context, CancellationToken cancellationToken)
    {
        var createRequest = SerializationUtilities.FromBytes<DaprAIEngineCreateChatRequest>(request.Data.Span);

        string key = CreateKey(createRequest.InstanceId);

        var history = await context.DaprClient.GetStateAsync<DaprChatHistory>(this.storeName!, key, cancellationToken: cancellationToken);

        if (history is not null)
        {
            throw new InvalidOperationException("The chat instance already exists.");
        }

        history = new DaprChatHistory(
            !String.IsNullOrEmpty(createRequest.SystemInstructions)
                ? new[] { new DaprChatHistoryItem("system", createRequest.SystemInstructions) }
                : Array.Empty<DaprChatHistoryItem>());

        await context.DaprClient.SaveStateAsync(this.storeName!, key, history, cancellationToken: cancellationToken);

        return new OutputBindingInvokeResponse();
    }

    private Task<OutputBindingInvokeResponse> SummarizeTextAsync(OutputBindingInvokeRequest request, AIEngineContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private async Task<OutputBindingInvokeResponse> TerminateChatAsync(OutputBindingInvokeRequest request, AIEngineContext context, CancellationToken cancellationToken)
    {
        var terminateRequest = SerializationUtilities.FromBytes<DaprAIEngineTerminateChatRequest>(request.Data.Span);

        string key = CreateKey(terminateRequest.InstanceId);

        await context.DaprClient.DeleteStateAsync(this.storeName!, key, cancellationToken: cancellationToken);

        return new OutputBindingInvokeResponse();
    }

    private static string CreateKey(string instanceId) => $"ai-chat-history-{instanceId}";

    private sealed record AIEngineContext(DaprClient DaprClient);
}
