using Partilhae_Net.Models;
using Partilhae_Net.Services;
using Partilhae_Net.Tests.Helpers;

namespace Partilhae_Net.Tests;

public class DoacaoServiceTests
{
    private static async Task PrepararDadosAsync(
        Partilhae_Net.Data.AppDbContext db,
        string statusDoacao = "EM_ANDAMENTO",
        string statusSolicitacao = "EM_ANDAMENTO")
    {
        db.Solicitacoes.Add(new Solicitacao
        {
            Id = 1,
            IdReceptor = 20,
            Titulo = "Alimentos",
            Descricao = "Preciso de alimentos",
            TipoAjuda = "ESPECIFICA",
            Status = statusSolicitacao,
            DataCriacao = DateTime.UtcNow
        });

        db.Conversas.Add(new Conversa
        {
            Id = 1,
            IdSolicitacao = 1,
            IdDoador = 10,
            IdReceptor = 20,
            DataCriacao = DateTime.UtcNow
        });

        db.Doacoes.Add(new Doacao
        {
            Id = 1,
            IdConversa = 1,
            IdSolicitacao = 1,
            IdDoador = 10,
            Status = statusDoacao,
            DataCriacao = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public async Task CriarAsync_DeveRejeitarIdsInvalidos(
        long idConversa, long idDoador)
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        var service = new DoacaoService(db);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CriarAsync(idConversa, idDoador));
    }

    [Fact]
    public async Task CriarAsync_DeveRejeitarConversaInexistente()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        var service = new DoacaoService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CriarAsync(999, 10));
    }

    [Fact]
    public async Task CriarAsync_DeveBloquearDoadorDiferente()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        await PrepararDadosAsync(db);

        var service = new DoacaoService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CriarAsync(1, 99));
    }

    [Fact]
    public async Task CriarAsync_DeveImpedirDoacaoDuplicada()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        await PrepararDadosAsync(db);

        var service = new DoacaoService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CriarAsync(1, 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task AtualizarStatusAsync_DeveRejeitarIdInvalido(long id)
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        var service = new DoacaoService(db);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.AtualizarStatusAsync(id, 10, 20, "ENTREGUE"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task AtualizarStatusAsync_DeveRejeitarStatusVazio(
        string status)
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        var service = new DoacaoService(db);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.AtualizarStatusAsync(1, 10, 20, status));
    }

    [Fact]
    public async Task AtualizarStatusAsync_DeveRejeitarDoacaoInexistente()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        var service = new DoacaoService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.AtualizarStatusAsync(999, 10, 20, "ENTREGUE"));
    }

    [Fact]
    public async Task AtualizarStatusAsync_DeveBloquearDoadorDiferente()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        await PrepararDadosAsync(db);

        var service = new DoacaoService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.AtualizarStatusAsync(1, 99, 20, "ENTREGUE"));
    }

    [Fact]
    public async Task AtualizarStatusAsync_DeveMarcarDoacaoComoEntregue()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        await PrepararDadosAsync(db);

        var service = new DoacaoService(db);

        var resultado = await service.AtualizarStatusAsync(
            1, 10, 20, " entregue ");

        Assert.Equal("ENTREGUE", resultado.Status);
        Assert.NotNull(resultado.DataEntregueDoador);

        db.ChangeTracker.Clear();

        var salva = await db.Doacoes.FindAsync(1L);

        Assert.NotNull(salva);
        Assert.Equal("ENTREGUE", salva.Status);
    }

    [Fact]
    public async Task AtualizarStatusAsync_DeveImpedirEntregaDuplicada()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        await PrepararDadosAsync(db, "ENTREGUE");

        var service = new DoacaoService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AtualizarStatusAsync(1, 10, 20, "ENTREGUE"));
    }

    [Fact]
    public async Task AtualizarStatusAsync_DeveBloquearReceptorDiferente()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        await PrepararDadosAsync(db, "ENTREGUE");

        var service = new DoacaoService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.AtualizarStatusAsync(1, 10, 99, "CONCLUIDA"));
    }

    [Fact]
    public async Task AtualizarStatusAsync_DeveImpedirConclusaoAntesDaEntrega()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        await PrepararDadosAsync(db);

        var service = new DoacaoService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AtualizarStatusAsync(1, 10, 20, "CONCLUIDA"));
    }

    [Fact]
    public async Task AtualizarStatusAsync_DeveConcluirDoacaoESolicitacao()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        await PrepararDadosAsync(db, "ENTREGUE");

        var service = new DoacaoService(db);

        var resultado = await service.AtualizarStatusAsync(
            1, 10, 20, "CONCLUIDA");

        Assert.Equal("CONCLUIDA", resultado.Status);
        Assert.NotNull(resultado.DataRecebidoReceptor);
        Assert.NotNull(resultado.DataEncerramentoChat);

        db.ChangeTracker.Clear();

        var doacaoSalva = await db.Doacoes.FindAsync(1L);
        var solicitacaoSalva = await db.Solicitacoes.FindAsync(1L);

        Assert.NotNull(doacaoSalva);
        Assert.NotNull(solicitacaoSalva);

        Assert.Equal("CONCLUIDA", doacaoSalva.Status);
        Assert.Equal("CONCLUIDA", solicitacaoSalva.Status);
    }

    [Theory]
    [InlineData("CANCELADA")]
    [InlineData("ABERTA")]
    public async Task AtualizarStatusAsync_DeveRejeitarStatusNaoPermitido(
        string status)
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        await PrepararDadosAsync(db);

        var service = new DoacaoService(db);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.AtualizarStatusAsync(1, 10, 20, status));
    }
}
