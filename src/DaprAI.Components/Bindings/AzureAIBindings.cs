using System.Text;
using Azure;
using Azure.AI.TextAnalytics;
using Dapr.PluggableComponents.Components;
using Dapr.PluggableComponents.Components.Bindings;

namespace DaprAI.Bindings;

internal sealed class AzureAIBindings : IInputBinding, IOutputBinding
{
    private string? azureAIEndpoint;
    private string? azureAIKey;

    #region IInputBinding Members

    public Task InitAsync(MetadataRequest request, CancellationToken cancellationToken = default)
    {
        request.Properties.TryGetValue("azure-ai-endpoint", out this.azureAIEndpoint);
        request.Properties.TryGetValue("azure-ai-key", out this.azureAIKey);

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
            "prompt" => Task.FromResult(new OutputBindingInvokeResponse { Data = new PromptResponse("Hello from Azure AI!").ToBytes() }),
            "summarize" => this.SummarizeAsync(request, cancellationToken),
            _ => throw new NotImplementedException(),
        };
    }

    public Task<string[]> ListOperationsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new[] { "prompt", "summarize" });
    }

    #endregion

    public async Task<OutputBindingInvokeResponse> SummarizeAsync(OutputBindingInvokeRequest request, CancellationToken cancellationToken = default)
    {
        var credentials = new AzureKeyCredential(this.azureAIKey!);
        var client = new TextAnalyticsClient(new Uri(this.azureAIEndpoint!), credentials);

        var summarizeRequest = PromptRequest.FromBytes(request.Data.Span);

        var batchInput = new List<string>
        {
            summarizeRequest.Prompt
        };

        TextAnalyticsActions actions = new TextAnalyticsActions()
        {
            ExtractSummaryActions = new List<ExtractSummaryAction>() { new ExtractSummaryAction() }
        };

            // Start analysis process.
        AnalyzeActionsOperation operation = await client.StartAnalyzeActionsAsync(batchInput, actions);
        await operation.WaitForCompletionAsync();

        string summarizedText = string.Empty;

        // View operation status.
        summarizedText += $"AnalyzeActions operation has completed" + Environment.NewLine;
        summarizedText += $"Created On   : {operation.CreatedOn}" + Environment.NewLine;
        summarizedText += $"Expires On   : {operation.ExpiresOn}" + Environment.NewLine;
        summarizedText += $"Id           : {operation.Id}" + Environment.NewLine;
        summarizedText += $"Status       : {operation.Status}" + Environment.NewLine;

        // View operation results.
        await foreach (AnalyzeActionsResult documentsInPage in operation.Value)
        {
            IReadOnlyCollection<ExtractSummaryActionResult> summaryResults = documentsInPage.ExtractSummaryResults;

            foreach (ExtractSummaryActionResult summaryActionResults in summaryResults)
            {
                if (summaryActionResults.HasError)
                {
                    continue;
                }

                foreach (ExtractSummaryResult documentResults in summaryActionResults.DocumentsResults)
                {
                    if (documentResults.HasError)
                    {
                        continue;
                    }

                    summarizedText += $"  Extracted the following {documentResults.Sentences.Count} sentence(s):" + Environment.NewLine;


                    foreach (SummarySentence sentence in documentResults.Sentences)
                    {
                        summarizedText += $"  Sentence: {sentence.Text}" + Environment.NewLine;
                    }
                }
            }
        }

        return new OutputBindingInvokeResponse { Data = new PromptResponse(summarizedText).ToBytes() };
    }
}
