using Dapr.PluggableComponents;
using DaprAI.Bindings;

var app = DaprPluggableComponentsApplication.Create();

app.RegisterService(
    "ai-engine",
    serviceBuilder =>
    {
        serviceBuilder.RegisterBinding<AiEngineBinding>();
    });

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

System.Threading.Thread.Sleep(2000);

app.Run();
