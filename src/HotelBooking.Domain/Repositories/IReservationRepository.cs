using HotelBooking.Domain.Entities;

namespace HotelBooking.Domain.Repositories;

public interface IReservationRepository
{
    /// <summary>
    /// Persists the reservation and its nights in a single transaction. The unique constraint
    /// on (RoomId, StayDate) is what can make this throw a conflict for an overlapping booking.
    /// </summary>
    Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default);

    Task<Reservation?> GetByBookingReferenceAsync(string bookingReference, CancellationToken cancellationToken = default);
}
