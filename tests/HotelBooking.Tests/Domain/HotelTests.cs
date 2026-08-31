using HotelBooking.Domain.Entities;

namespace HotelBooking.Tests.Domain;

public class HotelTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name)
    {
        Assert.Throws<ArgumentException>(() => new Hotel(name!, "1 Test Street"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithInvalidAddress_ThrowsArgumentException(string? address)
    {
        Assert.Throws<ArgumentException>(() => new Hotel("Test Hotel", address!));
    }

    [Fact]
    public void Constructor_WithValidData_SetsNameAndAddress()
    {
        var hotel = new Hotel("Test Hotel", "1 Test Street");

        Assert.Equal("Test Hotel", hotel.Name);
        Assert.Equal("1 Test Street", hotel.Address);
    }
}
