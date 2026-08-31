using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Tests.Integration;

/// <summary>
/// A hotel, room type and room created fresh for one test, with unique enough names to
/// never collide with another run - and deleted again on disposal, so the shared test
/// database stays empty regardless of how many times CI executes.
/// </summary>
internal sealed class IntegrationTestData : IAsyncDisposable
{
    private readonly HotelBookingDbContext _context;

    private IntegrationTestData(HotelBookingDbContext context, Hotel hotel, RoomType roomType, Room room)
    {
        _context = context;
        Hotel = hotel;
        RoomType = roomType;
        Room = room;
    }

    public Hotel Hotel { get; }

    public RoomType RoomType { get; }

    public Room Room { get; }

    public static async Task<IntegrationTestData> CreateAsync(HotelBookingDbContext context, int capacity = 2)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var hotel = new Hotel($"Test Hotel {suffix}", "1 Test Street");
        var roomType = new RoomType($"TestType-{suffix}", capacity);
        var room = new Room(hotel, roomType, "101");

        context.Hotels.Add(hotel);
        context.RoomTypes.Add(roomType);
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        return new IntegrationTestData(context, hotel, roomType, room);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.Reservations.Where(r => r.RoomId == Room.Id).ExecuteDeleteAsync();
        await _context.Rooms.Where(r => r.Id == Room.Id).ExecuteDeleteAsync();
        await _context.RoomTypes.Where(rt => rt.Id == RoomType.Id).ExecuteDeleteAsync();
        await _context.Hotels.Where(h => h.Id == Hotel.Id).ExecuteDeleteAsync();
    }
}
