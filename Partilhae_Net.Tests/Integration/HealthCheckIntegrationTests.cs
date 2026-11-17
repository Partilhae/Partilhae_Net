using System.Net;
using Partilhae_Net.Tests.Helpers;
using Xunit;

namespace Partilhae_Net.Tests.Integration;

public class HealthCheckIntegrationTests
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/api")]
    [InlineData("/health/db")]
    public async Task HealthCheck_DeveRetornarHealthy(string rota)
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var resposta = await client.GetAsync(rota);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var conteudo = await resposta.Content.ReadAsStringAsync();

        Assert.Equal("Healthy", conteudo);
    }
}