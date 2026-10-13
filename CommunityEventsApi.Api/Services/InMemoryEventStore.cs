using System.Collections.Concurrent;
using CommunityEventsApi.Api.Models;

namespace CommunityEventsApi.Api.Services;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IEventStore"/> backed by a
/// <see cref="ConcurrentDictionary{TKey,TValue}"/>, seeded with sample events on construction.
/// Used as a local-development fallback when Firestore credentials are not configured.
/// </summary>
public class InMemoryEventStore : IEventStore
{
    private readonly ConcurrentDictionary<string, Event> _events = new();

    public InMemoryEventStore()
    {
        Seed();
    }

    public Task<IReadOnlyList<Event>> GetAllAsync()
    {
        IReadOnlyList<Event> result = _events.Values.OrderBy(e => e.Date, StringComparer.Ordinal).ToList();
        return Task.FromResult(result);
    }

    public Task<Event?> GetByIdAsync(string id) =>
        Task.FromResult(_events.TryGetValue(id, out var ev) ? ev : null);

    public Task<Event> AddAsync(Event newEvent)
    {
        _events[newEvent.Id] = newEvent;
        return Task.FromResult(newEvent);
    }

    public Task<bool> DeleteAsync(string id) => Task.FromResult(_events.TryRemove(id, out _));

    private void Seed()
    {
        var seedEvents = new[]
        {
            new Event
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Downtown Farmers Market",
                Date = "2025-06-14",
                Location = "Main Street Plaza",
                Description = "Weekly farmers market featuring local produce, artisan crafts, and live music.",
                ImageUrl = "https://example.com/images/farmers-market.jpg",
                CreatedAt = DateTime.UtcNow.ToString("o")
            },
            new Event
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Community Park Cleanup",
                Date = "2025-06-21",
                Location = "Riverside Park",
                Description = "Join neighbors to help clean up Riverside Park. Gloves and bags provided.",
                ImageUrl = "",
                CreatedAt = DateTime.UtcNow.ToString("o")
            },
            new Event
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Summer Night Movie Screening",
                Date = "2025-07-04",
                Location = "City Amphitheater",
                Description = "Outdoor family-friendly movie night under the stars. Bring your own blankets and chairs.",
                ImageUrl = "https://example.com/images/movie-night.jpg",
                CreatedAt = DateTime.UtcNow.ToString("o")
            }
        };

        foreach (var ev in seedEvents)
        {
            _events[ev.Id] = ev;
        }
    }
}
