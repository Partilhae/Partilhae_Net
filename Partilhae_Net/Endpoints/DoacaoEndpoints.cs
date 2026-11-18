using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Partilhae_Net.Data;
using Partilhae_Net.DTOs;
using Partilhae_Net.Models;
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
                return Results.BadRequest(
                    new ErroResponseDto(ex.Message));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(
                    new ErroResponseDto(ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(
                    new ErroResponseDto(ex.Message));
            }
        })
        .WithSummary("Registrar doação")
        .WithDescription(
            "Permite que um doador autenticado registre uma doação " +
            "a partir de uma conversa existente. " +
            "A solicitação deve estar aberta e não pode haver " +
            "outra doação vinculada à mesma conversa. " +
            "Aceita Idempotency-Key para evitar duplicações.")
        .Produces<DoacaoResponseDto>(
            StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces<ErroResponseDto>(
            StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status429TooManyRequests);

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

            return Results.Ok(
                doacoes.Select(Resposta).ToList());
        })
        .WithSummary("Listar doações do doador")
        .WithDescription(
            "Retorna as doações do doador autenticado, " +
            "ordenadas da mais recente para a mais antiga. " +
            "Não permite consultar doações de outro doador.")
        .Produces<List<DoacaoResponseDto>>(
            StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status429TooManyRequests);

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
                    new ErroResponseDto("Doação não encontrada."));

            return Results.Ok(Resposta(doacao));
        })
        .WithSummary("Consultar doação pela solicitação")
        .WithDescription(
            "Retorna a doação mais recente vinculada à solicitação " +
            "informada e pertencente ao usuário autenticado. " +
            "Disponível para doadores e receptores envolvidos.")
        .Produces<DoacaoResponseDto>(
            StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces<ErroResponseDto>(
            StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status429TooManyRequests);

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
                return Results.BadRequest(
                    new ErroResponseDto(ex.Message));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(
                    new ErroResponseDto(ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(
                    new ErroResponseDto(ex.Message));
            }
        })
        .WithSummary("Atualizar status da doação")
        .WithDescription(
            "Permite ao doador confirmar a entrega com o status ENTREGUE " +
            "e ao receptor confirmar o recebimento com o status CONCLUIDA. " +
            "Respeita as permissões dos participantes e a sequência " +
            "de atualização dos status.")
        .Produces<DoacaoResponseDto>(
            StatusCodes.Status200OK)
        .Produces<ErroResponseDto>(
            StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces<ErroResponseDto>(
            StatusCodes.Status404NotFound)
        .Produces<ErroResponseDto>(
            StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status429TooManyRequests);
    }

    private static DoacaoResponseDto Resposta(Doacao d)
    {
        return new DoacaoResponseDto(
            d.Id,
            d.IdConversa,
            d.IdSolicitacao,
            d.IdDoador,
            d.Status,
            d.DataCriacao,
            d.DataEntregueDoador,
            d.DataRecebidoReceptor,
            d.DataEncerramentoChat);
    }
}