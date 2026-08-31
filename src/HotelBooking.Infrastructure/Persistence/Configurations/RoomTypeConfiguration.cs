using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class RoomTypeConfiguration : IEntityTypeConfiguration<RoomType>
{
    public void Configure(EntityTypeBuilder<RoomType> builder)
    {
        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Name)
            .HasMaxLength(50)
            .IsRequired();

        // Single/Double/Deluxe: a shared lookup, not duplicated per hotel.
        builder.HasIndex(rt => rt.Name)
            .IsUnique()
            .HasDatabaseName("IX_RoomTypes_Name");
    }
}
