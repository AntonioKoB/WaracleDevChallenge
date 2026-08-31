namespace HotelBooking.Infrastructure.Seeding;

public record SeedSummary(
    IReadOnlyCollection<string> HotelNames,
    IReadOnlyCollection<SeededReservation> Reservations);

public record SeededReservation(
    string BookingReference,
    string HotelName,
    string RoomNumber,
    DateOnly CheckInDate,
    DateOnly CheckOutDate);
