using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Repositories;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class HotelRepository(HotelBookingDbContext context) : IHotelRepository
{
    public Task<Hotel?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        context.Hotels.FirstOrDefaultAsync(h => h.Name == name, cancellationToken);

    public Task<Hotel?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        context.Hotels.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
}
