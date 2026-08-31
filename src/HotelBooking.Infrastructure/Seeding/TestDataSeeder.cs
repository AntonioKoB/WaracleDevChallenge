using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Seeding;

/// <summary>
/// Backs the testing-only /api/testing/seed and /api/testing/reset endpoints. Seeding
/// always resets first, so it's safe to call repeatedly without an explicit reset in
/// between - useful when driving the API by hand through Swagger.
/// </summary>
public class TestDataSeeder(HotelBookingDbContext context)
{
    public async Task<SeedSummary> SeedAsync(CancellationToken cancellationToken = default)
    {
        await ResetAsync(cancellationToken);

        var single = new RoomType("Single", capacity: 1);
        var doubleRoom = new RoomType("Double", capacity: 2);
        var deluxe = new RoomType("Deluxe", capacity: 4);
        context.RoomTypes.AddRange(single, doubleRoom, deluxe);

        var grandWaracle = new Hotel("The Grand Waracle", "1 King Street, London");
        var harbourview = new Hotel("Harbourview Inn", "22 Quayside, Bristol");
        context.Hotels.AddRange(grandWaracle, harbourview);

        var grandWaracleRooms = new[]
        {
            new Room(grandWaracle, single, "101"),
            new Room(grandWaracle, single, "102"),
            new Room(grandWaracle, doubleRoom, "201"),
            new Room(grandWaracle, doubleRoom, "202"),
            new Room(grandWaracle, deluxe, "301"),
            new Room(grandWaracle, deluxe, "302"),
        };
        var harbourviewRooms = new[]
        {
            new Room(harbourview, single, "101"),
            new Room(harbourview, single, "102"),
            new Room(harbourview, doubleRoom, "201"),
            new Room(harbourview, doubleRoom, "202"),
            new Room(harbourview, deluxe, "301"),
            new Room(harbourview, deluxe, "302"),
        };
        context.Rooms.AddRange(grandWaracleRooms);
        context.Rooms.AddRange(harbourviewRooms);

        // Relative to "today" rather than fixed dates, so the seed data is always a
        // near-future stay no matter when this actually runs.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var reservations = new[]
        {
            Reservation.Create(
                grandWaracleRooms[0], // Single 101
                today.AddDays(3), today.AddDays(5),
                [new Guest("Alice Example", "alice@example.com")]),
            Reservation.Create(
                harbourviewRooms[2], // Double 201
                today.AddDays(7), today.AddDays(10),
                [new Guest("Bob Example", "bob@example.com"), new Guest("Bea Example", "bea@example.com")]),
        };
        context.Reservations.AddRange(reservations);

        await context.SaveChangesAsync(cancellationToken);

        return new SeedSummary(
            HotelNames: [grandWaracle.Name, harbourview.Name],
            Reservations: reservations
                .Select(r => new SeededReservation(r.BookingReference, r.Room.Hotel.Name, r.Room.Number, r.CheckInDate, r.CheckOutDate))
                .ToList());
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        // Guests and ReservationNights cascade-delete with their Reservation, so deleting
        // Reservations first takes care of both. Rooms must go before Hotels/RoomTypes,
        // which they have a Restrict (not cascade) relationship with.
        await context.Reservations.ExecuteDeleteAsync(cancellationToken);
        await context.Rooms.ExecuteDeleteAsync(cancellationToken);
        await context.RoomTypes.ExecuteDeleteAsync(cancellationToken);
        await context.Hotels.ExecuteDeleteAsync(cancellationToken);
    }
}
