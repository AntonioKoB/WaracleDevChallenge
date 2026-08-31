namespace HotelBooking.Domain.Exceptions;

/// <summary>
/// A genuine double-booking: another reservation already holds at least one of the
/// requested nights for this room. Raised by the infrastructure layer when the database's
/// unique constraint on (RoomId, StayDate) rejects the insert - see the README's
/// "Preventing overbooking" section.
/// </summary>
public class BookingConflictException()
    : DomainException("The room is no longer available for one or more of the requested nights.");
