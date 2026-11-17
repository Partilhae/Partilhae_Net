using Microsoft.EntityFrameworkCore;
using Partilhae_Net.Data;

namespace Partilhae_Net.Services;

public class OracleChatPermissionService : IChatPermissionService
{
    private readonly AppDbContext _db;

    public OracleChatPermissionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> PodeConversarAsync(long idConversa)
    {
        var chatLiberado = await _db.Database
            .SqlQueryRaw<string>(
                "SELECT pkg_doacao.fn_pode_conversar({0}) AS \"Value\" FROM DUAL",
                idConversa)
            .SingleAsync();

        return chatLiberado == "S";
    }
}