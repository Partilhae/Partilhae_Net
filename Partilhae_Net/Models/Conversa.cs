namespace Partilhae_Net.Models;

public class Conversa
{
    public long Id { get; set; }

    public long IdSolicitacao { get; set; }

    public long IdDoador { get; set; }

    public long IdReceptor { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public Solicitacao Solicitacao { get; set; } = null!;

    public List<Mensagem> Mensagens { get; set; } = new();
}