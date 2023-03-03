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

app.MapGet(
    "/prompt",
    async ([FromQuery]string? component, [FromServices] DaprClient daprClient) =>
    {
        component ??= ChatGpt;

        var response = await daprClient.InvokeBindingAsync<string, PromptResponse>(component, "prompt", "Hello, World!");

        return response;
    })
    .WithName("Prompt")
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

record PromptResponse(
    [property: JsonPropertyName("response")]
    string Response);
