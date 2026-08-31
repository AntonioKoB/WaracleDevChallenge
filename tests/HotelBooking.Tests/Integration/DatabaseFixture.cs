using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Tests.Integration;

/// <summary>
/// Shared once per test collection. Applies migrations on first use - the same call
/// Program.cs makes at startup - so the schema exists whether this is the very first
/// run against an empty database or the thousandth.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly string? _connectionString =
        Environment.GetEnvironmentVariable(IntegrationFactAttribute.ConnectionStringEnvVar);

    public async Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            return;

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public HotelBookingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HotelBookingDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        return new HotelBookingDbContext(options);
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Database";
}
