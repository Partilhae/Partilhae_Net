using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Partilhae_Net.Data;
using Partilhae_Net.Models;

namespace Partilhae_Net.Services;

public class ConversaService
{
    private readonly AppDbContext _db;

    public ConversaService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Conversa> CriarOuBuscarAsync(
        long idSolicitacao,
        long idDoador)
    {
        if (idSolicitacao <= 0 || idDoador <= 0)
            throw new ArgumentException("IDs inválidos.");

        var solicitacao = await _db.Solicitacoes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == idSolicitacao);

        if (solicitacao is null)
            throw new KeyNotFoundException(
                "Solicitação não encontrada.");

        var conversaExistente = await _db.Conversas
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.IdSolicitacao == idSolicitacao &&
                c.IdDoador == idDoador);

        if (conversaExistente is not null)
            return conversaExistente;

        if (solicitacao.Status != "ABERTA")
            throw new InvalidOperationException(
                "Esta solicitação não está aberta.");

        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var connection = _db.Database.GetDbConnection();

        await using var command = connection.CreateCommand();

        command.Transaction = transaction.GetDbTransaction();
        command.CommandText =
            "SELECT SEQ_CONVERSA.NEXTVAL FROM DUAL";

        var idConversa = Convert.ToInt64(
            await command.ExecuteScalarAsync());

        var conversa = new Conversa
        {
            Id = idConversa,
            IdSolicitacao = idSolicitacao,
            IdDoador = idDoador,
            IdReceptor = solicitacao.IdReceptor,
            DataCriacao = DateTime.UtcNow
        };

        _db.Conversas.Add(conversa);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return conversa;
    }
    
    public async Task<List<Conversa>> ListarPorUsuarioAsync(
        long idUsuario,
        long? idDoador,
        long? idReceptor)
    {
        if (idUsuario <= 0)
            throw new ArgumentException("Usuário inválido.");

        if (idDoador is null && idReceptor is null)
            return new List<Conversa>();

        return await _db.Conversas
            .AsNoTracking()
            .Where(c =>
                (idDoador.HasValue && c.IdDoador == idDoador.Value) ||
                (idReceptor.HasValue && c.IdReceptor == idReceptor.Value))
            .OrderByDescending(c => c.DataCriacao)
            .ToListAsync();
    }
        
    public async Task<List<Mensagem>> ListarMensagensAsync(
        long idConversa,
        long? idDoador,
        long? idReceptor)
    {
        var conversa = await _db.Conversas
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == idConversa);

        if (conversa is null)
            throw new KeyNotFoundException("Conversa não encontrada.");

        if (!PertenceAConversa(conversa, idDoador, idReceptor))
            throw new UnauthorizedAccessException(
                "Você não participa desta conversa.");

        return await _db.Mensagens
            .AsNoTracking()
            .Where(m => m.IdConversa == idConversa)
            .OrderBy(m => m.DataEnvio)
            .ThenBy(m => m.Id)
            .ToListAsync();
    }

    public async Task<Mensagem> EnviarMensagemAsync(
        long idConversa,
        long idUsuario,
        long? idDoador,
        long? idReceptor,
        string conteudo)
    {
        if (idUsuario <= 0)
            throw new ArgumentException("Usuário inválido.");

        if (string.IsNullOrWhiteSpace(conteudo))
            throw new ArgumentException(
                "A mensagem não pode estar vazia.");

        conteudo = conteudo.Trim();

        if (conteudo.Length > 1000)
            throw new ArgumentException(
                "A mensagem deve ter no máximo 1000 caracteres.");

        var conversa = await _db.Conversas
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == idConversa);

        if (conversa is null)
            throw new KeyNotFoundException("Conversa não encontrada.");

        if (!PertenceAConversa(conversa, idDoador, idReceptor))
            throw new UnauthorizedAccessException(
                "Você não participa desta conversa.");

        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var connection = _db.Database.GetDbConnection();

        await using var command = connection.CreateCommand();

        command.Transaction = transaction.GetDbTransaction();
        command.CommandText =
            "SELECT SEQ_MENSAGEM.NEXTVAL FROM DUAL";

        var idMensagem = Convert.ToInt64(
            await command.ExecuteScalarAsync());

        var mensagem = new Mensagem
        {
            Id = idMensagem,
            IdConversa = idConversa,
            IdRemetenteUsuario = idUsuario,
            Conteudo = conteudo,
            DataEnvio = DateTime.UtcNow
        };

        _db.Mensagens.Add(mensagem);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return mensagem;
    }

    private static bool PertenceAConversa(
        Conversa conversa,
        long? idDoador,
        long? idReceptor)
    {
        return (idDoador.HasValue &&
                conversa.IdDoador == idDoador.Value)
            || (idReceptor.HasValue &&
                conversa.IdReceptor == idReceptor.Value);
    }
}