using System.Net;
using System.Net.Http.Json;
using Partilhae_Net.Tests.Helpers;
using Xunit;

namespace Partilhae_Net.Tests;

public class ApiIntegrationTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRaiz_DeveRetornar200()
    {
        var resposta = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task GetRaiz_DeveRetornarMensagemDaApi()
    {
        var resposta = await _client.GetFromJsonAsync<RespostaApi>("/");

        Assert.NotNull(resposta);
        Assert.Equal(
            "API Partilhaê funcionando!",
            resposta.Mensagem);
        Assert.Equal("1.0", resposta.Versao);
    }

    [Fact]
    public async Task GetHealthDb_DeveRetornar200()
    {
        var resposta = await _client.GetAsync("/health/db");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task GetSolicitacoes_DeveRetornar200()
    {
        var resposta = await _client.GetAsync("/solicitacoes");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task GetSolicitacaoInexistente_DeveRetornar404()
    {
        var resposta = await _client.GetAsync("/solicitacoes/999");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    private sealed class RespostaApi
    {
        public string Mensagem { get; set; } = "";
        public string Versao { get; set; } = "";
    }
}