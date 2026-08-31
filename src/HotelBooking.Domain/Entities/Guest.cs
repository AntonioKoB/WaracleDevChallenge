namespace HotelBooking.Domain.Entities;

public class Guest
{
    private Guest()
    {
    }

    public Guest(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A guest needs a name.", nameof(name));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("A guest needs an email.", nameof(email));

        Name = name;
        Email = email;
    }

    public int Id { get; private set; }

    public int ReservationId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;
}
