namespace Partilhae_Net.DTOs;

public record ItemSolicitacaoResponseDto(
    long Id,
    string Nome,
    decimal? Quantidade,
    string Unidade
);