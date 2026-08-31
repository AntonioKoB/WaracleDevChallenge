namespace HotelBooking.Domain.Exceptions;

public class InvalidReservationDatesException(DateOnly checkInDate, DateOnly checkOutDate)
    : DomainException($"Check-out ({checkOutDate:yyyy-MM-dd}) must be after check-in ({checkInDate:yyyy-MM-dd}).")
{
    public DateOnly CheckInDate { get; } = checkInDate;

    public DateOnly CheckOutDate { get; } = checkOutDate;
}
