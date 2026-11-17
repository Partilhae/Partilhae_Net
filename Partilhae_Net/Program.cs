using Microsoft.EntityFrameworkCore;
using Partilhae_Net.Data;
using Partilhae_Net.Services;
using Partilhae_Net.Endpoints;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddScoped<SolicitacaoService>();

builder.Services.AddScoped<ConversaService>();

builder.Services.AddScoped<DoacaoService>();

builder.Services.AddScoped<IIdGenerator, OracleIdGenerator>();

builder.Services.AddScoped<IChatPermissionService, OracleChatPermissionService>();

// Configuração do OpenAPI
builder.Services.AddOpenApi();

// Construção da aplicação
var app = builder.Build();

app.UseAuthentication();

app.UseAuthorization();

// Documentação da API
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
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

app.MapGet("/health/db", async (AppDbContext db, ILogger<Program> logger) =>
{
    try
    {
        var conectado = await db.Database.CanConnectAsync();

        return conectado
            ? Results.Ok(new { mensagem = "Conexão com Oracle funcionando!" })
            : Results.Problem(
                "Não foi possível conectar ao Oracle. Verifique os logs.",
                statusCode: 503);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Falha na conexão com Oracle");

        return Results.Problem(
            "Erro ao acessar o banco de dados. Verifique os logs.",
            statusCode: 503);
    }
});

app.MapSolicitacaoEndpoints();

app.MapConversaEndpoints();

app.MapDoacaoEndpoints();

app.Run();

public partial class Program { }
