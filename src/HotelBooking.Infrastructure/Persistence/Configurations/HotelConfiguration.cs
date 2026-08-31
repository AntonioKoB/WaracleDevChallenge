using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class HotelConfiguration : IEntityTypeConfiguration<Hotel>
{
    public void Configure(EntityTypeBuilder<Hotel> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(h => h.Address)
            .HasMaxLength(400)
            .IsRequired();

        // "Find a hotel based on its name" is a direct lookup, so it needs an index.
        builder.HasIndex(h => h.Name)
            .HasDatabaseName("IX_Hotels_Name");

        builder.HasMany(h => h.Rooms)
            .WithOne(r => r.Hotel)
            .HasForeignKey(r => r.HotelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(h => h.Rooms)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
