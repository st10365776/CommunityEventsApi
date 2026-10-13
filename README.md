# Community Events API

A simple, production-ready REST API server for a Community Events mobile application,
built with ASP.NET Core Minimal APIs. Event data is read from/written to the **same
Cloud Firestore `events` collection the Android app uses** (project `communityevents-e3552`),
with an in-memory store as a local-dev fallback when Firestore credentials aren't configured.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (the project targets `net9.0`).
  Verify with:
  ```
  dotnet --list-sdks
  ```
- (Optional) [JetBrains Rider](https://www.jetbrains.com/rider/) for the IDE experience.

## Firestore setup (required for real data)

The API reads/writes the `events` collection in the `communityevents-e3552` Firebase project.
Without credentials, it silently falls back to an in-memory store seeded with 3 sample events
(handy for local development, but it won't see real Firestore data).

To connect to real Firestore data:

1. In the [Firebase console](https://console.firebase.google.com/) for the `communityevents-e3552`
   project, go to **Project settings → Service accounts → Generate new private key**. This
   downloads a service account JSON key.
2. Save it as `CommunityEventsApi.Api/firebase-service-account.json` (already excluded from git
   via `.gitignore` — never commit this file).
   - Alternatively, set the `GOOGLE_APPLICATION_CREDENTIALS` environment variable to the full
     path of the key file instead of placing it in the project folder.
3. Restart the API. On startup it logs either:
   - `[Startup] Using Firestore event store (project 'communityevents-e3552').`, or
   - `[Startup] Firestore is not configured ... falling back to the in-memory event store.`

The Firestore project id and credentials file name/path are configured in
`CommunityEventsApi.Api/appsettings.json` under the `Firestore` section.

## Opening in JetBrains Rider

1. Open Rider and choose **Open**, then select `CommunityEventsApi.sln` at the repo root.
2. Let Rider restore NuGet packages automatically (or run `dotnet restore` from the terminal).
3. Select the `CommunityEventsApi.Api` run configuration (profile `http` or `https`) and click **Run**.

## Running from the CLI

```bash
dotnet restore
dotnet run --project CommunityEventsApi.Api
```

## Base URL / Port

By default (see `CommunityEventsApi.Api/Properties/launchSettings.json`), Kestrel listens on:

- HTTP: `http://localhost:5178`
- HTTPS: `https://localhost:7215`

All endpoints below use the HTTP base URL for simplicity:

```
http://localhost:5178
```

## API Documentation (Swagger)

When running in the `Development` environment, an interactive, browsable API reference is available via [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore):

- Swagger UI: `http://localhost:5178/swagger`
- Raw OpenAPI/Swagger JSON: `http://localhost:5178/swagger/v1/swagger.json`

Rider runs the `https` launch profile in Development by default, so these are also reachable at `https://localhost:7215/swagger`.

## API Endpoints

All endpoints are under `/api`.

### 1. Get all events

```bash
curl http://localhost:5178/api/events
```

Returns all events sorted ascending by `date`.

### 2. Get a single event by id

```bash
curl http://localhost:5178/api/events/{id}
```

**404 example** (unknown id):

```bash
curl -i http://localhost:5178/api/events/does-not-exist
```

```json
{ "error": "Event not found" }
```

### 3. Create an event

```bash
curl -X POST http://localhost:5178/api/events \
  -H "Content-Type: application/json" \
  -d '{
        "title": "Neighborhood Block Party",
        "date": "2025-08-15",
        "location": "Elm Street",
        "description": "An afternoon of food, games, and live music for the whole block.",
        "imageUrl": "https://example.com/images/block-party.jpg"
      }'
```

Returns `201 Created` with the full event object (including server-generated `id` and
`createdAt`), plus a `Location` header pointing at `GET /api/events/{id}`.

**400 validation example** (missing field or bad date format):

```bash
curl -i -X POST http://localhost:5178/api/events \
  -H "Content-Type: application/json" \
  -d '{ "title": "", "date": "08-15-2025", "location": "Elm Street", "description": "" }'
```

```json
{ "error": "Missing required fields or invalid date format YYYY-MM-DD" }
```

### 4. Delete an event

```bash
curl -X DELETE http://localhost:5178/api/events/{id}
```

```json
{ "message": "Event deleted successfully" }
```

If the id doesn't exist, returns `404` with `{ "error": "Event not found" }`.

### 5. Health check

```bash
curl http://localhost:5178/api/health
```

```json
{ "status": "OK", "uptime": 12.345 }
```

## Event JSON shape

```json
{
  "id": "string (GUID)",
  "title": "string",
  "date": "YYYY-MM-DD",
  "location": "string",
  "description": "string",
  "imageUrl": "string",
  "createdAt": "ISO-8601 UTC timestamp"
}
```

## Connecting the Android app

- **Android emulator**: point the base URL at `http://10.0.2.2:5178/` — this special
  alias routes to your development machine's `localhost` from inside the emulator.
- **Physical device** (same Wi-Fi network as your machine): use your machine's LAN IP,
  e.g. `http://192.168.1.42:5178/`. You may need to run the server bound to all interfaces
  (`dotnet run --project CommunityEventsApi.Api --urls http://0.0.0.0:5178`) and allow the
  port through your firewall.

## Project structure

```
CommunityEventsApi.sln
CommunityEventsApi.Api/
  Program.cs                      # Slim startup: DI, CORS, JSON options, middleware, Swagger, endpoint mapping
  Models/Event.cs                  # Event + CreateEventRequest models
  Services/IEventStore.cs          # Store abstraction (async)
  Services/FirestoreEventStore.cs  # Reads/writes the Firestore "events" collection
  Services/InMemoryEventStore.cs   # Thread-safe in-memory fallback store, seeded with sample data
  Endpoints/EventEndpoints.cs      # Route mappings for /api/events and /api/health
  firebase-service-account.json   # (you add this, gitignored) Firestore service account key
```
