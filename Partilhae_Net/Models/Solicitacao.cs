namespace Partilhae_Net.Models;

public class Solicitacao
{
    public long Id { get; set; }

    public long IdReceptor { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    public string TipoAjuda { get; set; } = string.Empty;

    public string Status { get; set; } = "aberta";

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public List<ItemSolicitacao> Itens { get; set; } = new();
}