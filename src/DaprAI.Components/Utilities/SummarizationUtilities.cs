namespace DaprAI.Utilities;

internal static class SummarizationUtilities
{
    private static readonly HttpClient HttpClient = new();

    public static Task<string> GetDocumentText(DaprSummarizationRequest request, CancellationToken cancellationToken)
    {
        if (request.DocumentText is not null && request.DocumentUrl is not null)
        {
            throw new ArgumentException($"Only one of {nameof(request.DocumentText)} or {nameof(request.DocumentUrl)} can be specified.", nameof(request));
        }
        else if (request.DocumentUrl is not null)
        {
            return HttpClient.GetStringAsync(request.DocumentUrl, cancellationToken);
        }
        else if (request.DocumentText is not null)
        {
            return Task.FromResult(request.DocumentText);
        }
        else
        {
            throw new ArgumentException($"Either {nameof(request.DocumentText)} or {nameof(request.DocumentUrl)} must be specified.", nameof(request));
        }
    }
}
