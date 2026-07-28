using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class TurnConfiguration : IEntityTypeConfiguration<Turn>
{
    public void Configure(EntityTypeBuilder<Turn> builder)
    {
        builder.ToTable("AvailabilitySlots");

        builder.Ignore(t => t.IsAvailable);
        builder.Ignore(t => t.StartDateTime);

        builder.Property(t => t.AvailabilityId)
            .HasColumnName("AvailabilityRuleId");

        builder.Property(t => t.Status)
            .HasConversion(
                status => ToDbValue(status),
                value => FromDbValue(value))
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(TurnStatus.Available);

        builder.Property(t => t.Deleted).HasDefaultValue(false);

        builder.HasOne(t => t.Availability)
            .WithMany(a => a.Turns)
            .HasForeignKey(t => t.AvailabilityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Doctor)
            .WithMany()
            .HasForeignKey(t => t.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.DoctorId, t.Date, t.StartTime })
            .IsUnique()
            .HasFilter("[Deleted] = 0");
    }

    private static string ToDbValue(TurnStatus status) => status switch
    {
        TurnStatus.Available => "AVAILABLE",
        TurnStatus.Booked => "BOOKED",
        TurnStatus.Blocked => "BLOCKED",
        _ => status.ToString()
    };

    private static TurnStatus FromDbValue(string value) => value switch
    {
        "AVAILABLE" => TurnStatus.Available,
        "BOOKED" => TurnStatus.Booked,
        "BLOCKED" => TurnStatus.Blocked,
        _ => Enum.Parse<TurnStatus>(value, true)
    };
}