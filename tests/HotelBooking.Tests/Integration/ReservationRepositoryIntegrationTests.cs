using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Exceptions;
using HotelBooking.Infrastructure.Repositories;

namespace HotelBooking.Tests.Integration;

[Collection(DatabaseCollection.Name)]
public class ReservationRepositoryIntegrationTests(DatabaseFixture fixture)
{
    [IntegrationFact]
    public async Task AddAsync_WhenNightIsAlreadyBooked_ThrowsBookingConflictException()
    {
        await using var context = fixture.CreateContext();
        await using var data = await IntegrationTestData.CreateAsync(context);
        var repository = new ReservationRepository(context);

        var checkIn = new DateOnly(2027, 1, 10);
        var checkOut = new DateOnly(2027, 1, 12);

        var first = Reservation.Create(data.Room, checkIn, checkOut, [new Guest("Guest One", "guest.one@example.com")]);
        await repository.AddAsync(first);

        var second = Reservation.Create(data.Room, checkIn, checkOut, [new Guest("Guest Two", "guest.two@example.com")]);

        await Assert.ThrowsAsync<BookingConflictException>(() => repository.AddAsync(second));
    }

    [IntegrationFact]
    public async Task AddAsync_WhenStaysAreBackToBackOnTheSameRoom_BothSucceed()
    {
        // The checkout day itself is never a booked night, so a second stay starting
        // the same day the first one ends must not conflict.
        await using var context = fixture.CreateContext();
        await using var data = await IntegrationTestData.CreateAsync(context);
        var repository = new ReservationRepository(context);

        var first = Reservation.Create(
            data.Room, new DateOnly(2027, 2, 1), new DateOnly(2027, 2, 3),
            [new Guest("Guest One", "guest.one@example.com")]);
        await repository.AddAsync(first);

        var second = Reservation.Create(
            data.Room, new DateOnly(2027, 2, 3), new DateOnly(2027, 2, 5),
            [new Guest("Guest Two", "guest.two@example.com")]);
        await repository.AddAsync(second);

        Assert.NotEqual(0, first.Id);
        Assert.NotEqual(0, second.Id);
    }

    [IntegrationFact]
    public async Task GetByBookingReferenceAsync_ReturnsReservationWithRoomAndGuests()
    {
        await using var context = fixture.CreateContext();
        await using var data = await IntegrationTestData.CreateAsync(context);
        var repository = new ReservationRepository(context);

        var reservation = Reservation.Create(
            data.Room, new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 4),
            [new Guest("Guest One", "guest.one@example.com")]);
        await repository.AddAsync(reservation);

        await using var readContext = fixture.CreateContext();
        var readRepository = new ReservationRepository(readContext);

        var found = await readRepository.GetByBookingReferenceAsync(reservation.BookingReference);

        Assert.NotNull(found);
        Assert.Equal(reservation.BookingReference, found!.BookingReference);
        Assert.Equal(data.Room.Id, found.Room.Id);
        Assert.Equal(data.Hotel.Id, found.Room.Hotel.Id);
        Assert.Single(found.Guests);
    }

    [IntegrationFact]
    public async Task GetByBookingReferenceAsync_WhenReferenceIsUnknown_ReturnsNull()
    {
        await using var context = fixture.CreateContext();
        var repository = new ReservationRepository(context);

        var found = await repository.GetByBookingReferenceAsync("000000000");

        Assert.Null(found);
    }
}
