namespace DecoupledPayments.Infrastructure.Services;

using DecoupledPayments.Application.Interfaces;
using DecoupledPayments.Infrastructure.Persistence;
using DecoupledPayments.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Anti-replay respaldado por la base de datos: INSERT del jti sobre una PK única.
/// Es atómico entre réplicas y funciona igual en PostgreSQL y SQL Server.
/// </summary>
public class DbReplayProtectionService : IReplayProtectionService
{
    private readonly PaymentsDbContext _db;

    public DbReplayProtectionService(PaymentsDbContext db) => _db = db;

    public async Task<bool> TryRegisterJtiAsync(string jti, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        _db.UsedTokens.Add(new UsedToken { Jti = jti, ExpiresAt = DateTime.UtcNow.Add(ttl) });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();

            // Independiente del motor: si la fila ya existe, fue un uso repetido; si no, es otro error.
            bool alreadyUsed = await _db.UsedTokens.AsNoTracking()
                .AnyAsync(t => t.Jti == jti, cancellationToken);

            if (alreadyUsed)
                return false;

            throw;
        }
    }
}
