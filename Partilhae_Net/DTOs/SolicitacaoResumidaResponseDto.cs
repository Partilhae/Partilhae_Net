namespace Partilhae_Net.DTOs;

public record SolicitacaoResumidaResponseDto(
    long Id,
    string Titulo,
    string Descricao,
    string TipoAjuda,
    string Status,
    DateTime DataCriacao,
    List<ItemSolicitacaoResponseDto> Itens
);