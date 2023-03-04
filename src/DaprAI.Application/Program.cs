using System.Text.Json.Serialization;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

const string ChatGpt = "chat-gpt";
const string AzureAI = "azure-ai";

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

app.MapPost($"/{AzureAI}", () =>
{
    // TODO: Handle chat response.
    return Results.Accepted();
});

app.MapPost($"/{ChatGpt}", () =>
{
    // TODO: Handle chat response.
    return Results.Accepted();
});

app.Run();

internal sealed record PromptRequest(
    [property: JsonPropertyName("prompt")]
    string Prompt);

internal sealed record PromptResponse(
    [property: JsonPropertyName("response")]
    string Response);

internal static class DaprClientExtensions
{
    public static Task<PromptResponse> PromptAIAsync(this DaprClient daprClient, string component, PromptRequest request) =>
        daprClient.InvokeBindingAsync<PromptRequest, PromptResponse>(component, "prompt", request);

    public static Task<PromptResponse> SummarizeAIAsync(this DaprClient daprClient, string component, PromptRequest request) =>
        daprClient.InvokeBindingAsync<PromptRequest, PromptResponse>(component, "summarize", request);
}
