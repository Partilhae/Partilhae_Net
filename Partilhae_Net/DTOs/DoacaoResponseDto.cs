namespace Partilhae_Net.DTOs;

public record DoacaoResponseDto(
    long Id,
    long IdConversa,
    long IdSolicitacao,
    long IdDoador,
    string Status,
    DateTime DataCriacao,
    DateTime? DataEntregueDoador,
    DateTime? DataRecebidoReceptor,
    DateTime? DataEncerramentoChat
);