using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Partilhae_Net.Tests.Helpers;
using Xunit;

namespace Partilhae_Net.Tests.Integration;

public class JwtIntegrationTests : IClassFixture<JwtApiFactory>
{
    private readonly HttpClient _client;

    public JwtIntegrationTests(JwtApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static string GerarToken(
        string role = "ROLE_DOADOR",
        string? segredo = null,
        bool expirado = false,
        bool incluirIdPerfil = true,
        string algoritmo = SecurityAlgorithms.HmacSha384)
    {
        var chave = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                segredo ?? TestApiFactory.JwtSecretTeste));

        var claims = new List<Claim>
        {
            new("sub", "teste@partilhae.com"),
            new("idUsuario", "200", ClaimValueTypes.Integer64),
            new("role", role)
        };

        if (incluirIdPerfil)
        {
            if (role == "ROLE_DOADOR")
                claims.Add(new Claim("idDoador", "20",
                    ClaimValueTypes.Integer64));

            if (role == "ROLE_RECEPTOR")
                claims.Add(new Claim("idReceptor", "10",
                    ClaimValueTypes.Integer64));
        }

        var token = new JwtSecurityToken(
            claims: claims,
            notBefore: DateTime.UtcNow.AddHours(-2),
            expires: expirado
                ? DateTime.UtcNow.AddHours(-1)
                : DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(
                chave, algoritmo));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private void Autenticar(string token)
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task TokenValido_Doador_DeveRetornar200()
    {
        Autenticar(GerarToken());

        var resposta = await _client.GetAsync("/doacoes/doador/20");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task TokenValido_Receptor_DeveRetornar200()
    {
        Autenticar(GerarToken(role: "ROLE_RECEPTOR"));

        var resposta = await _client.GetAsync(
            "/solicitacoes/receptor/10");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task SemToken_DeveRetornar401()
    {
        var resposta = await _client.GetAsync("/solicitacoes");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task TokenExpirado_DeveRetornar401()
    {
        Autenticar(GerarToken(expirado: true));

        var resposta = await _client.GetAsync("/solicitacoes");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task AssinaturaInvalida_DeveRetornar401()
    {
        Autenticar(GerarToken(
            segredo: new string('X', 48)));

        var resposta = await _client.GetAsync("/solicitacoes");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Theory]
    [InlineData("token-invalido")]
    [InlineData("abc.def.xyz")]
    public async Task TokenMalformado_DeveRetornar401(string token)
    {
        Autenticar(token);

        var resposta = await _client.GetAsync("/solicitacoes");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task TokenSemIdDoador_DeveRetornar403()
    {
        Autenticar(GerarToken(incluirIdPerfil: false));

        var resposta = await _client.GetAsync("/doacoes/doador/20");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task ReceptorEmRotaDeDoador_DeveRetornar403()
    {
        Autenticar(GerarToken(role: "ROLE_RECEPTOR"));

        var resposta = await _client.GetAsync("/doacoes/doador/20");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task DoadorConsultandoOutroDoador_DeveRetornar403()
    {
        Autenticar(GerarToken());

        var resposta = await _client.GetAsync("/doacoes/doador/99");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task AlgoritmoIncorreto_DeveRetornar401()
    {
        Autenticar(GerarToken(
            algoritmo: SecurityAlgorithms.HmacSha256));

        var resposta = await _client.GetAsync("/solicitacoes");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task PerfilDesconhecido_DeveRetornar403()
    {
        Autenticar(GerarToken(role: "ROLE_ADMIN"));

        var resposta = await _client.GetAsync("/doacoes/doador/20");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }
}