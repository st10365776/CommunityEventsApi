namespace CommunityEventsApi.Api.Models;

/// <summary>
/// Represents a community event stored by the API.
/// </summary>
public class Event
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;

    /// <summary>Event date formatted as YYYY-MM-DD.</summary>
    public string Date { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>ISO-8601 UTC timestamp of when the event was created.</summary>
    public string CreatedAt { get; set; } = string.Empty;
}

/// <summary>
/// Request body for creating a new event.
/// </summary>
public class CreateEventRequest
{
    public string? Title { get; set; }
    public string? Date { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
}
