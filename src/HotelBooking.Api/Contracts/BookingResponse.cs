namespace HotelBooking.Api.Contracts;

public record BookingResponse(
    string BookingReference,
    string HotelName,
    string RoomNumber,
    string RoomTypeName,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    IReadOnlyCollection<GuestResponse> Guests);

public record GuestResponse(string Name, string Email);
