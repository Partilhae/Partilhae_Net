using Partilhae_Net.Services;

namespace Partilhae_Net.Tests.Helpers;

public class FakeChatPermissionService : IChatPermissionService
{
    public bool ChatLiberado { get; set; } = true;

    public Task<bool> PodeConversarAsync(long idConversa)
    {
        return Task.FromResult(ChatLiberado);
    }
}