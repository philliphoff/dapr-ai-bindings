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

app.MapGet(
    "/engine/{instanceId}",
    async (string instanceId, [FromServices] DaprClient daprClient, CancellationToken cancellationToken) =>
    {
        var response = await daprClient.AIEngineGetChatAsync(new DaprAIEngineGetChatRequest(instanceId), cancellationToken);

        return response.History is not null ? Results.Ok(response.History) : Results.NotFound();
    })
    .WithName("Engine: Get Chat")
    .WithOpenApi();

app.MapPost(
    "/engine/{instanceId}",
    async (string instanceId, [FromBody] EngineCreateRequest request, [FromServices] DaprClient daprClient, CancellationToken cancellationToken) =>
    {
        await daprClient.AIEngineCreateChatAsync(
            new DaprAIEngineCreateChatRequest(instanceId)
            {
                SystemInstructions = request.SystemInstructions
            },
            cancellationToken);

        return Results.Created("/engine/create", new { instanceId });
    })
    .WithName("Engine: Create Chat")
    .WithOpenApi();

app.MapPost(
    "/engine/{instanceId}/complete",
    async (string instanceId, [FromBody] EngineCompletionRequest request, [FromServices] DaprClient daprClient, CancellationToken cancellationToken) =>
    {
        var response = await daprClient.AIEngineCompleteTextAsync(
            new DaprAIEngineCompletionRequest(request.UserPrompt)
            {
                InstanceId = instanceId
            },
            cancellationToken);

        return response;
    })
    .WithName("Engine: Complete Chat Text")
    .WithOpenApi();

app.MapPost(
    "/engine/complete",
    async ([FromBody] EngineCompletionRequest request, [FromServices] DaprClient daprClient, CancellationToken cancellationToken) =>
    {
        var response = await daprClient.AIEngineCompleteTextAsync(new DaprAIEngineCompletionRequest(request.UserPrompt), cancellationToken);

        return response;
    })
    .WithName("Engine: Complete Simple Text")
    .WithOpenApi();

app.MapPost(
    "/engine/summarize",
    async ([FromBody] DaprSummarizationRequest request, [FromServices] DaprClient daprClient, CancellationToken cancellationToken) =>
    {
        var response = await daprClient.AIEngineSummarizeTextAsync(request, cancellationToken);

        return response;
    })
    .WithName("Engine: Summarize")
    .WithOpenApi();

app.MapDelete(
    "/engine/{instanceId}",
    async (string instanceId, [FromServices] DaprClient daprClient, CancellationToken cancellationToken) =>
    {
        await daprClient.AIEngineTerminateChatAsync(new DaprAIEngineTerminateChatRequest(instanceId), cancellationToken);

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
