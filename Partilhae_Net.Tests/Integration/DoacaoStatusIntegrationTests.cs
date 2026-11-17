using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Partilhae_Net.Data;
using Partilhae_Net.Models;
using Partilhae_Net.Tests.Helpers;

namespace Partilhae_Net.Tests.Integration;

public class DoacaoStatusIntegrationTests
{
    private static async Task<(TestApiFactory Factory, HttpClient Client)>
        CriarAmbienteAsync(string status = "EM_ANDAMENTO")
    {
        var factory = new TestApiFactory();
        var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var solicitacao = new Solicitacao
        {
            Id = 500,
            IdReceptor = 10,
            Titulo = "Doação de alimentos",
            Descricao = "Solicitação para testes",
            TipoAjuda = "ALIMENTO",
            Status = status == "CONCLUIDA" ? "CONCLUIDA" : "EM_ANDAMENTO",
            DataCriacao = DateTime.UtcNow
        };

        var conversa = new Conversa
        {
            Id = 600,
            IdSolicitacao = 500,
            IdDoador = 20,
            IdReceptor = 10,
            DataCriacao = DateTime.UtcNow
        };

        var doacao = new Doacao
        {
            Id = 700,
            IdConversa = 600,
            IdSolicitacao = 500,
            IdDoador = 20,
            Status = status,
            DataCriacao = DateTime.UtcNow
        };

        db.Solicitacoes.Add(solicitacao);
        db.Conversas.Add(conversa);
        db.Doacoes.Add(doacao);

        await db.SaveChangesAsync();

        return (factory, client);
    }

    private static async Task<HttpResponseMessage> AlterarStatusAsync(
        HttpClient client,
        string perfil,
        string status)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            "/doacoes/700/status");

        request.Headers.Add("X-Test-Role", perfil);
        request.Content = JsonContent.Create(new { Status = status });

        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Doador_EntregaValida_DeveRetornar200()
    {
        var (factory, client) = await CriarAmbienteAsync();
        using (factory)
        using (client)
        {
            var resposta = await AlterarStatusAsync(
                client, "DOADOR", "ENTREGUE");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        }
    }

    [Fact]
    public async Task Doador_EntregaValida_DevePersistirStatus()
    {
        var (factory, client) = await CriarAmbienteAsync();
        using (factory)
        using (client)
        {
            var resposta = await AlterarStatusAsync(
                client, "DOADOR", "ENTREGUE");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var doacao = await db.Doacoes.AsNoTracking()
                .SingleAsync(d => d.Id == 700);

            Assert.Equal("ENTREGUE", doacao.Status);
            Assert.NotNull(doacao.DataEntregueDoador);
        }
    }

    [Fact]
    public async Task Receptor_ConcluirEntregaValida_DeveRetornar200()
    {
        var (factory, client) = await CriarAmbienteAsync("ENTREGUE");
        using (factory)
        using (client)
        {
            var resposta = await AlterarStatusAsync(
                client, "RECEPTOR", "CONCLUIDA");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        }
    }

    [Fact]
    public async Task Receptor_Concluir_DeveAtualizarDoacaoESolicitacao()
    {
        var (factory, client) = await CriarAmbienteAsync("ENTREGUE");
        using (factory)
        using (client)
        {
            var resposta = await AlterarStatusAsync(
                client, "RECEPTOR", "CONCLUIDA");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var doacao = await db.Doacoes.AsNoTracking()
                .SingleAsync(d => d.Id == 700);

            var solicitacao = await db.Solicitacoes.AsNoTracking()
                .SingleAsync(s => s.Id == 500);

            Assert.Equal("CONCLUIDA", doacao.Status);
            Assert.Equal("CONCLUIDA", solicitacao.Status);
            Assert.NotNull(doacao.DataRecebidoReceptor);
            Assert.NotNull(doacao.DataEncerramentoChat);
        }
    }

    [Fact]
    public async Task Receptor_NaoPodeMarcarEntrega()
    {
        var (factory, client) = await CriarAmbienteAsync();
        using (factory)
        using (client)
        {
            var resposta = await AlterarStatusAsync(
                client, "RECEPTOR", "ENTREGUE");

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }
    }

    [Fact]
    public async Task Doador_NaoPodeConcluirRecebimento()
    {
        var (factory, client) = await CriarAmbienteAsync("ENTREGUE");
        using (factory)
        using (client)
        {
            var resposta = await AlterarStatusAsync(
                client, "DOADOR", "CONCLUIDA");

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }
    }

    [Fact]
    public async Task Doador_NaoPodeEntregarDuasVezes()
    {
        var (factory, client) = await CriarAmbienteAsync("ENTREGUE");
        using (factory)
        using (client)
        {
            var resposta = await AlterarStatusAsync(
                client, "DOADOR", "ENTREGUE");

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        }
    }

    [Fact]
    public async Task Receptor_NaoPodeConcluirAntesDaEntrega()
    {
        var (factory, client) = await CriarAmbienteAsync();
        using (factory)
        using (client)
        {
            var resposta = await AlterarStatusAsync(
                client, "RECEPTOR", "CONCLUIDA");

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        }
    }

    [Fact]
    public async Task StatusInvalido_DeveRetornar400()
    {
        var (factory, client) = await CriarAmbienteAsync();
        using (factory)
        using (client)
        {
            var resposta = await AlterarStatusAsync(
                client, "DOADOR", "CANCELADA");

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        }
    }

    [Fact]
    public async Task StatusMinusculo_DeveSerNormalizado()
    {
        var (factory, client) = await CriarAmbienteAsync();
        using (factory)
        using (client)
        {
            var resposta = await AlterarStatusAsync(
                client, "DOADOR", " entregue ");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var doacao = await db.Doacoes.AsNoTracking()
                .SingleAsync(d => d.Id == 700);

            Assert.Equal("ENTREGUE", doacao.Status);
        }
    }

    [Fact]
    public async Task Doador_ConsultaDoacaoExistente_DeveRetornar200()
    {
        var (factory, client) = await CriarAmbienteAsync();
        using (factory)
        using (client)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/doacoes/solicitacao/500");

            request.Headers.Add("X-Test-Role", "DOADOR");

            var resposta = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        }
    }

    [Fact]
    public async Task Receptor_ConsultaDoacaoExistente_DeveRetornar200()
    {
        var (factory, client) = await CriarAmbienteAsync();
        using (factory)
        using (client)
        {
            var resposta = await client.GetAsync(
                "/doacoes/solicitacao/500");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        }
    }
}
