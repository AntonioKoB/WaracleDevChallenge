using HotelBooking.Domain.Entities;

namespace HotelBooking.Tests.Domain;

public class GuestTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name)
    {
        Assert.Throws<ArgumentException>(() => new Guest(name!, "guest@example.com"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithInvalidEmail_ThrowsArgumentException(string? email)
    {
        Assert.Throws<ArgumentException>(() => new Guest("Guest", email!));
    }

    [Fact]
    public void Constructor_WithValidData_SetsNameAndEmail()
    {
        var guest = new Guest("Guest", "guest@example.com");

        Assert.Equal("Guest", guest.Name);
        Assert.Equal("guest@example.com", guest.Email);
    }
}
