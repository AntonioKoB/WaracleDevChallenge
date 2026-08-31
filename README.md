# WaracleDevChallenge

A hotel room booking API built with ASP.NET Core and EF Core, for the Waracle backend developer challenge.

## Business rules

- Hotels have 3 room types: single, double, deluxe.
- Hotels have 6 rooms.
- A room cannot be double-booked for any given night.
- A booking never requires guests to change rooms mid-stay: one reservation always maps to exactly one room for the whole stay.
- Booking references are unique, with no overlapping bookings at any given time.
- A room cannot be occupied by more people than its capacity.

## Data model

| Table | Columns | Notes |
|---|---|---|
| `Hotel` | Id, Name, Address | Address kept as a single line for now |
| `RoomType` | Id, Name, Capacity | Shared lookup table. Single/Double/Deluxe are seeded once, not duplicated per hotel |
| `Room` | Id, HotelId (FK), RoomTypeId (FK), Number | Unique on (HotelId, Number) |
| `Reservation` | Id, RoomId (FK), BookingReference (unique), CheckInDate, CheckOutDate | `BookingReference` is the external-facing lookup key, not the internal `Id` |
| `ReservationNight` | Id, RoomId, StayDate, ReservationId (FK) | One row per booked night. This table is what actually prevents overbooking, see below |
| `Guest` | Id, ReservationId (FK), Name, Email | Scoped to a single reservation, not a reusable customer identity across bookings |

All tables use an auto-increment integer id.

## Preventing overbooking under concurrency

The API is expected to run as multiple load-balanced instances, so an in-process lock or singleton can't guarantee correctness. Each instance has its own memory, so two requests hitting two different instances at the same moment would both sail past an in-memory check without ever knowing about each other.

Instead, the database's own unique index does the work. Booking a room inserts the `Reservation` row and one `ReservationNight` row per booked night, all in a single transaction, with a unique constraint on `(RoomId, StayDate)`.

If two concurrent requests target the same room and night, SQL Server's key-range locking on that unique index serializes them: the second insert blocks briefly, then fails with a duplicate-key violation once the first commits. That rolls back its entire transaction, so there's no partial booking left behind. The application layer only needs to catch that specific error and return `409 Conflict`.

No distributed lock service, no message queue. The guarantee holds the same whether there's one API instance or fifty, because every instance goes through the same database index.

## Repository pattern

Repository interfaces (`IHotelRepository`, `IRoomRepository`, `IReservationRepository`) live in `HotelBooking.Domain`. Their EF Core implementations live in `HotelBooking.Infrastructure` and get injected into the API via DI. This keeps the domain rules, and their unit tests, free of any dependency on EF Core or a real database.

## Running locally

`Database.Migrate()` runs at startup, so an empty database is enough - the schema gets created automatically on first run. `appsettings.Development.json` is gitignored (it ends up holding a real connection string and password), so a fresh clone needs one set up locally.

1. **Get a SQL Server instance running.** Any of these work: a local SQL Server install (Developer or Express edition), SQL Server LocalDB, or a Docker container:
   ```
   docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<a-strong-password>" -p 1433:1433 --name sql-local -d mcr.microsoft.com/mssql/server:2022-latest
   ```

2. **Create a database and a login scoped to it**, rather than pointing the app at `sa`:
   ```sql
   CREATE DATABASE WaracleLocalDB;
   GO
   CREATE LOGIN AppUser WITH PASSWORD = '<a-strong-password>';
   GO
   USE WaracleLocalDB;
   CREATE USER AppUser FOR LOGIN AppUser;
   ALTER ROLE db_owner ADD MEMBER AppUser;
   ```
   This mirrors how the deployed version works too: the app's login only ever has rights inside its own database, never across the whole server.

