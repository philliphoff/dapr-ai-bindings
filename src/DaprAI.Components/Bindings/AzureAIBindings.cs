using Azure;
using Azure.AI.TextAnalytics;
using Dapr.PluggableComponents.Components;
using Dapr.PluggableComponents.Components.Bindings;
using DaprAI.Utilities;

namespace DaprAI.Bindings;

internal sealed class AzureAIBindings : IOutputBinding
{
    private string? azureAIEndpoint;
    private string? azureAIKey;

    #region IOutputBinding Members

    public Task InitAsync(MetadataRequest request, CancellationToken cancellationToken = default)
    {
        request.Properties.TryGetValue("azure-ai-endpoint", out this.azureAIEndpoint);
        request.Properties.TryGetValue("azure-ai-key", out this.azureAIKey);

        return Task.CompletedTask;
    }

    public Task<OutputBindingInvokeResponse> InvokeAsync(OutputBindingInvokeRequest request, CancellationToken cancellationToken = default)
    {
        return request.Operation switch
        {
            Constants.Operations.SummarizeText => this.SummarizeAsync(request, cancellationToken),
            _ => throw new NotImplementedException(),
        };
    }

    public Task<string[]> ListOperationsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new[] { Constants.Operations.SummarizeText });
    }

    #endregion

    private async Task<OutputBindingInvokeResponse> SummarizeAsync(OutputBindingInvokeRequest request, CancellationToken cancellationToken)
    {
        var credentials = new AzureKeyCredential(this.azureAIKey!);
        var client = new TextAnalyticsClient(new Uri(this.azureAIEndpoint!), credentials);

        var summarizationRequest = SerializationUtilities.FromBytes<DaprSummarizationRequest>(request.Data.Span);

        string documentText = await SummarizationUtilities.GetDocumentText(summarizationRequest, cancellationToken);

        var operation = await client.StartAnalyzeActionsAsync(
            new[]
            {
                documentText
            },
            new TextAnalyticsActions
            {
                ExtractSummaryActions = new[] { new ExtractSummaryAction() }
            },
            cancellationToken: cancellationToken);

        await operation.WaitForCompletionAsync(cancellationToken);

        var sentences = new List<string>();

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

                    sentences.AddRange(documentResults.Sentences.Select(s => s.Text));
                }
            }
        }

        var summarizationResponse = new DaprSummarizationResponse(String.Join(' ', sentences));

        return new OutputBindingInvokeResponse { Data = SerializationUtilities.ToBytes(summarizationResponse) };
    }
}
