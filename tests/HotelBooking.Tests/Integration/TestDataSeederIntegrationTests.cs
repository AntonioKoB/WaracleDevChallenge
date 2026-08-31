using HotelBooking.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Tests.Integration;

[Collection(DatabaseCollection.Name)]
public class TestDataSeederIntegrationTests(DatabaseFixture fixture)
{
    [IntegrationFact]
    public async Task SeedAsync_PopulatesTwoHotelsWithSixRoomsEach()
    {
        await using var context = fixture.CreateContext();
        var seeder = new TestDataSeeder(context);

        await seeder.SeedAsync();

        var roomCountsByHotel = await context.Hotels
            .Select(h => context.Rooms.Count(r => r.HotelId == h.Id))
            .ToListAsync();

        Assert.Equal(2, roomCountsByHotel.Count);
        Assert.All(roomCountsByHotel, count => Assert.Equal(6, count));

        await seeder.ResetAsync();
    }

    [IntegrationFact]
    public async Task SeedAsync_PopulatesTheThreeRoomTypesWithExpectedCapacities()
    {
        await using var context = fixture.CreateContext();
        var seeder = new TestDataSeeder(context);

        await seeder.SeedAsync();

        var roomTypes = await context.RoomTypes.ToDictionaryAsync(rt => rt.Name, rt => rt.Capacity);

        Assert.Equal(3, roomTypes.Count);
        Assert.Equal(1, roomTypes["Single"]);
        Assert.Equal(2, roomTypes["Double"]);
        Assert.Equal(4, roomTypes["Deluxe"]);

        await seeder.ResetAsync();
    }

    [IntegrationFact]
    public async Task SeedAsync_PopulatesReservationsWithFutureRelativeDates()
    {
        await using var context = fixture.CreateContext();
        var seeder = new TestDataSeeder(context);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var summary = await seeder.SeedAsync();

        Assert.Equal(2, summary.Reservations.Count);
        Assert.All(summary.Reservations, r => Assert.NotEmpty(r.BookingReference));
        // Relative to "today" rather than fixed dates, so this holds no matter when it runs.
        Assert.All(summary.Reservations, r => Assert.True(r.CheckInDate > today));
        Assert.Equal(2, await context.Reservations.CountAsync());

        await seeder.ResetAsync();
    }

    [IntegrationFact]
    public async Task SeedAsync_CalledTwiceInARow_DoesNotThrowAndLeavesTheSameShape()
    {
        // Seed is meant to be safe to click repeatedly in Swagger without an explicit
        // reset in between - this is what actually proves that.
        await using var context = fixture.CreateContext();
        var seeder = new TestDataSeeder(context);

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        Assert.Equal(2, await context.Hotels.CountAsync());
        Assert.Equal(3, await context.RoomTypes.CountAsync());
        Assert.Equal(12, await context.Rooms.CountAsync());
        Assert.Equal(2, await context.Reservations.CountAsync());

        await seeder.ResetAsync();
    }

    [IntegrationFact]
    public async Task ResetAsync_RemovesAllData()
    {
        await using var context = fixture.CreateContext();
        var seeder = new TestDataSeeder(context);
        await seeder.SeedAsync();

        await seeder.ResetAsync();

        Assert.Equal(0, await context.Hotels.CountAsync());
        Assert.Equal(0, await context.RoomTypes.CountAsync());
        Assert.Equal(0, await context.Rooms.CountAsync());
        Assert.Equal(0, await context.Reservations.CountAsync());
    }
}
