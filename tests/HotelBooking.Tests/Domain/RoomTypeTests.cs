using HotelBooking.Domain.Entities;

namespace HotelBooking.Tests.Domain;

public class RoomTypeTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveCapacity_ThrowsArgumentOutOfRangeException(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RoomType("Single", capacity));
    }

    [Fact]
    public void Constructor_WithValidData_SetsNameAndCapacity()
    {
        var roomType = new RoomType("Deluxe", 4);

        Assert.Equal("Deluxe", roomType.Name);
        Assert.Equal(4, roomType.Capacity);
    }
}
