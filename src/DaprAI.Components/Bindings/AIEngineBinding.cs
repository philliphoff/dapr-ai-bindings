using System.Text.Json.Serialization;
using Dapr.Client;
using Dapr.PluggableComponents.Components;
using Dapr.PluggableComponents.Components.Bindings;
using DaprAI.Utilities;

namespace DaprAI.Bindings;

internal sealed class AiEngineBinding : IOutputBinding
{
    private readonly ILogger<AiEngineBinding> logger;

    private string? aiName;
    private string? storeName;

    public AiEngineBinding(ILogger<AiEngineBinding> logger)
    {
        this.logger = logger;
    }

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

        this.logger.LogInformation("AI Engine is using AI service '{aiName}' with state store '{storeName}.'", this.aiName, this.storeName);

        return Task.CompletedTask;
    }

    public Task<OutputBindingInvokeResponse> InvokeAsync(OutputBindingInvokeRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.Metadata.TryGetValue(Constants.Metadata.DaprGrpcPort, out var daprPort))
        {
            throw new InvalidOperationException("Missing required metadata property 'daprGrpcPort'.");
        }

        string daprGrpcEndpoint = $"http://127.0.0.1:{daprPort}";

        this.logger.LogDebug("AI Engine is using Dapr gRPC endpoint '{daprGrpcEndpoint}'.", daprGrpcEndpoint);

        var daprClient = new DaprClientBuilder().UseGrpcEndpoint(daprGrpcEndpoint).Build();
        var context = new AIEngineContext(daprClient);

        return request.Operation switch
        {
            Constants.Operations.CompleteText => this.CompleteTextAsync(request, context, cancellationToken),
            Constants.Operations.CreateChat => this.CreateChatAsync(request, context, cancellationToken),
            Constants.Operations.GetChat => this.GetChatAsync(request, context, cancellationToken),
            Constants.Operations.GetChats => this.GetChatsAsync(request, context, cancellationToken),
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
                Constants.Operations.GetChat,
                Constants.Operations.GetChats,
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

        await this.AddIdToIndex(createRequest.InstanceId, context.DaprClient, cancellationToken);

        string key = CreateKey(createRequest.InstanceId);

        var history = await context.DaprClient.GetStateAsync<DaprChatHistory>(this.storeName!, key, cancellationToken: cancellationToken);

        if (history is null)
        {
            history = new DaprChatHistory(
                !String.IsNullOrEmpty(createRequest.SystemInstructions)
                    ? new[] { new DaprChatHistoryItem("system", createRequest.SystemInstructions) }
                    : Array.Empty<DaprChatHistoryItem>());

            await context.DaprClient.SaveStateAsync(this.storeName!, key, history, cancellationToken: cancellationToken);
        }

        return new OutputBindingInvokeResponse();
    }

    private async Task<OutputBindingInvokeResponse> GetChatAsync(OutputBindingInvokeRequest request, AIEngineContext context, CancellationToken cancellationToken)
    {
        var getRequest = SerializationUtilities.FromBytes<DaprAIEngineGetChatRequest>(request.Data.Span);

        string key = CreateKey(getRequest.InstanceId);

        var history = await context.DaprClient.GetStateAsync<DaprChatHistory?>(this.storeName!, key, cancellationToken: cancellationToken);

        return new OutputBindingInvokeResponse { Data = SerializationUtilities.ToBytes(new DaprAIEngineGetChatResponse { History = history }) };
    }

    private async Task<OutputBindingInvokeResponse> GetChatsAsync(OutputBindingInvokeRequest request, AIEngineContext context, CancellationToken cancellationToken)
    {
        // TODO: There are race conditions related to maintaining the index. It would be much better to do a key scan on the store, if only there was a common way to do that.

        var getRequest = SerializationUtilities.FromBytes<DaprAIEngineGetChatsRequest>(request.Data.Span);

        var metadata = await context.DaprClient.GetMetadataAsync(cancellationToken);

        var index = await context.DaprClient.GetStateAsync<ChatIndex>(this.storeName!, ChatIndexKey, cancellationToken: cancellationToken);

        return new OutputBindingInvokeResponse { Data = SerializationUtilities.ToBytes(new DaprAIEngineGetChatsResponse { Chats = index?.InstanceIds.Select(id => new DaprAIEngineGetChatsChat(id)).ToArray() ?? Array.Empty<DaprAIEngineGetChatsChat>() }) };
    }

    private async Task<OutputBindingInvokeResponse> SummarizeTextAsync(OutputBindingInvokeRequest request, AIEngineContext context, CancellationToken cancellationToken)
    {
        var summarizationRequest = SerializationUtilities.FromBytes<DaprSummarizationRequest>(request.Data.Span);

        var summarizationResponse = await context.DaprClient.SummarizeTextAsync(this.aiName!, summarizationRequest, cancellationToken: cancellationToken);

        return new OutputBindingInvokeResponse { Data = SerializationUtilities.ToBytes(summarizationResponse) };
    }

    private async Task<OutputBindingInvokeResponse> TerminateChatAsync(OutputBindingInvokeRequest request, AIEngineContext context, CancellationToken cancellationToken)
    {
        var terminateRequest = SerializationUtilities.FromBytes<DaprAIEngineTerminateChatRequest>(request.Data.Span);

        string key = CreateKey(terminateRequest.InstanceId);

        await context.DaprClient.DeleteStateAsync(this.storeName!, key, cancellationToken: cancellationToken);

        await this.RemoveIdFromIndex(terminateRequest.InstanceId, context.DaprClient, cancellationToken);

        return new OutputBindingInvokeResponse();
    }

    private async Task AddIdToIndex(string instanceId, DaprClient daprClient, CancellationToken cancellationToken)
    {
        var index = await daprClient.GetStateAsync<ChatIndex?>(this.storeName!, ChatIndexKey, cancellationToken: cancellationToken);

        if (index is null)
        {
            index = new ChatIndex(new[] { instanceId });
        }
        else
        {
            var ids = new HashSet<string>(index.InstanceIds);

            if (ids.Contains(instanceId))
            {
                return;
            }

            ids.Add(instanceId);

            index = index with { InstanceIds = ids.ToArray() };
        }

        await daprClient.SaveStateAsync(this.storeName!, ChatIndexKey, index, cancellationToken: cancellationToken);
    }

    private async Task RemoveIdFromIndex(string instanceId, DaprClient daprClient, CancellationToken cancellationToken)
    {
        var index = await daprClient.GetStateAsync<ChatIndex?>(this.storeName!, ChatIndexKey, cancellationToken: cancellationToken);

        if (index is not null)
        {
            var ids = new HashSet<string>(index.InstanceIds);

            if (ids.Remove(instanceId))
            {
                index = index with { InstanceIds = ids.ToArray() };

                await daprClient.SaveStateAsync(this.storeName!, ChatIndexKey, index, cancellationToken: cancellationToken);
            }
        }
    }

    private static string CreateKey(string instanceId) => $"ai-chat-history-{instanceId}";

    private sealed record AIEngineContext(DaprClient DaprClient);

    private const string ChatIndexKey = "ai-chat-index";

    private sealed record ChatIndex(
        [property: JsonPropertyName("ids")]
        string[] InstanceIds);
}
