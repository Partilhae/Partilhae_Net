using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Partilhae_Net.Data;
using Partilhae_Net.DTOs;
using Partilhae_Net.Services;

namespace Partilhae_Net.Endpoints;

public static class DoacaoEndpoints
{
    public static void MapDoacaoEndpoints(this WebApplication app)
    {
        var grupo = app.MapGroup("/doacoes")
            .WithTags("Doações")
            .RequireAuthorization();

        // Registrar doação
        grupo.MapPost("/", async (
            CriarDoacaoDto dto,
            ClaimsPrincipal usuario,
            DoacaoService service) =>
        {
            var role = usuario.FindFirst("role")?.Value;

            if (role != "ROLE_DOADOR" ||
                !long.TryParse(
                    usuario.FindFirst("idDoador")?.Value,
                    out var idDoador) ||
                idDoador <= 0)
                return Results.Forbid();

            try
            {
                var doacao = await service.CriarAsync(
                    dto.IdConversa, idDoador);

                return Results.Created(
                    $"/doacoes/{doacao.Id}",
                    Resposta(doacao));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { erro = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { erro = ex.Message });
            }
        });

        // Listar doações do doador
        grupo.MapGet("/doador/{idDoador:long}", async (
            long idDoador,
            ClaimsPrincipal usuario,
            AppDbContext db) =>
        {
            if (usuario.FindFirst("role")?.Value != "ROLE_DOADOR" ||
                !long.TryParse(
                    usuario.FindFirst("idDoador")?.Value,
                    out var idAutenticado) ||
                idAutenticado != idDoador)
                return Results.Forbid();

            var doacoes = await db.Doacoes
                .AsNoTracking()
                .Where(d => d.IdDoador == idDoador)
                .OrderByDescending(d => d.DataCriacao)
                .ToListAsync();

            return Results.Ok(doacoes.Select(Resposta));
        });

        // Consultar doação pela solicitação
        grupo.MapGet("/solicitacao/{idSolicitacao:long}", async (
            long idSolicitacao,
            ClaimsPrincipal usuario,
            AppDbContext db) =>
        {
            var role = usuario.FindFirst("role")?.Value;

            long? idDoador = role == "ROLE_DOADOR" &&
                long.TryParse(
                    usuario.FindFirst("idDoador")?.Value, out var d)
                ? d : null;

            long? idReceptor = role == "ROLE_RECEPTOR" &&
                long.TryParse(
                    usuario.FindFirst("idReceptor")?.Value, out var r)
                ? r : null;

            if (idDoador is null && idReceptor is null)
                return Results.Forbid();

            var doacao = await db.Doacoes
                .AsNoTracking()
                .Where(d => d.IdSolicitacao == idSolicitacao)
                .Where(d =>
                    (idDoador.HasValue &&
                     d.IdDoador == idDoador.Value) ||
                    (idReceptor.HasValue &&
                     d.Conversa.IdReceptor == idReceptor.Value))
                .OrderByDescending(d => d.DataCriacao)
                .FirstOrDefaultAsync();

            if (doacao is null)
                return Results.NotFound(
                    new { erro = "Doação não encontrada." });

            return Results.Ok(Resposta(doacao));
        });

        // Atualizar status da doação
        grupo.MapPatch("/{id:long}/status", async (
            long id,
            AtualizarStatusDoacaoDto dto,
            ClaimsPrincipal usuario,
            DoacaoService service) =>
        {
            var role = usuario.FindFirst("role")?.Value;

            long? idDoador = role == "ROLE_DOADOR" &&
                long.TryParse(
                    usuario.FindFirst("idDoador")?.Value, out var d)
                ? d : null;

            long? idReceptor = role == "ROLE_RECEPTOR" &&
                long.TryParse(
                    usuario.FindFirst("idReceptor")?.Value, out var r)
                ? r : null;

            if (idDoador is null && idReceptor is null)
                return Results.Forbid();

            try
            {
                var doacao = await service.AtualizarStatusAsync(
                    id, idDoador, idReceptor, dto.Status);

                return Results.Ok(Resposta(doacao));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { erro = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { erro = ex.Message });
            }
        });
    }

    private static object Resposta(Partilhae_Net.Models.Doacao d)
    {
        return new
        {
            d.Id,
            d.IdConversa,
            d.IdSolicitacao,
            d.IdDoador,
            d.Status,
            d.DataCriacao,
            d.DataEntregueDoador,
            d.DataRecebidoReceptor,
            d.DataEncerramentoChat
        };
    }
}
