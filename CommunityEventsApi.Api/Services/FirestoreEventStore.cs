using CommunityEventsApi.Api.Models;
using Google.Cloud.Firestore;

namespace CommunityEventsApi.Api.Services;

/// <summary>
/// Firestore-backed implementation of <see cref="IEventStore"/>. Reads/writes documents in the
/// "events" collection — the same collection the Android client reads directly via the
/// Firebase Firestore SDK — so both clients see the same data.
///
/// Firestore documents only store the fields the Android app writes (title, date, location,
/// description, imageUrl); the document ID becomes the Event's Id, and CreatedAt falls back to
/// the document's Firestore create time when no explicit "createdAt" field is present.
/// </summary>
public class FirestoreEventStore : IEventStore
{
    private const string CollectionName = "events";

    private readonly FirestoreDb _db;

    public FirestoreEventStore(FirestoreDb db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Event>> GetAllAsync()
    {
        var snapshot = await _db.Collection(CollectionName)
            .OrderBy("date")
            .GetSnapshotAsync();

        return snapshot.Documents.Select(ToEvent).ToList();
    }

    public async Task<Event?> GetByIdAsync(string id)
    {
        var doc = await _db.Collection(CollectionName).Document(id).GetSnapshotAsync();
        return doc.Exists ? ToEvent(doc) : null;
    }

    public async Task<Event> AddAsync(Event newEvent)
    {
        var docRef = _db.Collection(CollectionName).Document(newEvent.Id);
        await docRef.SetAsync(ToFirestoreFields(newEvent));
        return newEvent;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var docRef = _db.Collection(CollectionName).Document(id);
        var existing = await docRef.GetSnapshotAsync();
        if (!existing.Exists)
        {
            return false;
        }

        await docRef.DeleteAsync();
        return true;
    }

    private static Event ToEvent(DocumentSnapshot doc)
    {
        var createdAt = doc.TryGetValue<string>("createdAt", out var createdAtValue)
            ? createdAtValue
            : doc.CreateTime?.ToDateTime().ToString("o") ?? string.Empty;

        return new Event
        {
            Id = doc.Id,
            Title = doc.TryGetValue<string>("title", out var title) ? title : string.Empty,
            Date = doc.TryGetValue<string>("date", out var date) ? date : string.Empty,
            Location = doc.TryGetValue<string>("location", out var location) ? location : string.Empty,
            Description = doc.TryGetValue<string>("description", out var description) ? description : string.Empty,
            ImageUrl = doc.TryGetValue<string>("imageUrl", out var imageUrl) ? imageUrl : string.Empty,
            CreatedAt = createdAt
        };
    }

    private static Dictionary<string, object> ToFirestoreFields(Event ev) => new()
    {
        ["title"] = ev.Title,
        ["date"] = ev.Date,
        ["location"] = ev.Location,
        ["description"] = ev.Description,
        ["imageUrl"] = ev.ImageUrl,
        ["createdAt"] = ev.CreatedAt
    };
}
