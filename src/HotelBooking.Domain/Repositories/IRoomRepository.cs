using HotelBooking.Domain.Entities;

namespace HotelBooking.Domain.Repositories;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rooms with enough capacity for the party size and no booked night overlapping the
    /// requested stay, optionally narrowed to a single hotel. Unscoped by default - the
    /// brief asks to "find available rooms", not "find available rooms at a hotel".
    /// </summary>
    Task<IReadOnlyCollection<Room>> GetAvailableRoomsAsync(
        DateOnly checkInDate,
        DateOnly checkOutDate,
        int guestCount,
        int? hotelId = null,
        CancellationToken cancellationToken = default);
}
