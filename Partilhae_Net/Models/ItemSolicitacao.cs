namespace Partilhae_Net.Models;

public class ItemSolicitacao
{
    public long Id { get; set; }

    public long IdSolicitacao { get; set; }

    public string Nome { get; set; } = string.Empty;

    public decimal? Quantidade { get; set; }

    public string Unidade { get; set; } = string.Empty;

    public Solicitacao Solicitacao { get; set; } = null!;
}