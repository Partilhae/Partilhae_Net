namespace Partilhae_Net.Services;

public interface IChatPermissionService
{
    Task<bool> PodeConversarAsync(long idConversa);
}