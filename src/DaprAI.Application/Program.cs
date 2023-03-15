using DaprAI;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

const string ChatGpt = "chat-gpt";
const string AzureAI = "azure-ai";
const string AzureOpenAI = "azure-open-ai";

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDaprClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost(
    "/prompt",
    async ([FromQuery] string? component, [FromBody] PromptRequest request, [FromServices] DaprClient daprClient) =>
    {
        component ??= ChatGpt;

        var response = await daprClient.PromptAIAsync(component, request);

        return response;
    })
    .WithName("Prompt")
    .WithOpenApi();

app.MapPost(
    "/summarize",
    async ([FromQuery] string? component, [FromBody] PromptRequest request, [FromServices] DaprClient daprClient) =>
    {
        component ??= ChatGpt;

        var response = await daprClient.SummarizeAIAsync(component, request);

        return response;
    })
    .WithName("Summarize")
    .WithOpenApi();

app.Run();
