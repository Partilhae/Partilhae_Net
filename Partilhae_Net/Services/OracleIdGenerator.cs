using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Partilhae_Net.Data;

namespace Partilhae_Net.Services;

public class OracleIdGenerator : IIdGenerator
{
    private readonly AppDbContext _db;

    public OracleIdGenerator(AppDbContext db)
    {
        _db = db;
    }

    public async Task<long> ProximoIdAsync(
        string nomeSequencia,
        CancellationToken cancellationToken = default)
    {
        var sequenciasPermitidas = new HashSet<string>
        {
            "SEQ_SOLICITACAO",
            "SEQ_ITEM",
            "SEQ_CONVERSA",
            "SEQ_MENSAGEM",
            "SEQ_DOACAO"
        };

        if (!sequenciasPermitidas.Contains(nomeSequencia))
            throw new ArgumentException("Sequência inválida.");

        var connection = _db.Database.GetDbConnection();

        await using var command = connection.CreateCommand();

        if (_db.Database.CurrentTransaction is not null)
        {
            command.Transaction = _db.Database
                .CurrentTransaction.GetDbTransaction();
        }

        command.CommandText =
            $"SELECT {nomeSequencia}.NEXTVAL FROM DUAL";

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        var resultado = await command.ExecuteScalarAsync(
            cancellationToken);

        return Convert.ToInt64(resultado);
    }
}