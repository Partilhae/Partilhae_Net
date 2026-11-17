using Partilhae_Net.Data;
using Partilhae_Net.Models;
using Partilhae_Net.Services;
using Partilhae_Net.Tests.Helpers;

namespace Partilhae_Net.Tests;

public class ConversaServiceTests
{
    private static async Task PrepararDadosAsync(AppDbContext db)
    {
        db.Solicitacoes.Add(new Solicitacao
        {
            Id = 1,
            IdReceptor = 20,
            Titulo = "Doação de alimentos",
            Descricao = "Preciso de alimentos",
            TipoAjuda = "ESPECIFICA",
            Status = "ABERTA",
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

        await db.SaveChangesAsync();
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public async Task CriarOuBuscarAsync_DeveRejeitarIdsInvalidos(
        long idSolicitacao, long idDoador)
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        var service = new ConversaService(db);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CriarOuBuscarAsync(idSolicitacao, idDoador));
    }

    [Fact]
    public async Task CriarOuBuscarAsync_DeveRejeitarSolicitacaoInexistente()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        var service = new ConversaService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CriarOuBuscarAsync(999, 10));
    }

    [Fact]
    public async Task CriarOuBuscarAsync_DeveRetornarConversaExistente()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        await PrepararDadosAsync(db);

        var service = new ConversaService(db);

        var resultado = await service.CriarOuBuscarAsync(1, 10);

        Assert.Equal(1, resultado.Id);
        Assert.Equal(10, resultado.IdDoador);
    }

    [Fact]
    public async Task CriarOuBuscarAsync_DeveBloquearSolicitacaoFechada()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        await PrepararDadosAsync(db);

        var solicitacao = await db.Solicitacoes.FindAsync(1L);
        Assert.NotNull(solicitacao);
        solicitacao.Status = "CONCLUIDA";
        await db.SaveChangesAsync();

        var service = new ConversaService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CriarOuBuscarAsync(1, 99));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ListarPorUsuarioAsync_DeveRejeitarUsuarioInvalido(
        long idUsuario)
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        var service = new ConversaService(db);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ListarPorUsuarioAsync(idUsuario, 10, 20));
    }

    [Fact]
    public async Task ListarPorUsuarioAsync_SemPapeis_DeveRetornarListaVazia()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        await PrepararDadosAsync(db);

        var service = new ConversaService(db);

        var resultado = await service.ListarPorUsuarioAsync(
            100, null, null);

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task ListarPorUsuarioAsync_DeveListarConversasDoDoador()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        await PrepararDadosAsync(db);

        var service = new ConversaService(db);

        var resultado = await service.ListarPorUsuarioAsync(
            100, 10, null);

        Assert.Single(resultado);
        Assert.Equal(10, resultado[0].IdDoador);
    }

    [Fact]
    public async Task ListarPorUsuarioAsync_DeveListarConversasDoReceptor()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        await PrepararDadosAsync(db);

        var service = new ConversaService(db);

        var resultado = await service.ListarPorUsuarioAsync(
            100, null, 20);

        Assert.Single(resultado);
        Assert.Equal(20, resultado[0].IdReceptor);
    }

    [Fact]
    public async Task ListarPorUsuarioAsync_DeveRetornarVazioParaOutroUsuario()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        await PrepararDadosAsync(db);

        var service = new ConversaService(db);

        var resultado = await service.ListarPorUsuarioAsync(
            100, 99, 88);

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task ListarMensagensAsync_DeveRejeitarConversaInexistente()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        var service = new ConversaService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.ListarMensagensAsync(999, 10, null));
    }

    [Fact]
    public async Task ListarMensagensAsync_DeveBloquearUsuarioNaoParticipante()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        await PrepararDadosAsync(db);

        var service = new ConversaService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.ListarMensagensAsync(1, 99, 88));
    }

    [Fact]
    public async Task ListarMensagensAsync_DevePermitirDoador()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        await PrepararDadosAsync(db);

        var service = new ConversaService(db);

        var resultado = await service.ListarMensagensAsync(
            1, 10, null);

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task ListarMensagensAsync_DevePermitirReceptor()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        await PrepararDadosAsync(db);

        var service = new ConversaService(db);

        var resultado = await service.ListarMensagensAsync(
            1, null, 20);

        Assert.Empty(resultado);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task EnviarMensagemAsync_DeveRejeitarUsuarioInvalido(
        long idUsuario)
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        var service = new ConversaService(db);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.EnviarMensagemAsync(
                1, idUsuario, 10, null, "Olá"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task EnviarMensagemAsync_DeveRejeitarMensagemVazia(
        string conteudo)
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        var service = new ConversaService(db);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.EnviarMensagemAsync(
                1, 100, 10, null, conteudo));
    }

    [Fact]
    public async Task EnviarMensagemAsync_DeveRejeitarMensagemMuitoLonga()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        var service = new ConversaService(db);

        var conteudo = new string('A', 1001);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.EnviarMensagemAsync(
                1, 100, 10, null, conteudo));
    }

    [Fact]
    public async Task EnviarMensagemAsync_DeveRejeitarConversaInexistente()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        var service = new ConversaService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.EnviarMensagemAsync(
                999, 100, 10, null, "Olá"));
    }

    [Fact]
    public async Task EnviarMensagemAsync_DeveBloquearNaoParticipante()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        await PrepararDadosAsync(db);

        var service = new ConversaService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.EnviarMensagemAsync(
                1, 100, 99, 88, "Olá"));
    }
}