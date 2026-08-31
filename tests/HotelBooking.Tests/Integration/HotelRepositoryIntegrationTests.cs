using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Tests.Integration;

[Collection(DatabaseCollection.Name)]
public class HotelRepositoryIntegrationTests(DatabaseFixture fixture)
{
    private static async Task<Hotel> CreateHotelAsync(HotelBookingDbContext context, string name)
    {
        var hotel = new Hotel(name, "1 Test Street");
        context.Hotels.Add(hotel);
        await context.SaveChangesAsync();
        return hotel;
    }

    [IntegrationFact]
    public async Task SearchByNameAsync_WithExactName_ReturnsHotel()
    {
        await using var context = fixture.CreateContext();
        var hotel = await CreateHotelAsync(context, $"Test Hotel {Guid.NewGuid():N}");
        var repository = new HotelRepository(context, TestResilience.Pipeline);

        var results = await repository.SearchByNameAsync(hotel.Name);

        Assert.Contains(results, h => h.Id == hotel.Id);

        await context.Hotels.Where(h => h.Id == hotel.Id).ExecuteDeleteAsync();
    }

    [IntegrationFact]
    public async Task SearchByNameAsync_WithPartialNameInADifferentCase_StillMatches()
    {
        await using var context = fixture.CreateContext();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var hotel = await CreateHotelAsync(context, $"The Grand Waracle {suffix}");
        var repository = new HotelRepository(context, TestResilience.Pipeline);

        var results = await repository.SearchByNameAsync("WARACLE");

        Assert.Contains(results, h => h.Id == hotel.Id);

        await context.Hotels.Where(h => h.Id == hotel.Id).ExecuteDeleteAsync();
    }

    [IntegrationFact]
    public async Task SearchByNameAsync_WithSpaceInSearchTerm_ActsAsWildcardAcrossWords()
    {
        await using var context = fixture.CreateContext();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var hotel = await CreateHotelAsync(context, $"The Grand Waracle Hotel {suffix}");
        var repository = new HotelRepository(context, TestResilience.Pipeline);

        // "Waracle" sits between "Grand" and "Hotel" in the real name - the space in the
        // search term should act as a wildcard, not require an exact adjacent match.
        var results = await repository.SearchByNameAsync("grand hotel");

        Assert.Contains(results, h => h.Id == hotel.Id);

        await context.Hotels.Where(h => h.Id == hotel.Id).ExecuteDeleteAsync();
    }

    [IntegrationFact]
    public async Task SearchByNameAsync_WithLiteralPercentInSearchTerm_IsTreatedLiterally()
    {
        await using var context = fixture.CreateContext();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var hotel = await CreateHotelAsync(context, $"100% Waracle Suites {suffix}");
        var repository = new HotelRepository(context, TestResilience.Pipeline);

        var results = await repository.SearchByNameAsync("100%");

        Assert.Contains(results, h => h.Id == hotel.Id);

        await context.Hotels.Where(h => h.Id == hotel.Id).ExecuteDeleteAsync();
    }

    [IntegrationFact]
    public async Task SearchByNameAsync_WithNoMatch_ReturnsEmptyCollection()
    {
        await using var context = fixture.CreateContext();
        var repository = new HotelRepository(context, TestResilience.Pipeline);

        var results = await repository.SearchByNameAsync($"NoSuchHotel{Guid.NewGuid():N}");

        Assert.Empty(results);
    }
}
