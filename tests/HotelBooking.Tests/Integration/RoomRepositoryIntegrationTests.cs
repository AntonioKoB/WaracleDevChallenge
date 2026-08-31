using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Repositories;

namespace HotelBooking.Tests.Integration;

[Collection(DatabaseCollection.Name)]
public class RoomRepositoryIntegrationTests(DatabaseFixture fixture)
{
    [IntegrationFact]
    public async Task GetAvailableRoomsAsync_ExcludesARoomBookedForAnOverlappingNight()
    {
        await using var context = fixture.CreateContext();
        await using var data = await IntegrationTestData.CreateAsync(context);

        var reservationRepository = new ReservationRepository(context);
        var reservation = Reservation.Create(
            data.Room, new DateOnly(2027, 4, 10), new DateOnly(2027, 4, 13),
            [new Guest("Guest One", "guest.one@example.com")]);
        await reservationRepository.AddAsync(reservation);

        var roomRepository = new RoomRepository(context);
        var available = await roomRepository.GetAvailableRoomsAsync(
            data.Hotel.Id, new DateOnly(2027, 4, 11), new DateOnly(2027, 4, 12), guestCount: 1);

        Assert.DoesNotContain(available, r => r.Id == data.Room.Id);
    }

    [IntegrationFact]
    public async Task GetAvailableRoomsAsync_IncludesARoomWithNoOverlappingBooking()
    {
        await using var context = fixture.CreateContext();
        await using var data = await IntegrationTestData.CreateAsync(context);

        var roomRepository = new RoomRepository(context);
        var available = await roomRepository.GetAvailableRoomsAsync(
            data.Hotel.Id, new DateOnly(2027, 5, 1), new DateOnly(2027, 5, 3), guestCount: 1);

        Assert.Contains(available, r => r.Id == data.Room.Id);
    }

    [IntegrationFact]
    public async Task GetAvailableRoomsAsync_ExcludesRoomsBelowTheRequestedGuestCount()
    {
        await using var context = fixture.CreateContext();
        await using var data = await IntegrationTestData.CreateAsync(context, capacity: 1);

        var roomRepository = new RoomRepository(context);
        var available = await roomRepository.GetAvailableRoomsAsync(
            data.Hotel.Id, new DateOnly(2027, 6, 1), new DateOnly(2027, 6, 3), guestCount: 2);

        Assert.DoesNotContain(available, r => r.Id == data.Room.Id);
    }
}
