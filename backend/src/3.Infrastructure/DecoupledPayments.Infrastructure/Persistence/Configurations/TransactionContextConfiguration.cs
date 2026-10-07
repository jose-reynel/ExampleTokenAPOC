namespace DecoupledPayments.Infrastructure.Persistence.Configurations;

using DecoupledPayments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class TransactionContextConfiguration : IEntityTypeConfiguration<TransactionContext>
{
    // Garantiza Kind=Utc al leer (SQL Server devuelve Unspecified con datetime2).
    private static readonly ValueConverter<DateTime, DateTime> Utc =
        new(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    public void Configure(EntityTypeBuilder<TransactionContext> b)
    {
        b.ToTable("transactions", PaymentsDbContext.Schema);

        b.HasKey(x => x.TransactionId);
        b.Property(x => x.TransactionId).HasColumnName("transaction_id").ValueGeneratedNever();

        b.Property(x => x.MerchantId).HasColumnName("merchant_id").HasMaxLength(64).IsRequired();
        b.Property(x => x.UserDocumentId).HasColumnName("user_document_id").HasMaxLength(32).IsRequired();
        b.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2);
        b.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();

        // Estado como texto + token de concurrencia optimista:
        // UPDATE ... WHERE transaction_id = @id AND status = @estado_leído
        b.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired()
            .IsConcurrencyToken();

        b.Property(x => x.ExpectedOtp).HasColumnName("expected_otp").HasMaxLength(16).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasConversion(Utc);
        b.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasConversion(Utc);

        b.HasIndex(x => new { x.Status, x.ExpiresAt }).HasDatabaseName("ix_transactions_status_expires");
    }
}