3. **Add a `ConnectionStrings:DefaultConnection` entry to `src/HotelBooking.Api/appsettings.Development.json`** (create the file if it isn't there yet):
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=WaracleLocalDB;User Id=AppUser;Password=<a-strong-password>;TrustServerCertificate=True;MultipleActiveResultSets=true"
     }
   }
   ```
   `TrustServerCertificate=True` is needed because a local SQL Server instance's certificate usually isn't signed by a CA the client already trusts.

4. **Run it:**
   ```
   dotnet run --project src/HotelBooking.Api
   ```

## Azure footprint

Everything here fits inside Azure's free tiers.

- **App Service (F1 free tier)** for hosting. A plain ASP.NET Core Web API with Swagger UI is a natural fit here; Azure Functions would add isolated-worker ceremony for no real benefit on a synchronous CRUD API.
- **Azure SQL Database (free tier)**, which pairs directly with EF Core's SQL Server provider.
- **Application Insights** for tracing, logging, and request telemetry. The free ingestion tier covers a project this size comfortably.
- **Service Bus was deliberately left out.** Nothing in the business rules is asynchronous, and Service Bus has no permanent free tier. Adding it would mean paying for messaging infrastructure nobody asked for.

Deployment is manual, on purpose. For a single App Service and a single database, Bicep or Terraform would be solving a problem this project doesn't have, so Azure resources are provisioned by hand through the Portal instead. The database schema takes care of itself: `Database.Migrate()` runs at application startup, so the first time the deployed app boots against an empty Azure SQL database, EF Core applies every migration on its own.

## Resiliency

- EF Core connection resiliency (`EnableRetryOnFailure`) handles transient SQL faults such as throttling or failover.
- A Polly circuit breaker wraps the database-facing calls, so the API fails fast instead of piling up timeouts if the database is genuinely unreachable.
- These are kept distinct from the overbooking conflict above: a duplicate-key violation is a correct business outcome, not a fault, and it must never get retried.

## How this was built

The solution is being built as a sequence of steps, each one buildable and reviewable on its own. The CI/CD pipeline grows the same way: a build gate from step 02, test running added once step 03 has unit tests, and deployment added once step 04 has an actual database-backed app and real Azure resources to target. Azure deployment starts at step 04, not the last step, so everything built after that point is live-testable as it lands.

| Step | Focus | Testing |
|---|---|---|
| 01 | Solution scaffolding: `src/` layout (Api, Domain, Infrastructure projects), `tests/` project | Solution builds, no business logic yet |
| 02 | CI pipeline: GitHub Actions workflow that restores and builds on every push | Build gate only, no deploy yet |
| 03 | Domain model, business rules, repository interfaces | Unit tests for overlap detection, capacity, no mid-stay room change. CI extended to also run `dotnet test` |
| 04 | EF Core infrastructure, code-first: DbContext, Fluent API configuration, initial migration, connection resiliency, repository implementations. Azure resources provisioned by hand (App Service, Azure SQL free tier), and the pipeline picks up a deploy stage | Integration tests against a real database, proving the `ReservationNight` constraint behaves as designed. First live deploy confirms `Database.Migrate()` runs against the real Azure SQL database |
| 05 | Core API endpoints and Swagger UI together: find a hotel by name, find available rooms between two dates for a given number of people, book a room, find a booking by reference | End-to-end tests via `WebApplicationFactory`; endpoints explorable live on Azure from this point on |
| 06 | Seed/reset endpoints for testing: `POST /testing/seed`, `POST /testing/reset` | Tests confirming each does what it says |
| 07 | Circuit breaker and resiliency: Polly wrapping database calls, global exception handling | Tests simulating a failing dependency, where feasible |
| 08 | Observability: Application Insights wiring for traces and logs | Verified by inspection once deployed |
| 09 | Documentation: this README finalized with run instructions, the deployed URL, and any assumptions made along the way | N/A |

## Use of AI assistance

Claude Code took care of most of the typing on this project, from scaffolding through to implementation code, and helped get this documentation into shape too.

The architecture calls were mine: choosing App Service for hosting, the data model, the `ReservationNight` design for stopping overbooking, dropping Service Bus and infrastructure-as-code, and how the work above got broken into steps. Those were decided and reviewed before anything got built. AI assistance moved the actual typing along faster; it didn't pick the design.
