using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class AvailabilityConfiguration : IEntityTypeConfiguration<Availability>
{
    public void Configure(EntityTypeBuilder<Availability> builder)
    {
        builder.ToTable("Availabilities");

        builder.Ignore(a => a.IsAvailable);                   

        builder.Property(a => a.Status)                        
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(SlotStatus.Available);

        builder.Property(a => a.Deleted)                       
            .HasDefaultValue(false);

        builder.HasOne(a => a.Doctor)
            .WithMany()
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.DoctorId, a.StartDateTime })  
            .IsUnique()
            .HasFilter("[Deleted] = 0");    // si nosotros no filtramos aca trae tambien disponibilidades borradas        
    }
}