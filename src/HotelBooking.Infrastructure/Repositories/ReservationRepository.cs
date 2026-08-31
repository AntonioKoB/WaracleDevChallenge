using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Exceptions;
using HotelBooking.Domain.Repositories;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class ReservationRepository(HotelBookingDbContext context) : IReservationRepository
{
    private const string BookingReferenceIndex = "IX_Reservations_BookingReference";
    private const string OverlappingNightIndex = "IX_ReservationNights_RoomId_StayDate";
    private const int MaxBookingReferenceAttempts = 3;

    public async Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        var current = reservation;

        for (var attempt = 1; ; attempt++)
        {
            context.Reservations.Add(current);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateException ex) when (IsUniqueIndexViolation(ex, OverlappingNightIndex))
            {
                // A genuine double-booking: another request won the race for this room/night.
                throw new BookingConflictException();
            }
            catch (DbUpdateException ex) when (IsUniqueIndexViolation(ex, BookingReferenceIndex) && attempt < MaxBookingReferenceAttempts)
            {
                // A collision on the randomly generated reference itself (see
                // BookingReferenceGenerator) - astronomically unlikely, but retried rather
                // than assumed impossible. Only the failed reservation graph is detached;
                // the room (and its hotel/room type) stay tracked as they were.
                DetachGraph(current);
                current = Reservation.Create(current.Room, current.CheckInDate, current.CheckOutDate, current.Guests);
            }
        }
    }

    public Task<Reservation?> GetByBookingReferenceAsync(string bookingReference, CancellationToken cancellationToken = default) =>
        context.Reservations
            .Include(r => r.Room).ThenInclude(room => room.Hotel)
            .Include(r => r.Room).ThenInclude(room => room.RoomType)
            .Include(r => r.Guests)
            .FirstOrDefaultAsync(r => r.BookingReference == bookingReference, cancellationToken);

    private void DetachGraph(Reservation reservation)
    {
        context.Entry(reservation).State = EntityState.Detached;

        foreach (var guest in reservation.Guests)
            context.Entry(guest).State = EntityState.Detached;

        foreach (var night in reservation.Nights)
            context.Entry(night).State = EntityState.Detached;
    }

    private static bool IsUniqueIndexViolation(DbUpdateException exception, string indexName) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 } sqlException &&
        sqlException.Message.Contains(indexName, StringComparison.Ordinal);
}
