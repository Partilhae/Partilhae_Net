namespace Partilhae_Net.Models;

public class Mensagem
{
    public long Id { get; set; }

    public long IdConversa { get; set; }

    public long IdRemetenteUsuario { get; set; }

    public string Conteudo { get; set; } = string.Empty;

    public DateTime DataEnvio { get; set; } = DateTime.UtcNow;

    public Conversa Conversa { get; set; } = null!;
}