using Partilhae_Net.DTOs;
using Partilhae_Net.Services;
using Partilhae_Net.Tests.Helpers;

namespace Partilhae_Net.Tests.Services;

public class SolicitacaoCriacaoTests
{
    private static CriarSolicitacaoDto CriarDtoValido()
    {
        return new CriarSolicitacaoDto
        {
            Titulo = "Doação de alimentos",
            Descricao = "Precisamos de alimentos",
            TipoAjuda = "ESPECIFICA",
            Itens = new List<CriarItemSolicitacaoDto>()
        };
    }

    [Theory]
    [InlineData("INVALIDO")]
    [InlineData("")]
    [InlineData("DINHEIRO")]
    [InlineData("OUTRO_TIPO")]
    public async Task Criar_TipoAjudaInvalido_DeveLancarExcecao(
        string tipo)
    {
        var (db, connection) = TestDbContextFactory.Criar();

        using (db)
        using (connection)
        {
            var service = new SolicitacaoService(db, new FakeIdGenerator());
            var dto = CriarDtoValido();
            dto.TipoAjuda = tipo;

            await Assert.ThrowsAsync<ArgumentException>(
                () => service.CriarAsync(10, dto));
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task Criar_TituloVazio_DeveLancarExcecao(
        string titulo)
    {
        var (db, connection) = TestDbContextFactory.Criar();

        using (db)
        using (connection)
        {
            var service = new SolicitacaoService(db, new FakeIdGenerator());
            var dto = CriarDtoValido();
            dto.Titulo = titulo;

            var excecao = await Assert.ThrowsAsync<ArgumentException>(
                () => service.CriarAsync(10, dto));

            Assert.Equal("O título é obrigatório.", excecao.Message);
        }
    }
    
    [Fact]
    public async Task Criar_SolicitacaoValida_DeveSalvarNoBanco()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        var service = new SolicitacaoService(
            db, new FakeIdGenerator());

        var dto = CriarDtoValido();

        var resultado = await service.CriarAsync(10, dto);

        Assert.True(resultado.Id > 0);
        Assert.Equal(10L, resultado.IdReceptor);
        Assert.Equal("ABERTA", resultado.Status);

        db.ChangeTracker.Clear();

        var salva = await db.Solicitacoes.FindAsync(resultado.Id);

        Assert.NotNull(salva);
        Assert.Equal(dto.Titulo, salva.Titulo);
    }

    [Fact]
    public async Task Criar_DeveRemoverEspacosDosDados()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        var service = new SolicitacaoService(
            db, new FakeIdGenerator());

        var dto = CriarDtoValido();

        dto.Titulo = "  Doação de alimentos  ";
        dto.Descricao = "  Precisamos de ajuda  ";

        var resultado = await service.CriarAsync(10, dto);

        Assert.Equal("Doação de alimentos", resultado.Titulo);
        Assert.Equal("Precisamos de ajuda", resultado.Descricao);
    }

    [Fact]
    public async Task Criar_ComItens_DeveSalvarRelacionamento()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        var service = new SolicitacaoService(
            db, new FakeIdGenerator());

        var dto = CriarDtoValido();

        dto.Itens.Add(new CriarItemSolicitacaoDto
        {
            Nome = "  Arroz  ",
            Quantidade = 5,
            Unidade = "  KG  "
        });

        dto.Itens.Add(new CriarItemSolicitacaoDto
        {
            Nome = "Feijão",
            Quantidade = 3,
            Unidade = "KG"
        });

        var resultado = await service.CriarAsync(10, dto);

        db.ChangeTracker.Clear();

        var salva = await service.BuscarPorIdAsync(resultado.Id);

        Assert.NotNull(salva);
        Assert.Equal(2, salva.Itens.Count);

        Assert.All(salva.Itens, item =>
        {
            Assert.True(item.Id > 0);
            Assert.Equal(resultado.Id, item.IdSolicitacao);
        });

        Assert.Contains(salva.Itens, i =>
            i.Nome == "Arroz" && i.Unidade == "KG");

        Assert.Equal(2, salva.Itens.Select(i => i.Id).Distinct().Count());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public async Task Criar_QuantidadeInvalida_DeveRejeitar(
        int quantidade)
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        var service = new SolicitacaoService(
            db, new FakeIdGenerator());

        var dto = CriarDtoValido();

        dto.Itens.Add(new CriarItemSolicitacaoDto
        {
            Nome = "Arroz",
            Quantidade = quantidade,
            Unidade = "KG"
        });

        var excecao = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CriarAsync(10, dto));

        Assert.Equal(
            "A quantidade dos itens deve ser positiva.",
            excecao.Message);

        Assert.Empty(db.Solicitacoes);
    }

    [Fact]
    public async Task Criar_DeveGerarIdsDiferentes()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        var service = new SolicitacaoService(
            db, new FakeIdGenerator());

        var primeira = await service.CriarAsync(10, CriarDtoValido());
        var segunda = await service.CriarAsync(10, CriarDtoValido());

        Assert.NotEqual(primeira.Id, segunda.Id);
        Assert.Equal(2, db.Solicitacoes.Count());
    }
}