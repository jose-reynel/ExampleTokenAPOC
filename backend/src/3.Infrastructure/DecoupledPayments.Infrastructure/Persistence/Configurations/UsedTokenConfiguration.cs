namespace DecoupledPayments.Infrastructure.Persistence.Configurations;

using DecoupledPayments.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class UsedTokenConfiguration : IEntityTypeConfiguration<UsedToken>
{
    private static readonly ValueConverter<DateTime, DateTime> Utc =
        new(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    public void Configure(EntityTypeBuilder<UsedToken> b)
    {
        b.ToTable("used_tokens", PaymentsDbContext.Schema);

        // La PK única es lo que hace atómico el "consumir una sola vez".
        b.HasKey(x => x.Jti);
        b.Property(x => x.Jti).HasColumnName("jti").HasMaxLength(64).ValueGeneratedNever();
        b.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasConversion(Utc);

        b.HasIndex(x => x.ExpiresAt).HasDatabaseName("ix_used_tokens_expires_at");
    }
}
