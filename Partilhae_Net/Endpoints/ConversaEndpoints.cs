using System.Security.Claims;
using Partilhae_Net.DTOs;
using Partilhae_Net.Models;
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

                return Results.Ok(RespostaConversa(conversa));
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
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(
                    new ErroResponseDto(ex.Message));
            }
        })
        .WithSummary("Criar ou buscar conversa")
        .WithDescription(
            "Permite que um doador autenticado inicie uma conversa " +
            "relacionada a uma solicitação. Caso já exista uma conversa, " +
            "retorna a conversa existente. Aceita Idempotency-Key.")
        .Produces<ConversaResponseDto>(
            StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces<ErroResponseDto>(
            StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status429TooManyRequests);

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

            return Results.Ok(
                conversas.Select(RespostaConversa).ToList());
        })
        .WithSummary("Listar conversas do usuário")
        .WithDescription(
            "Retorna as conversas do usuário autenticado, " +
            "seja ele doador ou receptor. Não permite consultar " +
            "as conversas de outro usuário.")
        .Produces<List<ConversaResponseDto>>(
            StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status429TooManyRequests);

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

                return Results.Ok(
                    mensagens.Select(RespostaMensagem).ToList());
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
        })
        .WithSummary("Listar mensagens da conversa")
        .WithDescription(
            "Retorna o histórico de mensagens de uma conversa. " +
            "Somente o doador e o receptor participantes " +
            "podem acessar as mensagens.")
        .Produces<List<MensagemResponseDto>>(
            StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces<ErroResponseDto>(
            StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status429TooManyRequests);

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
                    RespostaMensagem(mensagem));
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
        })
        .WithSummary("Enviar mensagem")
        .WithDescription(
            "Envia uma mensagem em uma conversa existente. " +
            "Somente os participantes podem enviar mensagens. " +
            "O conteúdo deve ter entre 1 e 1000 caracteres. " +
            "Aceita Idempotency-Key para evitar mensagens duplicadas.")
        .Produces<MensagemResponseDto>(
            StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces<ErroResponseDto>(
            StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status429TooManyRequests);
    }

    private static ConversaResponseDto RespostaConversa(
        Conversa c)
    {
        return new ConversaResponseDto(
            c.Id,
            c.IdSolicitacao,
            c.IdDoador,
            c.IdReceptor,
            c.DataCriacao);
    }

    private static MensagemResponseDto RespostaMensagem(
        Mensagem m)
    {
        return new MensagemResponseDto(
            m.Id,
            m.IdConversa,
            m.IdRemetenteUsuario,
            m.Conteudo,
            m.DataEnvio);
    }
}