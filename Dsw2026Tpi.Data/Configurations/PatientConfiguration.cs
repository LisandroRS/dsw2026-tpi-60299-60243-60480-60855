using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");

        builder.Property(p => p.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(p => p.Dni)
            .IsRequired();

        builder.Property(p => p.FullName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.Deleted)
            .HasDefaultValue(false);

        builder.Property(p => p.IdentityUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.HasIndex(p => p.Email)
            .IsUnique();

        builder.HasIndex(p => p.Dni)
            .IsUnique();

        builder.HasIndex(p => p.IdentityUserId)
            .IsUnique();
    }
}