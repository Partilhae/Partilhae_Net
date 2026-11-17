using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Partilhae_Net.Data;
using Partilhae_Net.Models;
using Partilhae_Net.Services;
using Partilhae_Net.Tests.Helpers;
using Xunit;

namespace Partilhae_Net.Tests.Integration;

public class DoacaoIntegrationTests
{
    private static async Task PrepararBanco(
        TestApiFactory factory,
        string status = "ABERTA",
        long idDoador = 20)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

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
            IdDoador = idDoador,
            IdReceptor = 10,
            DataCriacao = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    private static async Task PrepararDoacao(
        TestApiFactory factory,
        long idDoador = 20)
    {
        await PrepararBanco(
            factory, "EM_ANDAMENTO", idDoador);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        db.Doacoes.Add(new Doacao
        {
            Id = 700,
            IdConversa = 600,
            IdSolicitacao = 500,
            IdDoador = idDoador,
            Status = "EM_ANDAMENTO",
            DataCriacao = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Post_DeveCriarDoacao()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "DOADOR");

        await PrepararBanco(factory);

        var response = await client.PostAsJsonAsync(
            "/doacoes",
            new { idConversa = 600 });

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var json = await response.Content
            .ReadFromJsonAsync<JsonElement>();

        var id = json.GetProperty("id").GetInt64();

        Assert.True(id > 0);

        Assert.Equal(
            600L,
            json.GetProperty("idConversa").GetInt64());

        Assert.Equal(
            500L,
            json.GetProperty("idSolicitacao").GetInt64());

        Assert.Equal(
            20L,
            json.GetProperty("idDoador").GetInt64());

        Assert.Equal(
            "EM_ANDAMENTO",
            json.GetProperty("status").GetString());

        Assert.Equal(
            $"/doacoes/{id}",
            response.Headers.Location?.ToString());

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var doacao = await db.Doacoes.SingleAsync();

        Assert.Equal(id, doacao.Id);
        Assert.Equal("EM_ANDAMENTO", doacao.Status);

        var solicitacao = await db.Solicitacoes
            .SingleAsync(s => s.Id == 500);

        Assert.Equal("EM_ANDAMENTO", solicitacao.Status);
    }

    [Theory]
    [InlineData("RECEPTOR", 403)]
    [InlineData("ANONIMO", 401)]
    public async Task Post_DeveRejeitarUsuarioNaoAutorizado(
        string role,
        int statusEsperado)
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", role);

        await PrepararBanco(factory);

        var response = await client.PostAsJsonAsync(
            "/doacoes",
            new { idConversa = 600 });

        Assert.Equal(
            statusEsperado,
            (int)response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Post_DeveRejeitarIdInvalido(
        long idConversa)
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "DOADOR");

        var response = await client.PostAsJsonAsync(
            "/doacoes",
            new { idConversa });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Post_DeveRetornar404ParaConversaInexistente()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "DOADOR");

        var response = await client.PostAsJsonAsync(
            "/doacoes",
            new { idConversa = 999 });

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Post_DeveRejeitarConversaDeOutroDoador()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "DOADOR");

        await PrepararBanco(factory, "ABERTA", 99);

        var response = await client.PostAsJsonAsync(
            "/doacoes",
            new { idConversa = 600 });

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Post_DeveImpedirDoacaoDuplicada()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "DOADOR");

        await PrepararBanco(factory);

        var primeira = await client.PostAsJsonAsync(
            "/doacoes",
            new { idConversa = 600 });

        var segunda = await client.PostAsJsonAsync(
            "/doacoes",
            new { idConversa = 600 });

        Assert.Equal(
            HttpStatusCode.Created,
            primeira.StatusCode);

        Assert.Equal(
            HttpStatusCode.Conflict,
            segunda.StatusCode);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        Assert.Single(await db.Doacoes.ToListAsync());
    }

    [Theory]
    [InlineData("CANCELADA")]
    [InlineData("CONCLUIDA")]
    [InlineData("EM_ANDAMENTO")]
    public async Task Post_DeveRejeitarSolicitacaoIndisponivel(
        string status)
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "DOADOR");

        await PrepararBanco(factory, status);

        var response = await client.PostAsJsonAsync(
            "/doacoes",
            new { idConversa = 600 });

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task Post_DeveRejeitarChatNaoLiberado()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "DOADOR");

        await PrepararBanco(factory);

        using var scope = factory.Services.CreateScope();

        var permission = (FakeChatPermissionService)
            scope.ServiceProvider
                .GetRequiredService<IChatPermissionService>();

        permission.ChatLiberado = false;

        var response = await client.PostAsJsonAsync(
            "/doacoes",
            new { idConversa = 600 });

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        Assert.Empty(await db.Doacoes.ToListAsync());

        var solicitacao = await db.Solicitacoes
            .SingleAsync(s => s.Id == 500);

        Assert.Equal("ABERTA", solicitacao.Status);
    }

    [Fact]
    public async Task Get_DeveListarDoacoesDoDoador()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "DOADOR");

        await PrepararDoacao(factory);

        var response = await client.GetAsync(
            "/doacoes/doador/20");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var json = await response.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(1, json.GetArrayLength());

        Assert.Equal(
            700L,
            json[0].GetProperty("id").GetInt64());
    }

    [Fact]
    public async Task Get_DeveRetornarListaVazia()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "DOADOR");

        await PrepararDoacao(factory, 99);

        var response = await client.GetAsync(
            "/doacoes/doador/20");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var json = await response.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(0, json.GetArrayLength());
    }

    [Theory]
    [InlineData("DOADOR", 99)]
    [InlineData("RECEPTOR", 20)]
    public async Task Get_DeveRejeitarListagemNaoAutorizada(
        string role,
        long idDoador)
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", role);

        var response = await client.GetAsync(
            $"/doacoes/doador/{idDoador}");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Theory]
    [InlineData("DOADOR")]
    [InlineData("RECEPTOR")]
    public async Task Get_DeveConsultarDoacaoPelaSolicitacao(
        string role)
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", role);

        await PrepararDoacao(factory);

        var response = await client.GetAsync(
            "/doacoes/solicitacao/500");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var json = await response.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            700L,
            json.GetProperty("id").GetInt64());

        Assert.Equal(
            "EM_ANDAMENTO",
            json.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Get_DeveRetornar404SemDoacao()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        await PrepararBanco(factory);

        var response = await client.GetAsync(
            "/doacoes/solicitacao/500");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Get_DeveOcultarDoacaoDeOutroDoador()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "DOADOR");

        await PrepararDoacao(factory, 99);

        var response = await client.GetAsync(
            "/doacoes/solicitacao/500");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
}