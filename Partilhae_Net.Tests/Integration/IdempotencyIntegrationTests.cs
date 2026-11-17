using System.Net;
using System.Net.Http.Json;
using Partilhae_Net.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Partilhae_Net.Data;

namespace Partilhae_Net.Tests.Integration;

public class IdempotencyIntegrationTests
{
    private static object CriarDados(
        string titulo = "Solicitação de alimentos")
    {
        return new
        {
            titulo,
            descricao = "Precisamos de alimentos",
            tipoAjuda = "ESPECIFICA",
            itens = new[]
            {
                new
                {
                    nome = "Arroz",
                    quantidade = 2,
                    unidade = "kg"
                }
            }
        };
    }

    [Fact]
    public async Task MesmaChave_DeveRetornarRespostaOriginal()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "Idempotency-Key", Guid.NewGuid().ToString());

        var primeira = await client.PostAsJsonAsync(
            "/solicitacoes", CriarDados());

        var segunda = await client.PostAsJsonAsync(
            "/solicitacoes", CriarDados());

        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);

        Assert.Equal(
            await primeira.Content.ReadAsStringAsync(),
            await segunda.Content.ReadAsStringAsync());

        Assert.Equal(
            "true",
            segunda.Headers.GetValues("Idempotency-Replayed").Single());
        
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var quantidade = await db.Solicitacoes
            .CountAsync(s => s.Titulo == "Solicitação de alimentos");

        Assert.Equal(1, quantidade);
    }

    [Fact]
    public async Task MesmaChaveComDadosDiferentes_DeveRetornar409()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "Idempotency-Key", Guid.NewGuid().ToString());

        var primeira = await client.PostAsJsonAsync(
            "/solicitacoes", CriarDados());

        var segunda = await client.PostAsJsonAsync(
            "/solicitacoes", CriarDados("Outra solicitação"));

        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
    }

    [Fact]
    public async Task ChavesDiferentes_DevemPermitirNovasOperacoes()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "Idempotency-Key", Guid.NewGuid().ToString());

        var primeira = await client.PostAsJsonAsync(
            "/solicitacoes", CriarDados());

        client.DefaultRequestHeaders.Remove("Idempotency-Key");

        client.DefaultRequestHeaders.Add(
            "Idempotency-Key", Guid.NewGuid().ToString());

        var segunda = await client.PostAsJsonAsync(
            "/solicitacoes", CriarDados());

        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);

        Assert.NotEqual(
            await primeira.Content.ReadAsStringAsync(),
            await segunda.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ChaveInvalida_DeveRetornar400()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "Idempotency-Key", new string('a', 101));

        var resposta = await client.PostAsJsonAsync(
            "/solicitacoes", CriarDados());

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }
}