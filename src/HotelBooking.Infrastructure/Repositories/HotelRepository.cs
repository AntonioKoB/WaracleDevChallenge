using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Repositories;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Resilience;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class HotelRepository(HotelBookingDbContext context, IDatabaseResiliencePipeline resilience) : IHotelRepository
{
    public Task<IReadOnlyCollection<Hotel>> SearchByNameAsync(string name, CancellationToken cancellationToken = default) =>
        resilience.ExecuteAsync(async ct =>
        {
            var pattern = ToWildcardPattern(name);

            return (IReadOnlyCollection<Hotel>)await context.Hotels
                .Where(h => EF.Functions.Like(h.Name, pattern))
                .OrderBy(h => h.Name)
                .ToListAsync(ct);
        }, cancellationToken);

    public Task<Hotel?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        resilience.ExecuteAsync(
            ct => context.Hotels.FirstOrDefaultAsync(h => h.Id == id, ct),
            cancellationToken);

    /// <summary>
    /// Turns free-text input into a SQL LIKE pattern: wrapped in wildcards on both ends,
    /// with spaces themselves also acting as wildcards. LIKE's own special characters
    /// ([, %, _) are bracket-escaped first, in that order, so a literal one in the search
    /// term isn't misread as pattern syntax. Case-insensitivity relies on the database's
    /// default collation (case-insensitive on Azure SQL and most SQL Server installs).
    /// </summary>
    private static string ToWildcardPattern(string term)
    {
        var escaped = term
            .Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]")
            .Replace(' ', '%');

        return $"%{escaped}%";
    }
}
