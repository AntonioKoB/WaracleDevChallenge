using HotelBooking.Infrastructure.Seeding;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Controllers;

/// <summary>
/// Testing-only endpoints for populating and clearing data. Not part of the hotel
/// booking business API itself.
/// </summary>
[ApiController]
[Route("api/testing")]
public class TestingController(TestDataSeeder seeder) : ControllerBase
{
    /// <summary>
    /// Clears all data and populates the database with a small, known set of hotels,
    /// room types, rooms, and a couple of reservations - enough to exercise every
    /// business endpoint by hand. Safe to call repeatedly; it resets before seeding.
    /// Returns the seeded hotel names and booking references, so there's something to
    /// test against straight away without needing to query the database directly.
    /// </summary>
    [HttpPost("seed")]
    public async Task<ActionResult<SeedSummary>> Seed(CancellationToken cancellationToken)
    {
        var summary = await seeder.SeedAsync(cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// Removes all data, leaving the database empty and ready for a fresh seed.
    /// </summary>
    [HttpPost("reset")]
    public async Task<IActionResult> Reset(CancellationToken cancellationToken)
    {
        await seeder.ResetAsync(cancellationToken);
        return NoContent();
    }
}
