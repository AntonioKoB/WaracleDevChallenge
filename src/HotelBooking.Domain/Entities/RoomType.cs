namespace HotelBooking.Domain.Entities;

public class RoomType
{
    private RoomType()
    {
    }

    public RoomType(string name, int capacity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A room type needs a name.", nameof(name));

        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be at least 1.");

        Name = name;
        Capacity = capacity;
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int Capacity { get; private set; }
}
