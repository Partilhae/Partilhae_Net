using Partilhae_Net.Models;
using Partilhae_Net.Services;
using Partilhae_Net.Tests.Helpers;

namespace Partilhae_Net.Tests.Services;

public class SolicitacaoServiceTests
{
    [Fact]
    public async Task BuscarPorIdAsync_DeveRetornarSolicitacaoExistente()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        var solicitacao = new Solicitacao
        {
            Id = 1,
            IdReceptor = 10,
            Titulo = "Doação de alimentos",
            Descricao = "Preciso de alimentos",
            TipoAjuda = "ESPECIFICA",
            Status = "ABERTA",
            DataCriacao = DateTime.UtcNow
        };

        db.Solicitacoes.Add(solicitacao);
        await db.SaveChangesAsync();

        var service = new SolicitacaoService(db, new FakeIdGenerator());

        var resultado = await service.BuscarPorIdAsync(1);

        Assert.NotNull(resultado);
        Assert.Equal(1, resultado.Id);
        Assert.Equal("Doação de alimentos", resultado.Titulo);
        Assert.Equal("ABERTA", resultado.Status);
    }
    
    [Fact]
    public async Task BuscarPorIdAsync_DeveRetornarNullQuandoNaoExiste()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        var service = new SolicitacaoService(db, new FakeIdGenerator());

        var resultado = await service.BuscarPorIdAsync(999);

        Assert.Null(resultado);
    }
    
    [Fact]
    public async Task ListarAsync_DeveFiltrarPorStatus()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        db.Solicitacoes.AddRange(
            new Solicitacao
            {
                Id = 1,
                IdReceptor = 10,
                Titulo = "Alimentos",
                Descricao = "Preciso de alimentos",
                TipoAjuda = "ESPECIFICA",
                Status = "ABERTA",
                DataCriacao = DateTime.UtcNow
            },
            new Solicitacao
            {
                Id = 2,
                IdReceptor = 20,
                Titulo = "Roupas",
                Descricao = "Preciso de roupas",
                TipoAjuda = "QUALQUER",
                Status = "CONCLUIDA",
                DataCriacao = DateTime.UtcNow
            }
        );

        await db.SaveChangesAsync();

        var service = new SolicitacaoService(db, new FakeIdGenerator());

        var resultado = await service.ListarAsync("ABERTA");

        Assert.Single(resultado);
        Assert.Equal(1, resultado[0].Id);
        Assert.Equal("ABERTA", resultado[0].Status);
    }
    
    [Fact]
    public async Task ListarPorReceptorAsync_DeveRetornarSomenteSolicitacoesDoReceptor()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        db.Solicitacoes.AddRange(
            new Solicitacao
            {
                Id = 1,
                IdReceptor = 10,
                Titulo = "Alimentos",
                Descricao = "Preciso de alimentos",
                TipoAjuda = "ESPECIFICA",
                Status = "ABERTA",
                DataCriacao = DateTime.UtcNow
            },
            new Solicitacao
            {
                Id = 2,
                IdReceptor = 20,
                Titulo = "Roupas",
                Descricao = "Preciso de roupas",
                TipoAjuda = "QUALQUER",
                Status = "ABERTA",
                DataCriacao = DateTime.UtcNow
            },
            new Solicitacao
            {
                Id = 3,
                IdReceptor = 10,
                Titulo = "Material escolar",
                Descricao = "Preciso de cadernos",
                TipoAjuda = "ESPECIFICA",
                Status = "CONCLUIDA",
                DataCriacao = DateTime.UtcNow
            }
        );

        await db.SaveChangesAsync();

        var service = new SolicitacaoService(db, new FakeIdGenerator());

        var resultado = await service.ListarPorReceptorAsync(10);

        Assert.Equal(2, resultado.Count);
        Assert.All(resultado, s => Assert.Equal(10, s.IdReceptor));
        Assert.Contains(resultado, s => s.Id == 1);
        Assert.Contains(resultado, s => s.Id == 3);
    }
    
    [Fact]
    public async Task AtualizarStatusAsync_DeveCancelarSolicitacaoAberta()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        db.Solicitacoes.Add(new Solicitacao
        {
            Id = 1,
            IdReceptor = 10,
            Titulo = "Doação de alimentos",
            Descricao = "Preciso de alimentos",
            TipoAjuda = "ESPECIFICA",
            Status = "ABERTA",
            DataCriacao = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var service = new SolicitacaoService(db, new FakeIdGenerator());

        var resultado = await service.AtualizarStatusAsync(
            id: 1,
            idReceptor: 10,
            status: "CANCELADA"
        );

        Assert.NotNull(resultado);
        Assert.Equal("CANCELADA", resultado.Status);

        db.ChangeTracker.Clear();

        var solicitacaoSalva = await db.Solicitacoes.FindAsync(1L);

        Assert.NotNull(solicitacaoSalva);
        Assert.Equal("CANCELADA", solicitacaoSalva.Status);
    }
    
    [Fact]
    public async Task AtualizarStatusAsync_DeveBloquearReceptorDiferente()
    {
        var (db, connection) = TestDbContextFactory.Criar();

        await using var context = db;
        await using var conexao = connection;

        db.Solicitacoes.Add(new Solicitacao
        {
            Id = 1,
            IdReceptor = 10,
            Titulo = "Doação de alimentos",
            Descricao = "Preciso de alimentos",
            TipoAjuda = "ESPECIFICA",
            Status = "ABERTA",
            DataCriacao = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var service = new SolicitacaoService(db, new FakeIdGenerator());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.AtualizarStatusAsync(
                id: 1,
                idReceptor: 20,
                status: "CANCELADA"
            )
        );

        db.ChangeTracker.Clear();

        var solicitacaoSalva = await db.Solicitacoes.FindAsync(1L);

        Assert.NotNull(solicitacaoSalva);
        Assert.Equal("ABERTA", solicitacaoSalva.Status);
    }
    
    [Fact]
    public async Task AtualizarStatusAsync_DeveRetornarNullQuandoNaoExiste()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        var service = new SolicitacaoService(db, new FakeIdGenerator());

        var resultado = await service.AtualizarStatusAsync(999, 10, "CANCELADA");

        Assert.Null(resultado);
    }

    [Theory]
    [InlineData("CONCLUIDA")]
    [InlineData("EM_ANDAMENTO")]
    [InlineData("ABERTA")]
    public async Task AtualizarStatusAsync_DeveRejeitarStatusDiferenteDeCancelada(
        string novoStatus)
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        db.Solicitacoes.Add(new Solicitacao
        {
            Id = 1,
            IdReceptor = 10,
            Titulo = "Alimentos",
            Descricao = "Preciso de alimentos",
            TipoAjuda = "ESPECIFICA",
            Status = "ABERTA",
            DataCriacao = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var service = new SolicitacaoService(db, new FakeIdGenerator());

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.AtualizarStatusAsync(1, 10, novoStatus)
        );
    }

    [Theory]
    [InlineData("CANCELADA")]
    [InlineData("CONCLUIDA")]
    [InlineData("EM_ANDAMENTO")]
    public async Task AtualizarStatusAsync_DeveImpedirCancelamentoSeNaoEstiverAberta(
        string statusAtual)
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        db.Solicitacoes.Add(new Solicitacao
        {
            Id = 1,
            IdReceptor = 10,
            Titulo = "Alimentos",
            Descricao = "Preciso de alimentos",
            TipoAjuda = "ESPECIFICA",
            Status = statusAtual,
            DataCriacao = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var service = new SolicitacaoService(db, new FakeIdGenerator());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AtualizarStatusAsync(1, 10, "CANCELADA")
        );
    }

    [Fact]
    public async Task ListarAsync_SemFiltro_DeveRetornarTodas()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        db.Solicitacoes.AddRange(
            new Solicitacao
            {
                Id = 1,
                IdReceptor = 10,
                Titulo = "Alimentos",
                Descricao = "Descrição",
                TipoAjuda = "ESPECIFICA",
                Status = "ABERTA",
                DataCriacao = DateTime.UtcNow
            },
            new Solicitacao
            {
                Id = 2,
                IdReceptor = 20,
                Titulo = "Roupas",
                Descricao = "Descrição",
                TipoAjuda = "QUALQUER",
                Status = "CONCLUIDA",
                DataCriacao = DateTime.UtcNow
            }
        );

        await db.SaveChangesAsync();

        var service = new SolicitacaoService(db, new FakeIdGenerator());

        var resultado = await service.ListarAsync(null);

        Assert.Equal(2, resultado.Count);
    }

    [Fact]
    public async Task ListarPorReceptorAsync_DeveRetornarListaVaziaQuandoNaoExistemSolicitacoes()
    {
        var (db, connection) = TestDbContextFactory.Criar();
        await using var context = db;
        await using var conexao = connection;

        var service = new SolicitacaoService(db, new FakeIdGenerator());

        var resultado = await service.ListarPorReceptorAsync(999);

        Assert.Empty(resultado);
    }
}