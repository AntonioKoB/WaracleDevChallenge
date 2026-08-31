namespace HotelBooking.Domain.Exceptions;

public class RoomCapacityExceededException(int capacity, int guestCount)
    : DomainException($"The room's capacity is {capacity}, but {guestCount} guests were requested.")
{
    public int Capacity { get; } = capacity;

    public int GuestCount { get; } = guestCount;
}
