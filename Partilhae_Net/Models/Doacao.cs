namespace Partilhae_Net.Models;

public class Doacao
{
    public long Id { get; set; }
    public long IdConversa { get; set; }
    public long IdSolicitacao { get; set; }
    public long IdDoador { get; set; }

    public string Status { get; set; } = "EM_ANDAMENTO";

    public DateTime? DataAceiteAvisoDoador { get; set; }
    public DateTime? DataAceiteAvisoReceptor { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataEntregueDoador { get; set; }
    public DateTime? DataRecebidoReceptor { get; set; }
    public DateTime? DataEncerramentoChat { get; set; }
    public DateTime? DataCancelamento { get; set; }

    public string? MotivoCancelamento { get; set; }

    public Conversa Conversa { get; set; } = null!;
    public Solicitacao Solicitacao { get; set; } = null!;
}