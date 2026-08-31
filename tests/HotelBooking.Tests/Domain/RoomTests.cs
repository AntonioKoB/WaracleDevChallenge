using HotelBooking.Domain.Entities;

namespace HotelBooking.Tests.Domain;

public class RoomTests
{
    private static Hotel CreateHotel() => new("Test Hotel", "1 Test Street");

    private static RoomType CreateRoomType() => new("Single", 1);

    [Fact]
    public void Constructor_WithNullHotel_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Room(null!, CreateRoomType(), "101"));
    }

    [Fact]
    public void Constructor_WithNullRoomType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Room(CreateHotel(), null!, "101"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithInvalidNumber_ThrowsArgumentException(string? number)
    {
        Assert.Throws<ArgumentException>(() => new Room(CreateHotel(), CreateRoomType(), number!));
    }

    [Fact]
    public void Constructor_WithValidData_SetsHotelRoomTypeAndNumber()
    {
        var hotel = CreateHotel();
        var roomType = CreateRoomType();

        var room = new Room(hotel, roomType, "101");

        Assert.Same(hotel, room.Hotel);
        Assert.Same(roomType, room.RoomType);
        Assert.Equal("101", room.Number);
    }
}
