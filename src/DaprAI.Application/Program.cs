using DaprAI;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

const string ChatGpt = "open-ai-gpt";

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
    "/engine/{instanceId}",
    async (string instanceId, [FromBody] EngineCreateRequest request, [FromServices] DaprClient daprClient) =>
    {
        await daprClient.AIEngineCreateChatAsync(
            new DaprAIEngineCreateChatRequest(instanceId)
            {
                SystemInstructions = request.SystemInstructions
            });

        return Results.Created("/engine/create", new { instanceId });
    })
    .WithName("Create Chat")
    .WithOpenApi();

app.MapPost(
    "/engine/{instanceId}/complete",
    async (string instanceId, [FromBody] EngineCompletionRequest request, [FromServices] DaprClient daprClient) =>
    {
        var response = await daprClient.AIEngineCompleteTextAsync(
            new DaprAIEngineCompletionRequest(request.UserPrompt)
            {
                InstanceId = instanceId
            });

        return response;
    })
    .WithName("Complete Chat Text")
    .WithOpenApi();

app.MapPost(
    "/engine/complete",
    async ([FromBody] EngineCompletionRequest request, [FromServices] DaprClient daprClient) =>
    {
        var response = await daprClient.AIEngineCompleteTextAsync(new DaprAIEngineCompletionRequest(request.UserPrompt));

        return response;
    })
    .WithName("Complete Simple Text")
    .WithOpenApi();

app.MapDelete(
    "/engine/{instanceId}",
    async (string instanceId, [FromServices] DaprClient daprClient) =>
    {
        await daprClient.AIEngineTerminateChatAsync(new DaprAIEngineTerminateChatRequest(instanceId));

        return Results.Ok();
    })
    .WithName("Terminate Chat")
    .WithOpenApi();

app.MapPost(
    "/complete",
    async ([FromQuery] string? component, [FromBody] DaprCompletionRequest request, [FromServices] DaprClient daprClient) =>
    {
        component ??= ChatGpt;

        var response = await daprClient.CompleteTextAsync(component, request);

        return response;
    })
    .WithName("Prompt")
    .WithOpenApi();

app.MapPost(
    "/summarize",
    async ([FromQuery] string? component, [FromBody] DaprSummarizationRequest request, [FromServices] DaprClient daprClient) =>
    {
        component ??= ChatGpt;

        var response = await daprClient.SummarizeTextAsync(component, request);

        return response;
    })
    .WithName("Summarize")
    .WithOpenApi();

app.Run();

sealed record EngineCreateRequest
{
    [JsonPropertyName("system")]
    public string? SystemInstructions { get; init; }
}

sealed record EngineCompletionRequest(
    [property: JsonPropertyName("user")]
    string UserPrompt);
