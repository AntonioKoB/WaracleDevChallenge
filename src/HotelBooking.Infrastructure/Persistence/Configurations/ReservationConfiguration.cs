using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.BookingReference)
            .HasMaxLength(9)
            .IsRequired();

        // The name of this index is checked in ReservationRepository to tell a booking
        // reference collision (retry with a new reference) apart from a genuine
        // double-booking conflict on IX_ReservationNights_RoomId_StayDate (return 409).
        builder.HasIndex(r => r.BookingReference)
            .IsUnique()
            .HasDatabaseName("IX_Reservations_BookingReference");

        builder.Property(r => r.CheckInDate)
            .IsRequired();

        builder.Property(r => r.CheckOutDate)
            .IsRequired();

        builder.HasOne(r => r.Room)
            .WithMany()
            .HasForeignKey(r => r.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Guests)
            .WithOne()
            .HasForeignKey(g => g.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(r => r.Guests)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(r => r.Nights)
            .WithOne(n => n.Reservation)
            .HasForeignKey(n => n.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(r => r.Nights)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
