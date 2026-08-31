using System.Security.Cryptography;

namespace HotelBooking.Domain;

/// <summary>
/// Generates a random numeric booking reference. It is deliberately not derived from the
/// reservation's auto-increment Id or from a timestamp: with no authentication on the API,
/// a sequential or time-derived reference would let anyone enumerate other guests' bookings.
///
/// Uniqueness is guaranteed by the database's unique constraint on Reservation.BookingReference,
/// not by this generator - a collision here is astronomically unlikely (1 in 10^9), but the
/// repository still retries on the rare conflict rather than assuming it can never happen.
/// </summary>
public static class BookingReferenceGenerator
{
    private const int Length = 9;

    public static string Generate()
    {
        Span<char> reference = stackalloc char[Length];
        for (var i = 0; i < Length; i++)
        {
            reference[i] = (char)('0' + RandomNumberGenerator.GetInt32(10));
        }

        return new string(reference);
    }
}
