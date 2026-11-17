using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Partilhae_Net.Data;
using Partilhae_Net.Models;
using Partilhae_Net.Tests.Helpers;
using Xunit;

namespace Partilhae_Net.Tests.Integration;

public class ConversaIntegrationTests
{
    private static async Task PrepararBanco(
        TestApiFactory factory,
        string status = "ABERTA")
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        db.Solicitacoes.Add(new Solicitacao
        {
            Id = 500,
            IdReceptor = 10,
            Titulo = "Doação de alimentos",
            Descricao = "Precisamos de ajuda",
            TipoAjuda = "ESPECIFICA",
            Status = status,
            DataCriacao = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        db.Conversas.Add(new Conversa
        {
            Id = 600,
            IdSolicitacao = 500,
            IdDoador = 30,
            IdReceptor = 10,
            DataCriacao = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Post_Doador_DeveCriarConversa()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        await PrepararBanco(factory);

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "DOADOR");

        var resposta = await client.PostAsJsonAsync(
            "/conversas",
            new { idSolicitacao = 500 });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var json = await resposta.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(20L, json.GetProperty("idDoador").GetInt64());
        Assert.Equal(10L, json.GetProperty("idReceptor").GetInt64());
        Assert.Equal(500L, json.GetProperty("idSolicitacao").GetInt64());

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        Assert.True(await db.Conversas.AnyAsync(
            c => c.IdSolicitacao == 500 && c.IdDoador == 20));
    }

    [Fact]
    public async Task Post_ConversaRepetida_NaoDeveDuplicar()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        await PrepararBanco(factory);

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "DOADOR");

        var primeira = await client.PostAsJsonAsync(
            "/conversas", new { idSolicitacao = 500 });

        var segunda = await client.PostAsJsonAsync(
            "/conversas", new { idSolicitacao = 500 });

        Assert.Equal(HttpStatusCode.OK, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);

        var json1 = await primeira.Content
            .ReadFromJsonAsync<JsonElement>();

        var json2 = await segunda.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            json1.GetProperty("id").GetInt64(),
            json2.GetProperty("id").GetInt64());

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        Assert.Equal(1, await db.Conversas.CountAsync(
            c => c.IdSolicitacao == 500 && c.IdDoador == 20));
    }

    [Theory]
    [InlineData("RECEPTOR", 500, HttpStatusCode.Forbidden)]
    [InlineData("DOADOR", 0, HttpStatusCode.BadRequest)]
    [InlineData("DOADOR", 999, HttpStatusCode.NotFound)]
    [InlineData("ANONIMO", 500, HttpStatusCode.Unauthorized)]
    public async Task Post_ConversaInvalida_DeveRetornarErro(
        string role,
        long idSolicitacao,
        HttpStatusCode esperado)
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        await PrepararBanco(factory);

        client.DefaultRequestHeaders.Add("X-Test-Role", role);

        var resposta = await client.PostAsJsonAsync(
            "/conversas", new { idSolicitacao });

        Assert.Equal(esperado, resposta.StatusCode);
    }

    [Fact]
    public async Task Post_SolicitacaoCancelada_DeveRetornar409()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        await PrepararBanco(factory, "CANCELADA");

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "DOADOR");

        var resposta = await client.PostAsJsonAsync(
            "/conversas", new { idSolicitacao = 500 });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Get_ConversasDoUsuario_DeveRetornar200()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        await PrepararBanco(factory);

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "RECEPTOR");

        var resposta = await client.GetAsync(
            "/conversas/usuario/100");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var json = await resposta.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(1, json.GetArrayLength());

        Assert.Equal(
            600L, json[0].GetProperty("id").GetInt64());
    }

    [Fact]
    public async Task Get_ConversasDeOutroUsuario_DeveRetornar403()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        await PrepararBanco(factory);

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "RECEPTOR");

        var resposta = await client.GetAsync(
            "/conversas/usuario/999");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Post_MensagemValida_DeveRetornar201()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        await PrepararBanco(factory);

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "RECEPTOR");

        var resposta = await client.PostAsJsonAsync(
            "/conversas/600/mensagens",
            new { conteudo = "Muito obrigada!" });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var json = await resposta.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            "Muito obrigada!",
            json.GetProperty("conteudo").GetString());

        Assert.Equal(
            100L,
            json.GetProperty("idRemetenteUsuario").GetInt64());

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        Assert.Equal(1, await db.Mensagens.CountAsync());
    }

    [Theory]
    [InlineData("RECEPTOR", 600, "", HttpStatusCode.BadRequest)]
    [InlineData("RECEPTOR", 999, "Olá", HttpStatusCode.NotFound)]
    [InlineData("DOADOR", 600, "Olá", HttpStatusCode.Forbidden)]
    public async Task Post_MensagemInvalida_DeveRetornarErro(
        string role,
        long idConversa,
        string conteudo,
        HttpStatusCode esperado)
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        await PrepararBanco(factory);

        client.DefaultRequestHeaders.Add("X-Test-Role", role);

        var resposta = await client.PostAsJsonAsync(
            $"/conversas/{idConversa}/mensagens",
            new { conteudo });

        Assert.Equal(esperado, resposta.StatusCode);
    }

    [Fact]
    public async Task Get_MensagensDeveRetornarConteudoEnviado()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        await PrepararBanco(factory);

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "RECEPTOR");

        var primeira = await client.PostAsJsonAsync(
            "/conversas/600/mensagens",
            new { conteudo = "Olá!" });

        var segunda = await client.PostAsJsonAsync(
            "/conversas/600/mensagens",
            new { conteudo = "Tudo bem?" });

        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);

        var resposta = await client.GetAsync(
            "/conversas/600/mensagens");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var json = await resposta.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(2, json.GetArrayLength());

        Assert.Equal(
            "Olá!", json[0].GetProperty("conteudo").GetString());

        Assert.Equal(
            "Tudo bem?", json[1].GetProperty("conteudo").GetString());
    }

    [Theory]
    [InlineData("DOADOR", 600, HttpStatusCode.Forbidden)]
    [InlineData("RECEPTOR", 999, HttpStatusCode.NotFound)]
    public async Task Get_MensagensSemAcesso_DeveRetornarErro(
        string role,
        long idConversa,
        HttpStatusCode esperado)
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        await PrepararBanco(factory);

        client.DefaultRequestHeaders.Add("X-Test-Role", role);

        var resposta = await client.GetAsync(
            $"/conversas/{idConversa}/mensagens");

        Assert.Equal(esperado, resposta.StatusCode);
    }
}