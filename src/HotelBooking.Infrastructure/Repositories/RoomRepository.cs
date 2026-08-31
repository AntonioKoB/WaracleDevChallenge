using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Repositories;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class RoomRepository(HotelBookingDbContext context) : IRoomRepository
{
    public Task<Room?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        context.Rooms
            .Include(r => r.Hotel)
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Room>> GetAvailableRoomsAsync(
        int hotelId,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        int guestCount,
        CancellationToken cancellationToken = default)
    {
        return await context.Rooms
            .Include(r => r.RoomType)
            .Include(r => r.Hotel)
            .Where(r => r.HotelId == hotelId && r.RoomType.Capacity >= guestCount)
            // A room is available if none of its booked nights fall inside the requested
            // stay - this mirrors the (RoomId, StayDate) uniqueness the database enforces.
            .Where(r => !context.ReservationNights.Any(n =>
                n.RoomId == r.Id && n.StayDate >= checkInDate && n.StayDate < checkOutDate))
            .OrderBy(r => r.Number)
            .ToListAsync(cancellationToken);
    }
}
