using HotelBooking.Api.Contracts;
using HotelBooking.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/rooms")]
public class RoomsController(IRoomRepository roomRepository, IHotelRepository hotelRepository) : ControllerBase
{
    /// <summary>
    /// Finds rooms free for the whole stay and with enough capacity for the party size.
    /// Unscoped by default, across every hotel - like searching a booking site before
    /// picking one. Pass <c>hotelId</c> to narrow the search to a single hotel instead.
    /// </summary>
    /// <param name="checkInDate">Format: yyyy-MM-dd, e.g. 2026-09-07. Other formats are rejected rather than guessed at.</param>
    /// <param name="checkOutDate">Format: yyyy-MM-dd, e.g. 2026-09-10.</param>
    /// <param name="guests">Party size. Must be at least 1.</param>
    /// <param name="hotelId">Optional - narrows the search to a single hotel.</param>
    /// <param name="cancellationToken"></param>
    [HttpGet("available")]
    [ProducesResponseType<IReadOnlyCollection<AvailableRoomResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<AvailableRoomResponse>>> GetAvailableRooms(
        [FromQuery] DateOnly checkInDate,
        [FromQuery] DateOnly checkOutDate,
        [FromQuery] int guests,
        [FromQuery] int? hotelId,
        CancellationToken cancellationToken)
    {
        if (checkOutDate <= checkInDate)
            return Problem(detail: "checkOutDate must be after checkInDate.", statusCode: StatusCodes.Status400BadRequest);

        if (guests <= 0)
            return Problem(detail: "guests must be at least 1.", statusCode: StatusCodes.Status400BadRequest);

        if (hotelId is not null)
        {
            var hotel = await hotelRepository.GetByIdAsync(hotelId.Value, cancellationToken);
            if (hotel is null)
                return NotFound();
        }

        var rooms = await roomRepository.GetAvailableRoomsAsync(checkInDate, checkOutDate, guests, hotelId, cancellationToken);

        return Ok(rooms
            .Select(r => new AvailableRoomResponse(r.Id, r.Hotel.Id, r.Hotel.Name, r.Number, r.RoomType.Name, r.RoomType.Capacity))
            .ToList());
    }
}
