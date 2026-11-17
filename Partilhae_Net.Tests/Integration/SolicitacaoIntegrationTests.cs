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

public class SolicitacaoIntegrationTests
{
    private static async Task PrepararBanco(TestApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        db.Solicitacoes.AddRange(
            new Solicitacao
            {
                Id = 500,
                IdReceptor = 10,
                Titulo = "Doação de alimentos",
                Descricao = "Precisamos de ajuda",
                TipoAjuda = "ESPECIFICA",
                Status = "ABERTA",
                DataCriacao = DateTime.UtcNow
            },
            new Solicitacao
            {
                Id = 501,
                IdReceptor = 10,
                Titulo = "Doação de roupas",
                Descricao = "Precisamos de roupas",
                TipoAjuda = "ESPECIFICA",
                Status = "CANCELADA",
                DataCriacao = DateTime.UtcNow
            },
            new Solicitacao
            {
                Id = 502,
                IdReceptor = 99,
                Titulo = "Outra solicitação",
                Descricao = "Outro receptor",
                TipoAjuda = "ESPECIFICA",
                Status = "ABERTA",
                DataCriacao = DateTime.UtcNow
            });

        await db.SaveChangesAsync();
    }

    private static object CriarDados(
        string titulo = "Nova solicitação",
        string tipoAjuda = "ESPECIFICA")
    {
        return new
        {
            titulo,
            descricao = "Precisamos de alimentos",
            tipoAjuda,
            itens = Array.Empty<object>()
        };
    }

