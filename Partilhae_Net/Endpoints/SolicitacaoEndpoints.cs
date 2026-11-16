using System.Security.Claims;
using Partilhae_Net.DTOs;
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
                    new
                    {
                        solicitacao.Id,
                        solicitacao.Titulo,
                        solicitacao.Descricao,
                        solicitacao.TipoAjuda,
                        solicitacao.Status,
                        solicitacao.DataCriacao,
                        solicitacao.Itens
                    });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new
                {
                    erro = ex.Message
                });
            }
        });
        
        grupo.MapGet("/", async (
            string? status,
            SolicitacaoService service) =>
        {
            var solicitacoes = await service.ListarAsync(status);

            return Results.Ok(solicitacoes.Select(s => new
            {
                s.Id,
                s.IdReceptor,
                s.Titulo,
                s.Descricao,
                s.TipoAjuda,
                s.Status,
                s.DataCriacao,
                Itens = s.Itens.Select(i => new
                {
                    i.Id,
                    i.Nome,
                    i.Quantidade,
                    i.Unidade
                })
            }));
        });

        grupo.MapGet("/{id:long}", async (
            long id,
            SolicitacaoService service) =>
        {
            var s = await service.BuscarPorIdAsync(id);

            if (s is null)
                return Results.NotFound();

            return Results.Ok(new
            {
                s.Id,
                s.IdReceptor,
                s.Titulo,
                s.Descricao,
                s.TipoAjuda,
                s.Status,
                s.DataCriacao,
                Itens = s.Itens.Select(i => new
                {
                    i.Id,
                    i.Nome,
                    i.Quantidade,
                    i.Unidade
                })
            });
        });

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

            return Results.Ok(solicitacoes.Select(s => new
            {
                s.Id,
                s.Titulo,
                s.Descricao,
                s.TipoAjuda,
                s.Status,
                s.DataCriacao,
                Itens = s.Itens.Select(i => new
                {
                    i.Id,
                    i.Nome,
                    i.Quantidade,
                    i.Unidade
                })
            }));
        });
        
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

                return Results.Ok(new
                {
                    solicitacao.Id,
                    solicitacao.Status
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { erro = ex.Message });
            }
        });
    }
}