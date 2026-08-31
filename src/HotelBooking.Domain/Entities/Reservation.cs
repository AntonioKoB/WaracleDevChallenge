using HotelBooking.Domain.Exceptions;

namespace HotelBooking.Domain.Entities;

public class Reservation
{
    private readonly List<Guest> _guests = [];
    private readonly List<ReservationNight> _nights = [];

    private Reservation()
    {
    }

    private Reservation(Room room, DateOnly checkInDate, DateOnly checkOutDate, IEnumerable<Guest> guests)
    {
        Room = room;
        CheckInDate = checkInDate;
        CheckOutDate = checkOutDate;
        BookingReference = BookingReferenceGenerator.Generate();
        _guests.AddRange(guests);

        for (var date = checkInDate; date < checkOutDate; date = date.AddDays(1))
        {
            _nights.Add(new ReservationNight(room, date));
        }
    }

    public int Id { get; private set; }

    public int RoomId { get; private set; }

    public Room Room { get; private set; } = null!;

    public string BookingReference { get; private set; } = string.Empty;

    public DateOnly CheckInDate { get; private set; }

    public DateOnly CheckOutDate { get; private set; }

    public IReadOnlyCollection<Guest> Guests => _guests;

    /// <summary>
    /// One row per booked night, all pointing at the same room - a reservation never splits
    /// a stay across rooms, and this is what the database enforces "no double booking" against.
    /// </summary>
    public IReadOnlyCollection<ReservationNight> Nights => _nights;

    /// <summary>
    /// The only way to build a Reservation, so the business rules below can never be bypassed:
    /// check-out after check-in, at least one guest, guest count within the room's capacity.
    /// </summary>
    public static Reservation Create(Room room, DateOnly checkInDate, DateOnly checkOutDate, IReadOnlyCollection<Guest> guests)
    {
        ArgumentNullException.ThrowIfNull(room);
        ArgumentNullException.ThrowIfNull(guests);

        if (checkOutDate <= checkInDate)
            throw new InvalidReservationDatesException(checkInDate, checkOutDate);

        if (guests.Count == 0)
            throw new ArgumentException("A reservation needs at least one guest.", nameof(guests));

        if (guests.Count > room.RoomType.Capacity)
            throw new RoomCapacityExceededException(room.RoomType.Capacity, guests.Count);

        return new Reservation(room, checkInDate, checkOutDate, guests);
    }
}
