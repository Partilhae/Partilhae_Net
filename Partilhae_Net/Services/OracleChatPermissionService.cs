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
        var quantidade = await _db.Database
            .SqlQueryRaw<int>(
                """
                SELECT COUNT(*) AS "Value"
                FROM CONVERSA c
                JOIN SOLICITACAO s
                    ON s.ID_SOLICITACAO = c.SOLICITACAO_ID_SOLICITACAO
                JOIN DOADOR dr
                    ON dr.ID_DOADOR = c.DOADOR_ID_DOADOR
                JOIN USUARIO ud
                    ON ud.ID_USUARIO = dr.USUARIO_ID_USUARIO
                JOIN RECEPTOR r
                    ON r.ID_RECEPTOR = c.RECEPTOR_ID_RECEPTOR
                JOIN USUARIO ur
                    ON ur.ID_USUARIO = r.USUARIO_ID_USUARIO
                LEFT JOIN DOACAO d
                    ON d.CONVERSA_ID_CONVERSA = c.ID_CONVERSA
                WHERE c.ID_CONVERSA = {0}
                    AND ud.TP_STATUS_CONTA = 'ATIVA'
                    AND ur.TP_STATUS_CONTA = 'ATIVA'
                    AND (
                        (d.ID_DOACAO IS NULL AND s.TP_STATUS = 'ABERTA')
                        OR d.TP_STATUS IN ('EM_ANDAMENTO', 'ENTREGUE')
                    )
                """,
                idConversa)
            .SingleAsync();

        return quantidade > 0;
    }
}