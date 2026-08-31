using HotelBooking.Api.Contracts;
using HotelBooking.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/hotels")]
public class HotelsController(IHotelRepository hotelRepository) : ControllerBase
{
    /// <summary>
    /// Searches hotels by name - case-insensitive, matches anywhere in the name. "waracle"
    /// matches "The Grand Waracle"; a space in the search term is itself a wildcard, so
    /// "grand waracle" still matches even with other words in between.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<HotelResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<HotelResponse>>> Search([FromQuery] string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Problem(detail: "A hotel name is required.", statusCode: StatusCodes.Status400BadRequest);

        var hotels = await hotelRepository.SearchByNameAsync(name, cancellationToken);

        return Ok(hotels.Select(h => new HotelResponse(h.Id, h.Name, h.Address)).ToList());
    }
}
