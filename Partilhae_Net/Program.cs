using Microsoft.EntityFrameworkCore;
using Partilhae_Net.Data;
using Partilhae_Net.Services;
using Partilhae_Net.Endpoints;
using Partilhae_Net.Security;
using System.Threading.RateLimiting;
using Microsoft.OpenApi;
using Partilhae_Net.Middlewares;
using Partilhae_Net.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Configuração do banco de dados
if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite("Data Source=:memory:"));
}
else
{
    var connectionString =
        builder.Configuration.GetConnectionString("Oracle");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Conexão Oracle não configurada.");
    }

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseOracle(connectionString));
}

// Registro dos serviços
builder.Services.AddScoped<SolicitacaoService>();
builder.Services.AddScoped<ConversaService>();
builder.Services.AddScoped<DoacaoService>();

builder.Services.AddScoped<IIdGenerator, OracleIdGenerator>();
builder.Services.AddScoped<IChatPermissionService, OracleChatPermissionService>();

// Autenticação e autorização
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddPartilhaeJwt(
        builder.Configuration["JWT_SECRET"]);
}

builder.Services.AddAuthorization();

// Configuração do OpenAPI
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "Partilhaê API";
        document.Info.Version = "v1";
        document.Info.Description =
            "API de solicitações, conversas, mensagens e doações.";

        document.Components ??= new OpenApiComponents();

        document.Components.SecuritySchemes =
            new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Informe o token JWT."
                }
            };

        foreach (var path in document.Paths)
        {
            if (path.Key == "/" || path.Key.StartsWith("/health"))
                continue;

            foreach (var operation in path.Value.Operations!)
            {
                operation.Value.Security ??= [];

                operation.Value.Security.Add(
                    new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(
                            "Bearer", document)] = []
                    });
            }
        }

        return Task.CompletedTask;
    });

    // Documentação do header de idempotência nos endpoints POST
    options.AddOperationTransformer(
        (operation, context, cancellationToken) =>
        {
            if (context.Description.HttpMethod == "POST")
            {
                operation.Parameters ??= [];

                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "Idempotency-Key",
                    In = ParameterLocation.Header,
                    Required = false,
                    Description =
                        "Chave única para evitar operações duplicadas. " +
                        "Reutilize a mesma chave ao repetir uma requisição " +
                        "com os mesmos dados. Respostas bem-sucedidas " +
                        "são armazenadas por 30 minutos.",
                    Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        MinLength = 1,
                        MaxLength = 100
                    }
                });
            }

            return Task.CompletedTask;
        });
});

// Configuração do Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter =
        PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var identificador =
                context.User.FindFirst("idUsuario")?.Value
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "anonimo";

            return RateLimitPartition.GetFixedWindowLimiter(
                identificador,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        });
});

// Cache utilizado pela idempotência
builder.Services.AddMemoryCache();

// Configuração dos Health Checks
builder.Services.AddHealthChecks()
    .AddCheck(
        "api",
        () => HealthCheckResult.Healthy("API funcionando."),
        tags: new[] { "api" })
    .AddCheck<DatabaseHealthCheck>(
        "database",
        tags: new[] { "db" });

// Construção da aplicação
var app = builder.Build();

app.UseAuthentication();

app.UseRateLimiter();

app.UseAuthorization();

app.UseMiddleware<IdempotencyMiddleware>();

// Documentação da API
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "Partilhaê API v1");

        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Partilhaê API";
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Endpoint para verificar se a API está funcionando
app.MapGet("/", () => Results.Ok(new
{
    mensagem = "API Partilhaê funcionando!",
    versao = "1.0"
}));

// Endpoints de Health Check
app.MapHealthChecks("/health");

app.MapHealthChecks("/health/api", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("api")
});

app.MapHealthChecks("/health/db", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db")
});

// Endpoints de negócio
app.MapSolicitacaoEndpoints();
app.MapConversaEndpoints();
app.MapDoacaoEndpoints();

app.Run();

public partial class Program { }