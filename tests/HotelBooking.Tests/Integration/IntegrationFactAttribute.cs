namespace HotelBooking.Tests.Integration;

/// <summary>
/// A [Fact] that skips itself when there's no real database to run against, instead of
/// failing. Locally that just means the DB-backed tests don't run unless you've set
/// AZURE_SQL_TEST_CONNECTION_STRING yourself; in CI, the secret of the same name is
/// always present.
/// </summary>
public sealed class IntegrationFactAttribute : FactAttribute
{
    public const string ConnectionStringEnvVar = "AZURE_SQL_TEST_CONNECTION_STRING";

    public IntegrationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionStringEnvVar)))
        {
            Skip = $"{ConnectionStringEnvVar} is not set - skipping tests that need a real database.";
        }
    }
}
