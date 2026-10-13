using CommunityEventsApi.Api.Models;

namespace CommunityEventsApi.Api.Services;

/// <summary>
/// Thread-safe in-memory store for community events.
/// </summary>
public interface IEventStore
{
    IReadOnlyList<Event> GetAll();
    Event? GetById(string id);
    Event Add(Event newEvent);
    bool Delete(string id);
}
