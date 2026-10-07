namespace DecoupledPayments.Application.Interfaces;

public interface IReplayProtectionService
{
    /// <summary>
    /// Registra el jti de forma atómica (equivalente a Redis SET NX EX).
    /// Retorna true si es la primera vez que se usa; false si ya fue consumido.
    /// </summary>
    Task<bool> TryRegisterJtiAsync(string jti, TimeSpan ttl, CancellationToken cancellationToken = default);
}
