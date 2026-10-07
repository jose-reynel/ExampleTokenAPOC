namespace DecoupledPayments.Infrastructure.Persistence;

using DecoupledPayments.Application.Interfaces;
using DecoupledPayments.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class EfTransactionRepository : ITransactionRepository
{
    private readonly PaymentsDbContext _db;

    public EfTransactionRepository(PaymentsDbContext db) => _db = db;

    public async Task SaveAsync(TransactionContext transaction)
    {
        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();
    }

    public Task<TransactionContext?> GetByIdAsync(Guid transactionId)
        => _db.Transactions.FirstOrDefaultAsync(t => t.TransactionId == transactionId);

    public async Task UpdateAsync(TransactionContext transaction)
    {
        // Entidad leída por este mismo contexto: el seguimiento de cambios basta.
        if (_db.Entry(transaction).State == EntityState.Detached)
            _db.Transactions.Update(transaction);

        // Lanza DbUpdateConcurrencyException si otro proceso cambió el estado entre lectura y escritura.
        await _db.SaveChangesAsync();
    }
}
