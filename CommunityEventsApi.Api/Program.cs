using System.Text.Json;
using CommunityEventsApi.Api.Endpoints;
using CommunityEventsApi.Api.Services;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Diagnostics;

const string CorsPolicyName = "AllowAll";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

RegisterEventStore(builder);

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

// Registers IEventStore as a Firestore-backed store when a project id + service account
// credentials are configured; otherwise falls back to the in-memory store so the API still
// runs locally without any Google Cloud setup.
static void RegisterEventStore(WebApplicationBuilder builder)
{
    var projectId = builder.Configuration["Firestore:ProjectId"];
    var credentialsPath = builder.Configuration["Firestore:CredentialsPath"];

    var resolvedCredentialsPath = !string.IsNullOrWhiteSpace(credentialsPath)
        ? Path.Combine(builder.Environment.ContentRootPath, credentialsPath)
        : null;

    var hasCredentialsFile = resolvedCredentialsPath is not null && File.Exists(resolvedCredentialsPath);
    var hasAdcEnvVar = !string.IsNullOrWhiteSpace(
        Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS"));

    if (string.IsNullOrWhiteSpace(projectId) || !(hasCredentialsFile || hasAdcEnvVar))
    {
        Console.WriteLine(
            "[Startup] Firestore is not configured (missing ProjectId or credentials) — " +
            "falling back to the in-memory event store.");
        builder.Services.AddSingleton<IEventStore, InMemoryEventStore>();
        return;
    }

    Console.WriteLine($"[Startup] Using Firestore event store (project '{projectId}').");

    var firestoreDb = new FirestoreDbBuilder
    {
        ProjectId = projectId,
        GoogleCredential = hasCredentialsFile
            ? CredentialFactory.FromFile<ServiceAccountCredential>(resolvedCredentialsPath).ToGoogleCredential()
            : null
    }.Build();

    builder.Services.AddSingleton(firestoreDb);
    builder.Services.AddSingleton<IEventStore, FirestoreEventStore>();
}
