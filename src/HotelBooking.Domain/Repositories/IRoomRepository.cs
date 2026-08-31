using HotelBooking.Domain.Entities;

namespace HotelBooking.Domain.Repositories;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rooms at the given hotel with enough capacity for the party size, and with no
    /// booked night overlapping the requested stay.
    /// </summary>
    Task<IReadOnlyCollection<Room>> GetAvailableRoomsAsync(
        int hotelId,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        int guestCount,
        CancellationToken cancellationToken = default);
}
