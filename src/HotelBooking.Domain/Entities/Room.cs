namespace HotelBooking.Domain.Entities;

public class Room
{
    private Room()
    {
    }

    public Room(Hotel hotel, RoomType roomType, string number)
    {
        ArgumentNullException.ThrowIfNull(hotel);
        ArgumentNullException.ThrowIfNull(roomType);

        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("A room needs a number.", nameof(number));

        Hotel = hotel;
        RoomType = roomType;
        Number = number;
    }

    public int Id { get; private set; }

    public int HotelId { get; private set; }

    public Hotel Hotel { get; private set; } = null!;

    public int RoomTypeId { get; private set; }

    public RoomType RoomType { get; private set; } = null!;

    public string Number { get; private set; } = string.Empty;
}
