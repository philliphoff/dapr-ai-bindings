using Dapr.PluggableComponents;
using DaprAI.Bindings;

var app = DaprPluggableComponentsApplication.Create();

app.RegisterService(
    "open-ai",
    serviceBuilder =>
    {
        serviceBuilder.RegisterBinding(context => new OpenAIBindings());
    });

app.RegisterService(
    "azure-ai",
    serviceBuilder =>
    {
        serviceBuilder.RegisterBinding(context => new AzureAIBindings());
    });

app.RegisterService(
    "azure-open-ai",
    serviceBuilder =>
    {
        serviceBuilder.RegisterBinding(context => new AzureOpenAIBindings());
    });

app.Run();
