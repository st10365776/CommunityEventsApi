using CommunityEventsApi.Api.Models;
using CommunityEventsApi.Api.Services;

namespace CommunityEventsApi.Api.Endpoints;

/// <summary>
/// Maps the /api/events and /api/health endpoints.
/// </summary>
public static class EventEndpoints
{
    public static void MapEventEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api");

        group.MapGet("/events", (IEventStore store) =>
            Results.Ok(store.GetAll()));

        group.MapGet("/events/{id}", (string id, IEventStore store) =>
        {
            var ev = store.GetById(id);
            return ev is null
                ? Results.NotFound(new { error = "Event not found" })
                : Results.Ok(ev);
        });

        group.MapPost("/events", (CreateEventRequest request, IEventStore store) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title) ||
                string.IsNullOrWhiteSpace(request.Date) ||
                string.IsNullOrWhiteSpace(request.Location) ||
                string.IsNullOrWhiteSpace(request.Description) ||
                !DateOnly.TryParseExact(request.Date, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out _))
            {
                return Results.BadRequest(new
                {
                    error = "Missing required fields or invalid date format YYYY-MM-DD"
                });
            }

            var newEvent = new Event
            {
                Id = Guid.NewGuid().ToString(),
                Title = request.Title,
                Date = request.Date,
                Location = request.Location,
                Description = request.Description,
                ImageUrl = request.ImageUrl ?? string.Empty,
                CreatedAt = DateTime.UtcNow.ToString("o")
            };

            store.Add(newEvent);

            return Results.Created($"/api/events/{newEvent.Id}", newEvent);
        });

        group.MapDelete("/events/{id}", (string id, IEventStore store) =>
        {
            return store.Delete(id)
                ? Results.Ok(new { message = "Event deleted successfully" })
                : Results.NotFound(new { error = "Event not found" });
        });

        group.MapGet("/health", () =>
        {
            var uptimeSeconds = Environment.TickCount64 / 1000.0;
            return Results.Ok(new { status = "OK", uptime = uptimeSeconds });
        });
    }
}
