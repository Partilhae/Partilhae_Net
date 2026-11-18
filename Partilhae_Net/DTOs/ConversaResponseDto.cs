namespace Partilhae_Net.DTOs;

public record ConversaResponseDto(
    long Id,
    long IdSolicitacao,
    long IdDoador,
    long IdReceptor,
    DateTime DataCriacao
);