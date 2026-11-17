namespace Partilhae_Net.Services;

public interface IIdGenerator
{
    Task<long> ProximoIdAsync(
        string nomeSequencia,
        CancellationToken cancellationToken = default);
}