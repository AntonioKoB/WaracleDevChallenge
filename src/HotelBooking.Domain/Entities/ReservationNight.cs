namespace HotelBooking.Domain.Entities;

/// <summary>
/// One booked night for a room. The unique constraint on (RoomId, StayDate), enforced in the
/// database, is what actually prevents two reservations from double-booking the same night -
/// see the "Preventing overbooking" section in the README.
/// </summary>
public class ReservationNight
{
    private ReservationNight()
    {
    }

    internal ReservationNight(Room room, DateOnly stayDate)
    {
        Room = room;
        StayDate = stayDate;
    }

    public int Id { get; private set; }

    public int RoomId { get; private set; }

    public Room Room { get; private set; } = null!;

    public DateOnly StayDate { get; private set; }

    public int ReservationId { get; private set; }

    public Reservation Reservation { get; private set; } = null!;
}
