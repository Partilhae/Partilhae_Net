using System.Net;
using System.Net.Http.Json;
using Partilhae_Net.Tests.Helpers;

namespace Partilhae_Net.Tests.Endpoints;

public class DoacaoEndpointsTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public DoacaoEndpointsTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CriarDoacao_Receptor_DeveRetornar403()
    {
        var resposta = await _client.PostAsJsonAsync(
            "/doacoes",
            new { IdConversa = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task ListarDoacoes_Receptor_DeveRetornar403()
    {
        var resposta = await _client.GetAsync("/doacoes/doador/20");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task ConsultarDoacaoInexistente_DeveRetornar404()
    {
        var resposta = await _client.GetAsync(
            "/doacoes/solicitacao/999");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task AtualizarDoacaoInexistente_DeveRetornar404()
    {
        var resposta = await _client.PatchAsJsonAsync(
            "/doacoes/999/status",
            new { Status = "CONCLUIDA" });

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task AtualizarDoacao_IdInvalido_DeveRetornar400()
    {
        var resposta = await _client.PatchAsJsonAsync(
            "/doacoes/0/status",
            new { Status = "CONCLUIDA" });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task AtualizarDoacao_StatusVazio_DeveRetornar400()
    {
        var resposta = await _client.PatchAsJsonAsync(
            "/doacoes/1/status",
            new { Status = "" });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task AtualizarDoacao_StatusNulo_DeveRetornar400()
    {
        var resposta = await _client.PatchAsJsonAsync(
            "/doacoes/1/status",
            new { Status = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task ConsultarDoacao_IdSolicitacaoNegativo_DeveRetornar404()
    {
        var resposta = await _client.GetAsync(
            "/doacoes/solicitacao/-1");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
    
    [Fact]
    public async Task ListarDoacoes_DoadorCorreto_DeveRetornar200()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, "/doacoes/doador/20");

        request.Headers.Add("X-Test-Role", "DOADOR");

        var resposta = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task ListarDoacoes_OutroDoador_DeveRetornar403()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, "/doacoes/doador/21");

        request.Headers.Add("X-Test-Role", "DOADOR");

        var resposta = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task ConsultarDoacao_DoadorSemRegistro_DeveRetornar404()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, "/doacoes/solicitacao/999");

        request.Headers.Add("X-Test-Role", "DOADOR");

        var resposta = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task ConsultarDoacao_Anonimo_DeveRetornar401()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, "/doacoes/solicitacao/1");

        request.Headers.Add("X-Test-Role", "ANONIMO");

        var resposta = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task ListarDoacoes_Anonimo_DeveRetornar401()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, "/doacoes/doador/20");

        request.Headers.Add("X-Test-Role", "ANONIMO");

        var resposta = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task CriarDoacao_Anonimo_DeveRetornar401()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post, "/doacoes");

        request.Headers.Add("X-Test-Role", "ANONIMO");
        request.Content = JsonContent.Create(new { IdConversa = 1 });

        var resposta = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task AtualizarDoacao_Anonimo_DeveRetornar401()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Patch, "/doacoes/1/status");

        request.Headers.Add("X-Test-Role", "ANONIMO");
        request.Content = JsonContent.Create(new { Status = "ENTREGUE" });

        var resposta = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task CriarDoacao_DoadorConversaInexistente_DeveRetornar404()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post, "/doacoes");

        request.Headers.Add("X-Test-Role", "DOADOR");
        request.Content = JsonContent.Create(new { IdConversa = 999 });

        var resposta = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}