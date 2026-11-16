using System.Security.Claims;
using Partilhae_Net.DTOs;
using Partilhae_Net.Services;

namespace Partilhae_Net.Endpoints;

public static class ConversaEndpoints
{
    public static void MapConversaEndpoints(this WebApplication app)
    {
        var grupo = app.MapGroup("/conversas")
            .WithTags("Conversas")
            .RequireAuthorization();

        grupo.MapPost("/", async (
            CriarConversaDto dto,
            ClaimsPrincipal usuario,
            ConversaService service) =>
        {
            var role = usuario.FindFirst("role")?.Value;
            var idClaim = usuario.FindFirst("idDoador")?.Value;

            if (role != "ROLE_DOADOR" ||
                !long.TryParse(idClaim, out var idDoador) ||
                idDoador <= 0)
            {
                return Results.Forbid();
            }

            try
            {
                var conversa = await service.CriarOuBuscarAsync(
                    dto.IdSolicitacao, idDoador);

                return Results.Ok(new
                {
                    conversa.Id,
                    conversa.IdSolicitacao,
                    conversa.IdDoador,
                    conversa.IdReceptor,
                    conversa.DataCriacao
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { erro = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { erro = ex.Message });
            }
        });
        
        grupo.MapGet("/usuario/{idUsuario:long}", async (
            long idUsuario,
            ClaimsPrincipal usuario,
            ConversaService service) =>
        {
            var idUsuarioClaim = usuario.FindFirst("idUsuario")?.Value;

            if (!long.TryParse(idUsuarioClaim, out var idAutenticado) ||
                idAutenticado != idUsuario)
            {
                return Results.Forbid();
            }

            long? idDoador = long.TryParse(
                usuario.FindFirst("idDoador")?.Value, out var d)
                ? d : null;

            long? idReceptor = long.TryParse(
                usuario.FindFirst("idReceptor")?.Value, out var r)
                ? r : null;

            var conversas = await service.ListarPorUsuarioAsync(
                idUsuario, idDoador, idReceptor);

            return Results.Ok(conversas.Select(c => new
            {
                c.Id,
                c.IdSolicitacao,
                c.IdDoador,
                c.IdReceptor,
                c.DataCriacao
            }));
        });
        
        grupo.MapGet("/{idConversa:long}/mensagens", async (
            long idConversa,
            ClaimsPrincipal usuario,
            ConversaService service) =>
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
                var mensagens = await service.ListarMensagensAsync(
                    idConversa, idDoador, idReceptor);

                return Results.Ok(mensagens.Select(m => new
                {
                    m.Id,
                    m.IdConversa,
                    m.IdRemetenteUsuario,
                    m.Conteudo,
                    m.DataEnvio
                }));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { erro = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });
        
        grupo.MapPost("/{idConversa:long}/mensagens", async (
            long idConversa,
            EnviarMensagemDto dto,
            ClaimsPrincipal usuario,
            ConversaService service) =>
        {
            var role = usuario.FindFirst("role")?.Value;

            if (!long.TryParse(
                    usuario.FindFirst("idUsuario")?.Value,
                    out var idUsuario) || idUsuario <= 0)
            {
                return Results.Forbid();
            }

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
                var mensagem = await service.EnviarMensagemAsync(
                    idConversa,
                    idUsuario,
                    idDoador,
                    idReceptor,
                    dto.Conteudo);

                return Results.Created(
                    $"/conversas/{idConversa}/mensagens",
                    new
                    {
                        mensagem.Id,
                        mensagem.IdConversa,
                        mensagem.IdRemetenteUsuario,
                        mensagem.Conteudo,
                        mensagem.DataEnvio
                    });
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
        });
    }
}