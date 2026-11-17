namespace Partilhae_Net.Services;

public static class DoacaoValidator
{
    public static bool IdValido(long id)
    {
        return id > 0;
    }

    public static bool PodeCriar(
        string statusSolicitacao,
        long idDoadorConversa,
        long idDoadorAutenticado,
        bool jaExisteDoacao,
        bool chatLiberado)
    {
        return statusSolicitacao == "ABERTA"
               && idDoadorConversa == idDoadorAutenticado
               && !jaExisteDoacao
               && chatLiberado;
    }

    public static bool PodeMarcarEntregue(
        string statusAtual,
        long idDoadorDoacao,
        long? idDoadorAutenticado)
    {
        return statusAtual == "EM_ANDAMENTO"
               && idDoadorDoacao == idDoadorAutenticado;
    }

    public static bool PodeConcluir(
        string statusAtual,
        long idReceptorConversa,
        long? idReceptorAutenticado)
    {
        return statusAtual == "ENTREGUE"
               && idReceptorConversa == idReceptorAutenticado;
    }

    public static bool StatusPermitido(string? status)
    {
        return status?.Trim().ToUpperInvariant()
            is "ENTREGUE" or "CONCLUIDA";
    }
}