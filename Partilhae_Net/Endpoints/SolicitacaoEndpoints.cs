using System.Security.Claims;
using Partilhae_Net.DTOs;
using Partilhae_Net.Models;
using Partilhae_Net.Services;

namespace Partilhae_Net.Endpoints;

public static class SolicitacaoEndpoints
{
    public static void MapSolicitacaoEndpoints(this WebApplication app)
    {
        var grupo = app.MapGroup("/solicitacoes")
            .WithTags("Solicitações")
            .RequireAuthorization();

        grupo.MapPost("/", async (
            CriarSolicitacaoDto dto,
            ClaimsPrincipal usuario,
            SolicitacaoService service) =>
        {
            if (!usuario.IsInRole("ROLE_RECEPTOR") &&
                usuario.FindFirst("role")?.Value != "ROLE_RECEPTOR")
            {
                return Results.Forbid();
            }

            var idClaim = usuario.FindFirst("idReceptor")?.Value;

            if (!long.TryParse(idClaim, out var idReceptor) ||
                idReceptor <= 0)
            {
                return Results.Forbid();
            }

            try
            {
                var solicitacao = await service.CriarAsync(
                    idReceptor, dto);

                return Results.Created(
                    $"/solicitacoes/{solicitacao.Id}",
                    RespostaResumida(solicitacao));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(
                    new ErroResponseDto(ex.Message));
            }
        })
        .WithSummary("Criar solicitação")
        .WithDescription(
            "Cria uma nova solicitação de ajuda. " +
            "Disponível somente para receptores autenticados. " +
            "Aceita Idempotency-Key para evitar duplicações.")
        .Produces<SolicitacaoResumidaResponseDto>(
            StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status429TooManyRequests);

        grupo.MapGet("/", async (
            string? status,
            SolicitacaoService service) =>
        {
            var solicitacoes = await service.ListarAsync(status);

            return Results.Ok(
                solicitacoes.Select(Resposta).ToList());
        })
        .WithSummary("Listar solicitações")
        .WithDescription(
            "Lista as solicitações cadastradas. " +
            "Permite filtrar pelo parâmetro opcional status.")
        .Produces<List<SolicitacaoResponseDto>>(
            StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status429TooManyRequests);

        grupo.MapGet("/{id:long}", async (
            long id,
            SolicitacaoService service) =>
        {
            var s = await service.BuscarPorIdAsync(id);

            if (s is null)
                return Results.NotFound();

            return Results.Ok(Resposta(s));
        })
        .WithSummary("Buscar solicitação por ID")
        .WithDescription(
            "Retorna os detalhes de uma solicitação, " +
            "incluindo seus respectivos itens.")
        .Produces<SolicitacaoResponseDto>(
            StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status429TooManyRequests);

        grupo.MapGet("/receptor/{idReceptor:long}", async (
            long idReceptor,
            ClaimsPrincipal usuario,
            SolicitacaoService service) =>
        {
            var idClaim = usuario.FindFirst("idReceptor")?.Value;

            if (!long.TryParse(idClaim, out var idAutenticado) ||
                idAutenticado != idReceptor)
            {
                return Results.Forbid();
            }

            var solicitacoes =
                await service.ListarPorReceptorAsync(idReceptor);

            return Results.Ok(
                solicitacoes.Select(RespostaResumida).ToList());
        })
        .WithSummary("Listar solicitações do receptor")
        .WithDescription(
            "Retorna as solicitações pertencentes ao receptor autenticado. " +
            "Não permite consultar as solicitações de outro receptor.")
        .Produces<List<SolicitacaoResumidaResponseDto>>(
            StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status429TooManyRequests);

        grupo.MapPatch("/{id:long}/status", async (
            long id,
            AtualizarStatusSolicitacaoDto dto,
            ClaimsPrincipal usuario,
            SolicitacaoService service) =>
        {
            var idClaim = usuario.FindFirst("idReceptor")?.Value;

            if (usuario.FindFirst("role")?.Value != "ROLE_RECEPTOR" ||
                !long.TryParse(idClaim, out var idReceptor) ||
                idReceptor <= 0)
            {
                return Results.Forbid();
            }

            try
            {
                var solicitacao = await service.AtualizarStatusAsync(
                    id, idReceptor, dto.Status);

                if (solicitacao is null)
                    return Results.NotFound();

                return Results.Ok(
                    new StatusSolicitacaoResponseDto(
                        solicitacao.Id,
                        solicitacao.Status));
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(
                    new ErroResponseDto(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(
                    new ErroResponseDto(ex.Message));
            }
        })
        .WithSummary("Atualizar status da solicitação")
        .WithDescription(
            "Permite ao receptor responsável cancelar uma solicitação aberta, " +
            "informando o status CANCELADA.")
        .Produces<StatusSolicitacaoResponseDto>(
            StatusCodes.Status200OK)
        .Produces<ErroResponseDto>(
            StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces<ErroResponseDto>(
            StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status429TooManyRequests);
    }

    private static SolicitacaoResponseDto Resposta(
        Solicitacao s)
    {
        return new SolicitacaoResponseDto(
            s.Id,
            s.IdReceptor,
            s.Titulo,
            s.Descricao,
            s.TipoAjuda,
            s.Status,
            s.DataCriacao,
            RespostaItens(s));
    }

    private static SolicitacaoResumidaResponseDto RespostaResumida(
        Solicitacao s)
    {
        return new SolicitacaoResumidaResponseDto(
            s.Id,
            s.Titulo,
            s.Descricao,
            s.TipoAjuda,
            s.Status,
            s.DataCriacao,
            RespostaItens(s));
    }

    private static List<ItemSolicitacaoResponseDto> RespostaItens(
        Solicitacao s)
    {
        return s.Itens.Select(i =>
            new ItemSolicitacaoResponseDto(
                i.Id,
                i.Nome,
                i.Quantidade,
                i.Unidade))
            .ToList();
    }
}