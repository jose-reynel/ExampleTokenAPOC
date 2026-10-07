namespace DecoupledPayments.Application.Interfaces;

using DecoupledPayments.Domain.Entities;

public interface ITransactionRepository
{
    Task SaveAsync(TransactionContext transaction);
    Task<TransactionContext?> GetByIdAsync(Guid transactionId);
    Task UpdateAsync(TransactionContext transaction);
}
