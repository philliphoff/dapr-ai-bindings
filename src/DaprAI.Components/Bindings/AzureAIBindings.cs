using Dapr.PluggableComponents.Components;
using Dapr.PluggableComponents.Components.Bindings;

namespace DaprAI.Bindings;

internal sealed class AzureAIBindings : IInputBinding, IOutputBinding
{
    public const string ChatGpt = "chatgpt";

    #region IInputBinding Members

    public Task InitAsync(MetadataRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task ReadAsync(MessageDeliveryHandler<InputBindingReadRequest, InputBindingReadResponse> deliveryHandler, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region IOutputBinding Members

    public Task<OutputBindingInvokeResponse> InvokeAsync(OutputBindingInvokeRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<string[]> ListOperationsAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    #endregion
}
