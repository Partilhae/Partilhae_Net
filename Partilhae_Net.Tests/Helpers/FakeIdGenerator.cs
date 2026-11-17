using Partilhae_Net.Services;

namespace Partilhae_Net.Tests.Helpers;

public class FakeIdGenerator : IIdGenerator
{
    private long _proximoId;

    public Task<long> ProximoIdAsync(
        string nomeSequencia,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            Interlocked.Increment(ref _proximoId));
    }
}