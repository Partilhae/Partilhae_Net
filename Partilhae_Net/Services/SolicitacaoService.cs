using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Partilhae_Net.Data;
using Partilhae_Net.DTOs;
using Partilhae_Net.Models;

namespace Partilhae_Net.Services;

public class SolicitacaoService
{
    private readonly AppDbContext _db;

    public SolicitacaoService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Solicitacao> CriarAsync(
        long idReceptor,
        CriarSolicitacaoDto dto)
    {
        var tipoAjuda = dto.TipoAjuda.Trim().ToUpperInvariant();

        if (tipoAjuda == "ESPECIFICO")
            tipoAjuda = "ESPECIFICA";

        if (!SolicitacaoValidator.TipoAjudaValido(dto.TipoAjuda))
            throw new ArgumentException("Tipo de ajuda inválido.");

        if (string.IsNullOrWhiteSpace(dto.Titulo))
            throw new ArgumentException("O título é obrigatório.");

        if (dto.Itens.Any(i =>
                !SolicitacaoValidator.QuantidadeValida(i.Quantidade)))
            throw new ArgumentException("A quantidade dos itens deve ser positiva.");

        var solicitacao = new Solicitacao
        {
            IdReceptor = idReceptor,
            Titulo = dto.Titulo.Trim(),
            Descricao = dto.Descricao.Trim(),
            TipoAjuda = tipoAjuda,
            Status = "ABERTA",
            DataCriacao = DateTime.UtcNow,
            Itens = dto.Itens.Select(item => new ItemSolicitacao
            {
                Nome = item.Nome.Trim(),
                Quantidade = item.Quantidade,
                Unidade = item.Unidade.Trim()
            }).ToList()
        };

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var connection = _db.Database.GetDbConnection();

        await using (var command = connection.CreateCommand())
        {
            command.Transaction = _db.Database.CurrentTransaction!
                .GetDbTransaction();

            command.CommandText = "SELECT SEQ_SOLICITACAO.NEXTVAL FROM DUAL";

            solicitacao.Id = Convert.ToInt64(
                await command.ExecuteScalarAsync());
        }

        foreach (var item in solicitacao.Itens)
        {
            await using var command = connection.CreateCommand();

            command.Transaction = _db.Database.CurrentTransaction!
                .GetDbTransaction();

            command.CommandText = "SELECT SEQ_ITEM.NEXTVAL FROM DUAL";

            item.Id = Convert.ToInt64(
                await command.ExecuteScalarAsync());
        }

        _db.Solicitacoes.Add(solicitacao);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return solicitacao;
    }
    
    
    public async Task<List<Solicitacao>> ListarAsync(string? status)
    {
        var consulta = _db.Solicitacoes
            .AsNoTracking()
            .Include(s => s.Itens)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            consulta = consulta.Where(s =>
                s.Status == status.Trim().ToUpper());
        }

        return await consulta
            .OrderByDescending(s => s.DataCriacao)
            .ToListAsync();
    }

    public async Task<Solicitacao?> BuscarPorIdAsync(long id)
    {
        return await _db.Solicitacoes
            .AsNoTracking()
            .Include(s => s.Itens)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<Solicitacao>> ListarPorReceptorAsync(long idReceptor)
    {
        return await _db.Solicitacoes
            .AsNoTracking()
            .Include(s => s.Itens)
            .Where(s => s.IdReceptor == idReceptor)
            .OrderByDescending(s => s.DataCriacao)
            .ToListAsync();
    }
    
    public async Task<Solicitacao?> AtualizarStatusAsync(
        long id,
        long idReceptor,
        string status)
    {
        var solicitacao = await _db.Solicitacoes
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitacao is null)
            return null;

        if (solicitacao.IdReceptor != idReceptor)
            throw new UnauthorizedAccessException();

        var novoStatus = status.Trim().ToUpperInvariant();

        if (novoStatus != "CANCELADA")
            throw new ArgumentException(
                "O receptor só pode cancelar a solicitação por esta rota.");

        if (!SolicitacaoValidator.PodeCancelar(
                solicitacao.Status,
                solicitacao.IdReceptor,
                idReceptor))
        {
            throw new InvalidOperationException(
                "Somente solicitações abertas podem ser canceladas.");
        }

        solicitacao.Status = novoStatus;

        await _db.SaveChangesAsync();

        return solicitacao;
    }
}