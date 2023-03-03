using Dapr.PluggableComponents;
using DaprAI.Bindings;

var app = DaprPluggableComponentsApplication.Create();

app.RegisterService(
    "chat-gpt",
    serviceBuilder =>
    {
        serviceBuilder.RegisterBinding<ChatGptBindings>();
    });

app.RegisterService(
    "azure-ai",
    serviceBuilder =>
    {
        serviceBuilder.RegisterBinding<AzureAIBindings>();
    });

app.Run();