    [Fact]
    public async Task Post_Receptor_DeveCriarSolicitacao()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "RECEPTOR");

        var resposta = await client.PostAsJsonAsync(
            "/solicitacoes", CriarDados());

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var json = await resposta.Content
            .ReadFromJsonAsync<JsonElement>();

        var id = json.GetProperty("id").GetInt64();

        Assert.True(id > 0);

        Assert.Equal(
            "ABERTA",
            json.GetProperty("status").GetString());

        Assert.Equal(
            $"/solicitacoes/{id}",
            resposta.Headers.Location?.ToString());

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var solicitacao = await db.Solicitacoes.FindAsync(id);

        Assert.NotNull(solicitacao);
        Assert.Equal(10L, solicitacao.IdReceptor);
    }

    [Fact]
    public async Task Post_ComItens_DevePersistirRelacionamento()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "RECEPTOR");

        var resposta = await client.PostAsJsonAsync(
            "/solicitacoes",
            new
            {
                titulo = "Cesta básica",
                descricao = "Doação de alimentos",
                tipoAjuda = "ESPECIFICA",
                itens = new[]
                {
                    new
                    {
                        nome = "Arroz",
                        quantidade = 5,
                        unidade = "KG"
                    },
                    new
                    {
                        nome = "Feijão",
                        quantidade = 3,
                        unidade = "KG"
                    }
                }
            });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var json = await resposta.Content
            .ReadFromJsonAsync<JsonElement>();

        var id = json.GetProperty("id").GetInt64();

        Assert.Equal(
            2, json.GetProperty("itens").GetArrayLength());

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var solicitacao = await db.Solicitacoes
            .Include(s => s.Itens)
            .SingleAsync(s => s.Id == id);

        Assert.Equal(2, solicitacao.Itens.Count);

        Assert.All(solicitacao.Itens,
            item => Assert.Equal(id, item.IdSolicitacao));
    }

    [Theory]
    [InlineData("", "ESPECIFICA")]
    [InlineData("   ", "ESPECIFICA")]
    [InlineData("Alimentos", "INVALIDO")]
    public async Task Post_DadosInvalidos_DeveRetornar400(
        string titulo,
        string tipoAjuda)
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "RECEPTOR");

        var resposta = await client.PostAsJsonAsync(
            "/solicitacoes",
            CriarDados(titulo, tipoAjuda));

        Assert.Equal(
            HttpStatusCode.BadRequest, resposta.StatusCode);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        Assert.Equal(0, await db.Solicitacoes.CountAsync());
    }

    [Theory]
    [InlineData("DOADOR", HttpStatusCode.Forbidden)]
    [InlineData("ANONIMO", HttpStatusCode.Unauthorized)]
    public async Task Post_SemPermissao_DeveRetornarErro(
        string role,
        HttpStatusCode esperado)
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Test-Role", role);

        var resposta = await client.PostAsJsonAsync(
            "/solicitacoes", CriarDados());

        Assert.Equal(esperado, resposta.StatusCode);
    }

    [Fact]
    public async Task Get_DeveListarSolicitacoes()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        await PrepararBanco(factory);

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "DOADOR");

        var resposta = await client.GetAsync("/solicitacoes");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var json = await resposta.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(3, json.GetArrayLength());
    }

    [Theory]
    [InlineData("ABERTA", 2)]
    [InlineData("cancelada", 1)]
    [InlineData("CONCLUIDA", 0)]
    public async Task Get_DeveFiltrarPorStatus(
        string status,
        int quantidade)
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        await PrepararBanco(factory);

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "DOADOR");

        var resposta = await client.GetAsync(
            $"/solicitacoes?status={status}");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var json = await resposta.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(quantidade, json.GetArrayLength());

        foreach (var item in json.EnumerateArray())
        {
            Assert.Equal(
                status.ToUpperInvariant(),
                item.GetProperty("status").GetString());
        }
    }

    [Fact]
    public async Task Get_IdExistente_DeveRetornarSolicitacao()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        await PrepararBanco(factory);

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "DOADOR");

        var resposta = await client.GetAsync(
            "/solicitacoes/500");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var json = await resposta.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(500L, json.GetProperty("id").GetInt64());

        Assert.Equal(
            "Doação de alimentos",
            json.GetProperty("titulo").GetString());
    }

    [Fact]
    public async Task Get_IdInexistente_DeveRetornar404()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        await PrepararBanco(factory);

        var resposta = await client.GetAsync(
            "/solicitacoes/999");

        Assert.Equal(
            HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Get_Receptor_DeveListarApenasProprias()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        await PrepararBanco(factory);

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "RECEPTOR");

        var resposta = await client.GetAsync(
            "/solicitacoes/receptor/10");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var json = await resposta.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(2, json.GetArrayLength());

        foreach (var item in json.EnumerateArray())
        {
            Assert.NotEqual(
                502L, item.GetProperty("id").GetInt64());
        }
    }

    [Fact]
    public async Task Get_OutroReceptor_DeveRetornar403()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        await PrepararBanco(factory);

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "RECEPTOR");

        var resposta = await client.GetAsync(
            "/solicitacoes/receptor/99");

        Assert.Equal(
            HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Patch_Cancelamento_DevePersistirStatus()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        await PrepararBanco(factory);

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "RECEPTOR");

        var resposta = await client.PatchAsJsonAsync(
            "/solicitacoes/500/status",
            new { status = "  cancelada  " });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var json = await resposta.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            "CANCELADA",
            json.GetProperty("status").GetString());

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var solicitacao = await db.Solicitacoes.FindAsync(500L);

        Assert.NotNull(solicitacao);
        Assert.Equal("CANCELADA", solicitacao.Status);
    }

    [Theory]
    [InlineData(502, "CANCELADA", HttpStatusCode.Forbidden)]
    [InlineData(501, "CANCELADA", HttpStatusCode.Conflict)]
    [InlineData(500, "CONCLUIDA", HttpStatusCode.BadRequest)]
    [InlineData(999, "CANCELADA", HttpStatusCode.NotFound)]
    public async Task Patch_OperacaoInvalida_DeveRetornarErro(
        long id,
        string status,
        HttpStatusCode esperado)
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        await PrepararBanco(factory);

        client.DefaultRequestHeaders.Add(
            "X-Test-Role", "RECEPTOR");

        var resposta = await client.PatchAsJsonAsync(
            $"/solicitacoes/{id}/status",
            new { status });

        Assert.Equal(esperado, resposta.StatusCode);
    }
}