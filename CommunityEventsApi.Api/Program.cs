using System.Text.Json;
using CommunityEventsApi.Api.Endpoints;
using CommunityEventsApi.Api.Services;
using Microsoft.AspNetCore.Diagnostics;

const string CorsPolicyName = "AllowAll";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IEventStore, InMemoryEventStore>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // Raw OpenAPI/Swagger JSON at /swagger/v1/swagger.json.
    app.UseSwaggerUI(); // Browsable Swagger UI at /swagger.
}

// Global exception handling: return a clean JSON error body instead of the dev exception page.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var isBadJson = feature?.Error is JsonException or BadHttpRequestException;

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = isBadJson
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status500InternalServerError;

        var message = isBadJson ? "Invalid request body" : "Internal server error";
        await context.Response.WriteAsJsonAsync(new { error = message });
    });
});

app.UseCors(CorsPolicyName);

app.MapEventEndpoints();

app.Run();
