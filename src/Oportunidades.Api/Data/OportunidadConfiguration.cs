using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oportunidades.Api.Domain;

namespace Oportunidades.Api.Data;

public sealed class OportunidadConfiguration : IEntityTypeConfiguration<Oportunidad>
{
    public void Configure(EntityTypeBuilder<Oportunidad> builder)
    {
        builder.ToTable("Oportunidades", table =>
            table.HasCheckConstraint(
                "CK_Oportunidades_MontoEstimado_Positivo",
                "[MontoEstimado] > 0"));

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .ValueGeneratedOnAdd();

        builder.Property(o => o.Nombre)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(o => o.Cliente)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(o => o.MontoEstimado)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(o => o.Estado)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.FechaCreacionUtc)
            .HasColumnType("datetime2(7)")
            .HasConversion(
                valor => valor,
                valor => DateTime.SpecifyKind(valor, DateTimeKind.Utc))
            .IsRequired();

        builder.HasIndex(o => new { o.Estado, o.FechaCreacionUtc })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Oportunidades_Estado_FechaCreacionUtc");
    }
}
