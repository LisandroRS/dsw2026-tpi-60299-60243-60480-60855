using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");

        builder.Ignore(a => a.IsActive);

        builder.Property(a => a.TurnId)
            .HasColumnName("AvailabilitySlotId");

        builder.Property(a => a.Reason)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(a => a.Status)
            .HasConversion(
                status => ToDbValue(status),
                value => FromDbValue(value))
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(AppointmentStatus.Booked);

        builder.HasOne(a => a.Turn)
            .WithMany()
            .HasForeignKey(a => a.TurnId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Patient)
            .WithMany()
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.TurnId)
            .IsUnique()
            .HasFilter("[Status] = 'BOOKED'");

        builder.HasIndex(a => a.PatientId);
    }

    private static string ToDbValue(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Booked => "BOOKED",
        AppointmentStatus.Cancelled => "CANCELLED",
        AppointmentStatus.Attended => "ATTENDED",
        AppointmentStatus.NoShow => "NO_SHOW",
        _ => status.ToString()
    };

    private static AppointmentStatus FromDbValue(string value) => value switch
    {
        "BOOKED" => AppointmentStatus.Booked,
        "CANCELLED" => AppointmentStatus.Cancelled,
        "ATTENDED" => AppointmentStatus.Attended,
        "NO_SHOW" => AppointmentStatus.NoShow,
        _ => Enum.Parse<AppointmentStatus>(value, true)
    };
}