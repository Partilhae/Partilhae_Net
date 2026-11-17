using Partilhae_Net.Services;

namespace Partilhae_Net.Tests;

public class DoacaoValidatorTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(999)]
    public void IdValido_DeveAceitarIdsPositivos(long id)
    {
        Assert.True(DoacaoValidator.IdValido(id));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void IdValido_DeveRejeitarIdsInvalidos(long id)
    {
        Assert.False(DoacaoValidator.IdValido(id));
    }

    [Fact]
    public void PodeCriar_DevePermitirDoacaoValida()
    {
        Assert.True(DoacaoValidator.PodeCriar(
            "ABERTA", 5, 5, false, true));
    }

    [Theory]
    [InlineData("EM_ANDAMENTO", 5, 5, false, true)]
    [InlineData("CANCELADA", 5, 5, false, true)]
    [InlineData("ABERTA", 5, 10, false, true)]
    [InlineData("ABERTA", 5, 5, true, true)]
    [InlineData("ABERTA", 5, 5, false, false)]
    public void PodeCriar_DeveBloquearDoacaoInvalida(
        string status,
        long idDoadorConversa,
        long idDoadorAutenticado,
        bool jaExiste,
        bool chatLiberado)
    {
        Assert.False(DoacaoValidator.PodeCriar(
            status,
            idDoadorConversa,
            idDoadorAutenticado,
            jaExiste,
            chatLiberado));
    }

    [Theory]
    [InlineData("EM_ANDAMENTO", 5, 5, true)]
    [InlineData("ENTREGUE", 5, 5, false)]
    [InlineData("CONCLUIDA", 5, 5, false)]
    [InlineData("EM_ANDAMENTO", 5, 10, false)]
    [InlineData("EM_ANDAMENTO", 5, null, false)]
    public void PodeMarcarEntregue_DeveRespeitarRegras(
        string status,
        long idDoador,
        int? idAutenticado,
        bool esperado)
    {
        Assert.Equal(esperado,
            DoacaoValidator.PodeMarcarEntregue(
                status, idDoador, idAutenticado));
    }

    [Theory]
    [InlineData("ENTREGUE", 8, 8, true)]
    [InlineData("EM_ANDAMENTO", 8, 8, false)]
    [InlineData("CONCLUIDA", 8, 8, false)]
    [InlineData("ENTREGUE", 8, 10, false)]
    [InlineData("ENTREGUE", 8, null, false)]
    public void PodeConcluir_DeveRespeitarRegras(
        string status,
        long idReceptor,
        int? idAutenticado,
        bool esperado)
    {
        Assert.Equal(esperado,
            DoacaoValidator.PodeConcluir(
                status, idReceptor, idAutenticado));
    }

    [Theory]
    [InlineData("ENTREGUE", true)]
    [InlineData("CONCLUIDA", true)]
    [InlineData("entregue", true)]
    [InlineData("CANCELADA", false)]
    [InlineData("EM_ANDAMENTO", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void StatusPermitido_DeveValidarStatus(
        string? status,
        bool esperado)
    {
        Assert.Equal(esperado,
            DoacaoValidator.StatusPermitido(status));
    }
}