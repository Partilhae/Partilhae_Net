using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Partilhae_Net.Tests.Helpers;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string TestScheme = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var perfil = Request.Headers["X-Test-Role"].ToString().ToUpperInvariant();

        if (perfil == "ANONIMO")
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        Claim[] claims;

        if (perfil == "DOADOR")
        {
            claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "200"),
                new Claim(ClaimTypes.Role, "ROLE_DOADOR"),
                new Claim("role", "ROLE_DOADOR"),
                new Claim("idUsuario", "200"),
                new Claim("idDoador", "20")
            };
        }
        else
        {
            claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "100"),
                new Claim(ClaimTypes.Role, "ROLE_RECEPTOR"),
                new Claim("role", "ROLE_RECEPTOR"),
                new Claim("idUsuario", "100"),
                new Claim("idReceptor", "10")
            };
        }

        var identity = new ClaimsIdentity(claims, TestScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, TestScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}