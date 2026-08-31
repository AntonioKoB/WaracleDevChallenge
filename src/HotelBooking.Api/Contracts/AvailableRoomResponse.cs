namespace HotelBooking.Api.Contracts;

public record AvailableRoomResponse(
    int RoomId,
    int HotelId,
    string HotelName,
    string Number,
    string RoomTypeName,
    int Capacity);
