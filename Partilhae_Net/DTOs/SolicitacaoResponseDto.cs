namespace Partilhae_Net.DTOs;

public record SolicitacaoResponseDto(
    long Id,
    long IdReceptor,
    string Titulo,
    string Descricao,
    string TipoAjuda,
    string Status,
    DateTime DataCriacao,
    List<ItemSolicitacaoResponseDto> Itens
);