using Microsoft.EntityFrameworkCore;
using Partilhae_Net.Models;
using Partilhae_Net.Services;
using Partilhae_Net.Tests.Helpers;
using Xunit;

namespace Partilhae_Net.Tests.Services;

public class ConversaCriacaoTests
{
    private static async Task PrepararSolicitacao(
        Partilhae_Net.Data.AppDbContext db)
    {
        db.Solicitacoes.Add(new Solicitacao
        {
            Id = 100,
            IdReceptor = 10,
            Titulo = "Doação de alimentos",
            Descricao = "Precisamos de ajuda",
            TipoAjuda = "ESPECIFICA",
            Status = "ABERTA",
            DataCriacao = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Criar_ConversaValida_DevePersistir()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        await PrepararSolicitacao(db);

        var service = new ConversaService(
            db, new FakeIdGenerator());

        var conversa = await service.CriarOuBuscarAsync(100, 20);

        Assert.True(conversa.Id > 0);
        Assert.Equal(100L, conversa.IdSolicitacao);
        Assert.Equal(20L, conversa.IdDoador);
        Assert.Equal(10L, conversa.IdReceptor);

        db.ChangeTracker.Clear();

        Assert.NotNull(
            await db.Conversas.FindAsync(conversa.Id));
    }

    [Fact]
    public async Task Criar_ConversaExistente_NaoDeveDuplicar()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        await PrepararSolicitacao(db);

        var service = new ConversaService(
            db, new FakeIdGenerator());

        var primeira = await service.CriarOuBuscarAsync(100, 20);
        var segunda = await service.CriarOuBuscarAsync(100, 20);

        Assert.Equal(primeira.Id, segunda.Id);
        Assert.Equal(1, await db.Conversas.CountAsync());
    }

    [Fact]
    public async Task Criar_DoadoresDiferentes_DeveCriarConversasDistintas()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        await PrepararSolicitacao(db);

        var service = new ConversaService(
            db, new FakeIdGenerator());

        var primeira = await service.CriarOuBuscarAsync(100, 20);
        var segunda = await service.CriarOuBuscarAsync(100, 30);

        Assert.NotEqual(primeira.Id, segunda.Id);
        Assert.Equal(2, await db.Conversas.CountAsync());
    }

    [Fact]
    public async Task Enviar_DoadorParticipante_DeveSalvarMensagem()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        await PrepararSolicitacao(db);

        var service = new ConversaService(
            db, new FakeIdGenerator());

        var conversa = await service.CriarOuBuscarAsync(100, 20);

        var mensagem = await service.EnviarMensagemAsync(
            conversa.Id, 200, 20, null, "Olá!");

        Assert.True(mensagem.Id > 0);
        Assert.Equal(conversa.Id, mensagem.IdConversa);
        Assert.Equal(200L, mensagem.IdRemetenteUsuario);
        Assert.Equal("Olá!", mensagem.Conteudo);

        db.ChangeTracker.Clear();

        Assert.NotNull(
            await db.Mensagens.FindAsync(mensagem.Id));
    }

    [Fact]
    public async Task Enviar_ReceptorParticipante_DeveSalvarMensagem()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        await PrepararSolicitacao(db);

        var service = new ConversaService(
            db, new FakeIdGenerator());

        var conversa = await service.CriarOuBuscarAsync(100, 20);

        var mensagem = await service.EnviarMensagemAsync(
            conversa.Id, 100, null, 10, "Muito obrigada!");

        Assert.Equal(100L, mensagem.IdRemetenteUsuario);
        Assert.Equal("Muito obrigada!", mensagem.Conteudo);
        Assert.Equal(1, await db.Mensagens.CountAsync());
    }

    [Fact]
    public async Task Enviar_DeveRemoverEspacos()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        await PrepararSolicitacao(db);

        var service = new ConversaService(
            db, new FakeIdGenerator());

        var conversa = await service.CriarOuBuscarAsync(100, 20);

        var mensagem = await service.EnviarMensagemAsync(
            conversa.Id, 200, 20, null, "  Posso ajudar!  ");

        Assert.Equal("Posso ajudar!", mensagem.Conteudo);
    }

    [Fact]
    public async Task Enviar_ComMilCaracteres_DeveAceitar()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        await PrepararSolicitacao(db);

        var service = new ConversaService(
            db, new FakeIdGenerator());

        var conversa = await service.CriarOuBuscarAsync(100, 20);

        var mensagem = await service.EnviarMensagemAsync(
            conversa.Id, 200, 20, null, new string('A', 1000));

        Assert.Equal(1000, mensagem.Conteudo.Length);
        Assert.Equal(1, await db.Mensagens.CountAsync());
    }

    [Fact]
    public async Task Enviar_VariasMensagens_DevePersistirTodas()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        await PrepararSolicitacao(db);

        var service = new ConversaService(
            db, new FakeIdGenerator());

        var conversa = await service.CriarOuBuscarAsync(100, 20);

        var primeira = await service.EnviarMensagemAsync(
            conversa.Id, 200, 20, null, "Olá!");

        var segunda = await service.EnviarMensagemAsync(
            conversa.Id, 100, null, 10, "Oi!");

        Assert.NotEqual(primeira.Id, segunda.Id);

        db.ChangeTracker.Clear();

        var mensagens = await service.ListarMensagensAsync(
            conversa.Id, 20, null);

        Assert.Equal(2, mensagens.Count);
        Assert.Equal("Olá!", mensagens[0].Conteudo);
        Assert.Equal("Oi!", mensagens[1].Conteudo);
    }
}