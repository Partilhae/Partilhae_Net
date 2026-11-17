using Microsoft.EntityFrameworkCore;
using Partilhae_Net.Data;
using Partilhae_Net.Models;
using Partilhae_Net.Services;
using Partilhae_Net.Tests.Helpers;
using Xunit;

namespace Partilhae_Net.Tests.Services;

public class DoacaoCriacaoTests
{
    private static async Task PrepararBanco(
        AppDbContext db,
        string status = "ABERTA")
    {
        db.Solicitacoes.Add(new Solicitacao
        {
            Id = 500,
            IdReceptor = 10,
            Titulo = "Doação de alimentos",
            Descricao = "Alimentos",
            TipoAjuda = "ESPECIFICA",
            Status = status,
            DataCriacao = DateTime.UtcNow
        });

        db.Conversas.Add(new Conversa
        {
            Id = 600,
            IdSolicitacao = 500,
            IdDoador = 20,
            IdReceptor = 10,
            DataCriacao = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CriarAsync_DeveCriarDoacaoComSucesso()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        await PrepararBanco(db);

        var service = new DoacaoService(
            db,
            new FakeIdGenerator(),
            new FakeChatPermissionService());

        var doacao = await service.CriarAsync(600, 20);

        Assert.True(doacao.Id > 0);
        Assert.Equal(600L, doacao.IdConversa);
        Assert.Equal(500L, doacao.IdSolicitacao);
        Assert.Equal(20L, doacao.IdDoador);
        Assert.Equal("EM_ANDAMENTO", doacao.Status);
    }

    [Fact]
    public async Task CriarAsync_DevePersistirDoacao()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        await PrepararBanco(db);

        var service = new DoacaoService(
            db,
            new FakeIdGenerator(),
            new FakeChatPermissionService());

        var doacao = await service.CriarAsync(600, 20);

        db.ChangeTracker.Clear();

        var salva = await db.Doacoes
            .SingleAsync(d => d.Id == doacao.Id);

        Assert.Equal("EM_ANDAMENTO", salva.Status);
        Assert.Equal(600L, salva.IdConversa);
        Assert.Equal(20L, salva.IdDoador);
    }

    [Fact]
    public async Task CriarAsync_DeveAtualizarStatusDaSolicitacao()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        await PrepararBanco(db);

        var service = new DoacaoService(
            db,
            new FakeIdGenerator(),
            new FakeChatPermissionService());

        await service.CriarAsync(600, 20);

        db.ChangeTracker.Clear();

        var solicitacao = await db.Solicitacoes
            .SingleAsync(s => s.Id == 500);

        Assert.Equal("EM_ANDAMENTO", solicitacao.Status);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(600, 0)]
    [InlineData(600, -1)]
    public async Task CriarAsync_DeveRejeitarIdsInvalidos(
        long idConversa,
        long idDoador)
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        var service = new DoacaoService(
            db,
            new FakeIdGenerator(),
            new FakeChatPermissionService());

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CriarAsync(idConversa, idDoador));

        Assert.Empty(await db.Doacoes.ToListAsync());
    }

    [Fact]
    public async Task CriarAsync_DeveRejeitarConversaInexistente()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        var service = new DoacaoService(
            db,
            new FakeIdGenerator(),
            new FakeChatPermissionService());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CriarAsync(999, 20));

        Assert.Empty(await db.Doacoes.ToListAsync());
    }

    [Fact]
    public async Task CriarAsync_DeveRejeitarOutroDoador()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        await PrepararBanco(db);

        var service = new DoacaoService(
            db,
            new FakeIdGenerator(),
            new FakeChatPermissionService());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CriarAsync(600, 99));

        Assert.Empty(await db.Doacoes.ToListAsync());
    }

    [Fact]
    public async Task CriarAsync_DeveImpedirDoacaoDuplicada()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        await PrepararBanco(db);

        var service = new DoacaoService(
            db,
            new FakeIdGenerator(),
            new FakeChatPermissionService());

        await service.CriarAsync(600, 20);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CriarAsync(600, 20));

        Assert.Equal(
            "Já existe uma doação vinculada a esta conversa.",
            ex.Message);

        Assert.Single(await db.Doacoes.ToListAsync());
    }

    [Theory]
    [InlineData("CANCELADA")]
    [InlineData("CONCLUIDA")]
    public async Task CriarAsync_DeveRejeitarSolicitacaoIndisponivel(
        string status)
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        await PrepararBanco(db, status);

        var service = new DoacaoService(
            db,
            new FakeIdGenerator(),
            new FakeChatPermissionService());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CriarAsync(600, 20));

        Assert.Equal(
            "Esta solicitação não está disponível.",
            ex.Message);

        Assert.Empty(await db.Doacoes.ToListAsync());
    }

    [Fact]
    public async Task CriarAsync_DeveRejeitarChatNaoLiberado()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        await PrepararBanco(db);

        var fakeChat = new FakeChatPermissionService
        {
            ChatLiberado = false
        };

        var service = new DoacaoService(
            db,
            new FakeIdGenerator(),
            fakeChat);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CriarAsync(600, 20));

        Assert.Equal(
            "A conversa não está liberada para registrar uma doação.",
            ex.Message);

        Assert.Empty(await db.Doacoes.ToListAsync());

        db.ChangeTracker.Clear();

        var solicitacao = await db.Solicitacoes
            .SingleAsync(s => s.Id == 500);

        Assert.Equal("ABERTA", solicitacao.Status);
    }
}