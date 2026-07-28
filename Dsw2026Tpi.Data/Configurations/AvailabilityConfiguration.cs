using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;


    public class AvailabilityConfiguration : IEntityTypeConfiguration<Availability>
    {
        public void Configure(EntityTypeBuilder<Availability> builder)
        {
            builder.ToTable("AvailabilityRules");

            builder.Property(a => a.Year)
                .HasConversion<short>()
                .IsRequired();

            builder.Property(a => a.Month)
                .HasConversion<byte>()
                .IsRequired();

            builder.Property(a => a.DayOfWeek)
                .HasConversion<byte>()
                .IsRequired();

            builder.Property(a => a.StartTime).IsRequired();
            builder.Property(a => a.EndTime).IsRequired();

            builder.Property(a => a.Deleted).HasDefaultValue(false);

            builder.HasOne(a => a.Doctor)
                .WithMany()
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(a => new
            {
                a.DoctorId,
                a.Year,
                a.Month,
                a.DayOfWeek,
                a.StartTime,
                a.EndTime
            })
            .IsUnique()
            .HasFilter("[Deleted] = 0");
        }
    }
