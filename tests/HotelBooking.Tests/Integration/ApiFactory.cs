using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace HotelBooking.Tests.Integration;

/// <summary>
/// Boots the real app (real DI wiring, real migrations-on-startup) against the same test
/// database as the other integration tests, overriding whatever connection string
/// appsettings would otherwise pick - in CI there's no appsettings.Development.json at all.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var connectionString = Environment.GetEnvironmentVariable(IntegrationFactAttribute.ConnectionStringEnvVar);
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString,
                });
            }
        });
    }
}
