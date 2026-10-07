namespace DecoupledPayments.Infrastructure.Persistence;

using DecoupledPayments.Domain.Entities;
using DecoupledPayments.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

public class PaymentsDbContext : DbContext
{
    public const string Schema = "payments";

    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options)
    {
    }

    public DbSet<TransactionContext> Transactions => Set<TransactionContext>();
    public DbSet<UsedToken> UsedTokens => Set<UsedToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentsDbContext).Assembly);
    }
}
