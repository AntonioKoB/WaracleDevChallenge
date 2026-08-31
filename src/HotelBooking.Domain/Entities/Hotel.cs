namespace HotelBooking.Domain.Entities;

public class Hotel
{
    private readonly List<Room> _rooms = [];

    private Hotel()
    {
    }

    public Hotel(string name, string address)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A hotel needs a name.", nameof(name));

        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("A hotel needs an address.", nameof(address));

        Name = name;
        Address = address;
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Address { get; private set; } = string.Empty;

    public IReadOnlyCollection<Room> Rooms => _rooms;
}
