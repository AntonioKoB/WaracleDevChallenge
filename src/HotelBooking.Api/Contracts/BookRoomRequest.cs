namespace HotelBooking.Api.Contracts;

/// <param name="RoomId">The room to book.</param>
/// <param name="CheckInDate">Format: yyyy-MM-dd, e.g. 2026-09-07.</param>
/// <param name="CheckOutDate">Format: yyyy-MM-dd, e.g. 2026-09-10.</param>
/// <param name="Guests">At least one guest is required.</param>
public record BookRoomRequest(int RoomId, DateOnly CheckInDate, DateOnly CheckOutDate, IReadOnlyCollection<GuestRequest> Guests);

public record GuestRequest(string Name, string Email);
