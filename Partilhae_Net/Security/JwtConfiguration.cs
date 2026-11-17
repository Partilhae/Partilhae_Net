using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Partilhae_Net.Security;

public static class JwtConfiguration
{
    public static IServiceCollection AddPartilhaeJwt(
        this IServiceCollection services,
        string? jwtSecret)
    {
        if (string.IsNullOrWhiteSpace(jwtSecret))
        {
            throw new InvalidOperationException(
                "JWT_SECRET não configurado.");
        }

        var keyBytes = Encoding.UTF8.GetBytes(jwtSecret);

        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT_SECRET possui tamanho insuficiente.");
        }

        var algoritmo = keyBytes.Length >= 64
            ? SecurityAlgorithms.HmacSha512
            : keyBytes.Length >= 48
                ? SecurityAlgorithms.HmacSha384
                : SecurityAlgorithms.HmacSha256;

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey =
                            new SymmetricSecurityKey(keyBytes),

                        ValidateIssuer = false,
                        ValidateAudience = false,

                        ValidateLifetime = true,
                        RequireExpirationTime = true,
                        RequireSignedTokens = true,

                        ValidAlgorithms = new[] { algoritmo },

                        RoleClaimType = "role",
                        NameClaimType = "sub",

                        ClockSkew = TimeSpan.Zero
                    };
            });

        return services;
    }
}