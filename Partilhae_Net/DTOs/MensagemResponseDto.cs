namespace Partilhae_Net.DTOs;

public record MensagemResponseDto(
    long Id,
    long IdConversa,
    long IdRemetenteUsuario,
    string Conteudo,
    DateTime DataEnvio
);