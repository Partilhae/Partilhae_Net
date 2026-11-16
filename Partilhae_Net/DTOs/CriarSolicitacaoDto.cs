namespace Partilhae_Net.DTOs;

public class CriarSolicitacaoDto
{
    public string Titulo { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string TipoAjuda { get; set; } = "";
    public List<CriarItemSolicitacaoDto> Itens { get; set; } = new();
}

public class CriarItemSolicitacaoDto
{
    public string Nome { get; set; } = "";
    public decimal Quantidade { get; set; }
    public string Unidade { get; set; } = "";
}