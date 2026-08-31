using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Exceptions;

namespace HotelBooking.Tests.Domain;

public class ReservationTests
{
    private static Room CreateRoom(int capacity = 2)
    {
        var hotel = new Hotel("Test Hotel", "1 Test Street");
        var roomType = new RoomType("Double", capacity);
        return new Room(hotel, roomType, "101");
    }

    private static List<Guest> CreateGuests(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new Guest($"Guest {i}", $"guest{i}@example.com"))
            .ToList();

    [Fact]
    public void Create_WithValidData_SetsBookingReferenceAndDates()
    {
        var room = CreateRoom();
        var guests = CreateGuests(2);
        var checkIn = new DateOnly(2026, 9, 10);
        var checkOut = new DateOnly(2026, 9, 13);

        var reservation = Reservation.Create(room, checkIn, checkOut, guests);

        Assert.NotEmpty(reservation.BookingReference);
        Assert.Equal(checkIn, reservation.CheckInDate);
        Assert.Equal(checkOut, reservation.CheckOutDate);
        Assert.Equal(2, reservation.Guests.Count);
    }

    [Theory]
    [InlineData(2026, 9, 10, 2026, 9, 10)] // same day
    [InlineData(2026, 9, 10, 2026, 9, 9)] // checkout before checkin
    public void Create_WhenCheckOutIsNotAfterCheckIn_ThrowsInvalidReservationDatesException(
        int inYear, int inMonth, int inDay, int outYear, int outMonth, int outDay)
    {
        var room = CreateRoom();
        var guests = CreateGuests(1);
        var checkIn = new DateOnly(inYear, inMonth, inDay);
        var checkOut = new DateOnly(outYear, outMonth, outDay);

        Assert.Throws<InvalidReservationDatesException>(() => Reservation.Create(room, checkIn, checkOut, guests));
    }

    [Fact]
    public void Create_WithNoGuests_ThrowsArgumentException()
    {
        var room = CreateRoom();
        var checkIn = new DateOnly(2026, 9, 10);
        var checkOut = new DateOnly(2026, 9, 12);

        Assert.Throws<ArgumentException>(() => Reservation.Create(room, checkIn, checkOut, []));
    }

    [Fact]
    public void Create_WhenGuestCountExceedsRoomCapacity_ThrowsRoomCapacityExceededException()
    {
        var room = CreateRoom(capacity: 2);
        var guests = CreateGuests(3);
        var checkIn = new DateOnly(2026, 9, 10);
        var checkOut = new DateOnly(2026, 9, 12);

        var exception = Assert.Throws<RoomCapacityExceededException>(
            () => Reservation.Create(room, checkIn, checkOut, guests));

        Assert.Equal(2, exception.Capacity);
        Assert.Equal(3, exception.GuestCount);
    }

    [Fact]
    public void Create_GeneratesOneNightPerStayDate_ExcludingCheckOutDate()
    {
        var room = CreateRoom();
        var guests = CreateGuests(1);
        var checkIn = new DateOnly(2026, 9, 10);
        var checkOut = new DateOnly(2026, 9, 13); // 3 nights: 10th, 11th, 12th

        var reservation = Reservation.Create(room, checkIn, checkOut, guests);

        var expectedNights = new[]
        {
            new DateOnly(2026, 9, 10),
            new DateOnly(2026, 9, 11),
            new DateOnly(2026, 9, 12),
        };

        Assert.Equal(expectedNights, reservation.Nights.Select(n => n.StayDate).OrderBy(d => d));
    }

    [Fact]
    public void Create_AllNightsBelongToTheBookedRoom()
    {
        // A stay is never split across rooms - every generated night must reference
        // the same room the reservation was made for.
        var room = CreateRoom();
        var guests = CreateGuests(1);
        var checkIn = new DateOnly(2026, 9, 10);
        var checkOut = new DateOnly(2026, 9, 14);

        var reservation = Reservation.Create(room, checkIn, checkOut, guests);

        Assert.All(reservation.Nights, night => Assert.Same(room, night.Room));
    }
}
