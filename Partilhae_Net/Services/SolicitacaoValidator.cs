namespace Partilhae_Net.Services;

public static class SolicitacaoValidator
{
    public static bool TipoAjudaValido(string? tipo)
    {
        return tipo?.Trim().ToUpperInvariant()
            is "ESPECIFICA" or "ESPECIFICO" or "QUALQUER";
    }

    public static bool QuantidadeValida(decimal quantidade)
    {
        return quantidade > 0;
    }

    public static bool PodeCancelar(
        string statusAtual,
        long idReceptorSolicitacao,
        long idReceptorAutenticado)
    {
        return statusAtual == "ABERTA" &&
               idReceptorSolicitacao == idReceptorAutenticado;
    }
}