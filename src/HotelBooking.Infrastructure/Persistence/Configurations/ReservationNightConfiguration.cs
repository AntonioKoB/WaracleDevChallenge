using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class ReservationNightConfiguration : IEntityTypeConfiguration<ReservationNight>
{
    public void Configure(EntityTypeBuilder<ReservationNight> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.StayDate)
            .IsRequired();

        // This is what actually prevents overbooking: two reservations can never insert a
        // row for the same room on the same night. See the README's "Preventing overbooking"
        // section for how the concurrency guarantee this gives us actually works.
        builder.HasIndex(n => new { n.RoomId, n.StayDate })
            .IsUnique()
            .HasDatabaseName("IX_ReservationNights_RoomId_StayDate");

        builder.HasOne(n => n.Room)
            .WithMany()
            .HasForeignKey(n => n.RoomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
