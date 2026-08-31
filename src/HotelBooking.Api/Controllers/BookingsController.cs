using HotelBooking.Api.Contracts;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController(IRoomRepository roomRepository, IReservationRepository reservationRepository) : ControllerBase
{
    /// <summary>
    /// Books a room for the given dates and guests. Invalid dates, capacity exceeded, and
    /// a genuine double-booking conflict are all raised as domain exceptions and mapped to
    /// the right status code by the global exception handler, not caught here.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<BookingResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingResponse>> Book([FromBody] BookRoomRequest request, CancellationToken cancellationToken)
    {
        if (request.Guests is not { Count: > 0 })
            return Problem(detail: "At least one guest is required.", statusCode: StatusCodes.Status400BadRequest);

        var room = await roomRepository.GetByIdAsync(request.RoomId, cancellationToken);
        if (room is null)
            return Problem(detail: $"Room {request.RoomId} was not found.", statusCode: StatusCodes.Status404NotFound);

        var guests = request.Guests.Select(g => new Guest(g.Name, g.Email)).ToList();

        var reservation = Reservation.Create(room, request.CheckInDate, request.CheckOutDate, guests);
        await reservationRepository.AddAsync(reservation, cancellationToken);

        return CreatedAtAction(
            nameof(GetByReference),
            new { bookingReference = reservation.BookingReference },
            ToResponse(reservation));
    }

    /// <summary>Finds a booking by its reference.</summary>
    [HttpGet("{bookingReference}")]
    [ProducesResponseType<BookingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingResponse>> GetByReference(string bookingReference, CancellationToken cancellationToken)
    {
        var reservation = await reservationRepository.GetByBookingReferenceAsync(bookingReference, cancellationToken);
        if (reservation is null)
            return NotFound();

        return Ok(ToResponse(reservation));
    }

    private static BookingResponse ToResponse(Reservation reservation) =>
        new(
            reservation.BookingReference,
            reservation.Room.Hotel.Name,
            reservation.Room.Number,
            reservation.Room.RoomType.Name,
            reservation.CheckInDate,
            reservation.CheckOutDate,
            reservation.Guests.Select(g => new GuestResponse(g.Name, g.Email)).ToList());
}
