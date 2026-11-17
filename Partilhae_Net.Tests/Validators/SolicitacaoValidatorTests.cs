using Partilhae_Net.Services;

namespace Partilhae_Net.Tests.Validators;

public class SolicitacaoValidatorTests
{
    [Theory]
    [InlineData("ESPECIFICA")]
    [InlineData("ESPECIFICO")]
    [InlineData("QUALQUER")]
    [InlineData("especifica")]
    public void TipoAjudaValido_DeveAceitarTiposPermitidos(
        string tipo)
    {
        Assert.True(SolicitacaoValidator.TipoAjudaValido(tipo));
    }

    [Theory]
    [InlineData("DINHEIRO")]
    [InlineData("")]
    [InlineData("ROUPAS")]
    public void TipoAjudaValido_DeveRejeitarTiposInvalidos(
        string tipo)
    {
        Assert.False(SolicitacaoValidator.TipoAjudaValido(tipo));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(0.5)]
    public void QuantidadeValida_DeveAceitarValoresPositivos(
        decimal quantidade)
    {
        Assert.True(SolicitacaoValidator.QuantidadeValida(
            quantidade));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void QuantidadeValida_DeveRejeitarValoresInvalidos(
        decimal quantidade)
    {
        Assert.False(SolicitacaoValidator.QuantidadeValida(
            quantidade));
    }

    [Fact]
    public void PodeCancelar_DevePermitirReceptorResponsavel()
    {
        var resultado = SolicitacaoValidator.PodeCancelar(
            "ABERTA", 5, 5);

        Assert.True(resultado);
    }

    [Fact]
    public void PodeCancelar_DeveBloquearOutroReceptor()
    {
        var resultado = SolicitacaoValidator.PodeCancelar(
            "ABERTA", 5, 10);

        Assert.False(resultado);
    }

    [Fact]
    public void PodeCancelar_DeveBloquearSolicitacaoEmAndamento()
    {
        var resultado = SolicitacaoValidator.PodeCancelar(
            "EM_ANDAMENTO", 5, 5);

        Assert.False(resultado);
    }
}