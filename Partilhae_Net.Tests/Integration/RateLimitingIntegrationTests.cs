using System.Net;
using Partilhae_Net.Tests.Helpers;

namespace Partilhae_Net.Tests.Integration;

public class RateLimitingIntegrationTests
{
    [Fact]
    public async Task LimiteExcedido_DeveRetornar429()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        for (int i = 0; i < 100; i++)
        {
            using var resposta = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        }

        var respostaExcedida = await client.GetAsync("/");

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            respostaExcedida.StatusCode);
    }

    [Fact]
    public async Task UsuariosDiferentes_DevemTerLimitesIndependentes()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Test-Role", "DOADOR");

        for (int i = 0; i < 100; i++)
        {
            using var resposta = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        }

        var respostaDoador = await client.GetAsync("/");

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            respostaDoador.StatusCode);

        client.DefaultRequestHeaders.Remove("X-Test-Role");

        client.DefaultRequestHeaders.Add("X-Test-Role", "RECEPTOR");

        var respostaReceptor = await client.GetAsync("/");

        Assert.Equal(
            HttpStatusCode.OK,
            respostaReceptor.StatusCode);
    }
}