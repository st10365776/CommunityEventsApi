using CommunityEventsApi.Api.Models;

namespace CommunityEventsApi.Api.Services;

/// <summary>
/// Thread-safe in-memory store for community events.
/// </summary>
public interface IEventStore
{
    Task<IReadOnlyList<Event>> GetAllAsync();
    Task<Event?> GetByIdAsync(string id);
    Task<Event> AddAsync(Event newEvent);
    Task<bool> DeleteAsync(string id);
}
