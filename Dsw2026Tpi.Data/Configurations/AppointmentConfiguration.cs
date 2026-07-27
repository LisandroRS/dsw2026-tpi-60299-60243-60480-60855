using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations
{
    public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
    {
        public void Configure(EntityTypeBuilder<Appointment> builder)
        {
            builder.ToTable("Appointments");

            builder.Ignore(a => a.IsActive);

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

            builder.Property(a => a.RowVersion)
                .IsRowVersion();

            builder.HasOne(a => a.Availability)
                .WithMany()
                .HasForeignKey(a => a.AvailabilityId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.Patient)
                .WithMany()
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(a => a.AvailabilityId)
                .IsUnique()
                .HasFilter("[Status] = 'BOOKED'");

            builder.HasIndex(a => a.PatientId);


        }
        private static string ToDbValue(AppointmentStatus status) => status switch
        {
            AppointmentStatus.Booked => "BOOKED",
            AppointmentStatus.Cancelled => "CANCELLED",
            AppointmentStatus.Attended => "ATTENDED",
            AppointmentStatus.NoShow => "NOSHOW",
                    _=> status.ToString()
        };

        private static AppointmentStatus FromDbValue(string value) => value switch
        {
            "BOOKED" => AppointmentStatus.Booked,
            "CANCELLED" => AppointmentStatus.Cancelled,
            "ATTENDED" => AppointmentStatus.Attended,
            "NOSHOW" => AppointmentStatus.NoShow,
                    _=> Enum.Parse<AppointmentStatus>(value, true)
        };
    }
}