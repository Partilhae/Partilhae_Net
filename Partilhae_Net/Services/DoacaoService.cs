using Microsoft.EntityFrameworkCore;
using Partilhae_Net.Data;
using Partilhae_Net.Models;

namespace Partilhae_Net.Services;

public class DoacaoService
{
    private readonly AppDbContext _db;
    private readonly IIdGenerator _idGenerator;
    private readonly IChatPermissionService _chatPermission;

    public DoacaoService(
        AppDbContext db,
        IIdGenerator idGenerator,
        IChatPermissionService chatPermission)
    {
        _db = db;
        _idGenerator = idGenerator;
        _chatPermission = chatPermission;
    }

    public async Task<Doacao> CriarAsync(
        long idConversa,
        long idDoador)
    {
        if (idConversa <= 0 || idDoador <= 0)
            throw new ArgumentException("IDs inválidos.");

        var conversa = await _db.Conversas
            .FirstOrDefaultAsync(c => c.Id == idConversa);

        if (conversa is null)
            throw new KeyNotFoundException(
                "Conversa não encontrada.");

        if (conversa.IdDoador != idDoador)
            throw new UnauthorizedAccessException(
                "Você não participa desta conversa como doador.");

        var doacaoExistente = await _db.Doacoes
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.IdConversa == idConversa);

        if (doacaoExistente is not null)
            throw new InvalidOperationException(
                "Já existe uma doação vinculada a esta conversa.");

        var solicitacao = await _db.Solicitacoes
            .FirstOrDefaultAsync(s =>
                s.Id == conversa.IdSolicitacao);

        if (solicitacao is null)
            throw new KeyNotFoundException(
                "Solicitação não encontrada.");

        if (solicitacao.Status != "ABERTA")
            throw new InvalidOperationException(
                "Esta solicitação não está disponível.");
        
        var chatLiberado = await _chatPermission
            .PodeConversarAsync(idConversa);

        if (!chatLiberado)
            throw new InvalidOperationException(
                "A conversa não está liberada para registrar uma doação.");

        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var idDoacao = await _idGenerator.ProximoIdAsync(
            "SEQ_DOACAO");

        var doacao = new Doacao
        {
            Id = idDoacao,
            IdConversa = conversa.Id,
            IdSolicitacao = conversa.IdSolicitacao,
            IdDoador = idDoador,
            Status = "EM_ANDAMENTO",
            DataCriacao = DateTime.UtcNow
        };

        _db.Doacoes.Add(doacao);

        solicitacao.Status = "EM_ANDAMENTO";

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return doacao;
    }
    
    public async Task<Doacao> AtualizarStatusAsync(
        long idDoacao,
        long? idDoador,
        long? idReceptor,
        string novoStatus)
    {
        if (idDoacao <= 0)
            throw new ArgumentException("ID da doação inválido.");

        if (string.IsNullOrWhiteSpace(novoStatus))
            throw new ArgumentException("Informe o status.");

        novoStatus = novoStatus.Trim().ToUpperInvariant();

        var doacao = await _db.Doacoes
            .FirstOrDefaultAsync(d => d.Id == idDoacao);

        if (doacao is null)
            throw new KeyNotFoundException("Doação não encontrada.");

        var conversa = await _db.Conversas
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == doacao.IdConversa);

        if (conversa is null)
            throw new KeyNotFoundException("Conversa não encontrada.");

        var agora = DateTime.UtcNow;

        if (novoStatus == "ENTREGUE")
        {
            if (idDoador != doacao.IdDoador)
                throw new UnauthorizedAccessException(
                    "Somente o doador pode marcar a entrega.");

            if (doacao.Status != "EM_ANDAMENTO")
                throw new InvalidOperationException(
                    "A doação não está em andamento.");

            doacao.Status = "ENTREGUE";
            doacao.DataEntregueDoador = agora;
        }
        else if (novoStatus == "CONCLUIDA")
        {
            if (idReceptor != conversa.IdReceptor)
                throw new UnauthorizedAccessException(
                    "Somente o receptor pode confirmar o recebimento.");

            if (doacao.Status != "ENTREGUE")
                throw new InvalidOperationException(
                    "O doador ainda não confirmou a entrega.");

            doacao.Status = "CONCLUIDA";
            doacao.DataRecebidoReceptor = agora;
            doacao.DataEncerramentoChat = agora;
            
            var solicitacao = await _db.Solicitacoes
                .FirstOrDefaultAsync(s => s.Id == doacao.IdSolicitacao);

            if (solicitacao is null)
                throw new KeyNotFoundException(
                    "Solicitação vinculada não encontrada.");

            solicitacao.Status = "CONCLUIDA";
        }
        else
        {
            throw new ArgumentException(
                "Status inválido. Utilize ENTREGUE ou CONCLUIDA.");
        }

        await _db.SaveChangesAsync();

        return doacao;
    }
}
