using Microsoft.EntityFrameworkCore;
using Partilhae_Net.Models;

namespace Partilhae_Net.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Solicitacao> Solicitacoes => Set<Solicitacao>();
    public DbSet<ItemSolicitacao> ItensSolicitacao => Set<ItemSolicitacao>();
    public DbSet<Conversa> Conversas => Set<Conversa>();
    public DbSet<Mensagem> Mensagens => Set<Mensagem>();
    public DbSet<Doacao> Doacoes => Set<Doacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Solicitacao>(e =>
        {
            e.ToTable("SOLICITACAO");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id)
                .HasColumnName("ID_SOLICITACAO")
                .ValueGeneratedNever();

            e.Property(x => x.IdReceptor)
                .HasColumnName("RECEPTOR_ID_RECEPTOR");

            e.Property(x => x.Titulo)
                .HasColumnName("NM_TITULO")
                .HasMaxLength(100)
                .IsRequired();

            e.Property(x => x.Descricao)
                .HasColumnName("DS_SOLICITACAO")
                .HasMaxLength(500)
                .IsRequired(false);

            e.Property(x => x.TipoAjuda)
                .HasColumnName("TP_TIPO_AJUDA")
                .HasMaxLength(20)
                .IsRequired();

            e.Property(x => x.Status)
                .HasColumnName("TP_STATUS")
                .HasMaxLength(20)
                .IsRequired();

            e.Property(x => x.DataCriacao)
                .HasColumnName("DT_CRIACAO")
                .HasColumnType("DATE")
                .IsRequired();

            e.HasMany(x => x.Itens)
                .WithOne(x => x.Solicitacao)
                .HasForeignKey(x => x.IdSolicitacao)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ItemSolicitacao>(e =>
        {
            e.ToTable("ITEM_SOLICITACAO");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id)
                .HasColumnName("ID_ITEM")
                .ValueGeneratedNever();

            e.Property(x => x.IdSolicitacao)
                .HasColumnName("SOLICITACAO_ID_SOLICITACAO");

            e.Property(x => x.Nome)
                .HasColumnName("NM_ITEM")
                .HasMaxLength(80)
                .IsRequired(false);

            e.Property(x => x.Quantidade)
                .HasColumnName("QTD_ITEM")
                .HasPrecision(6, 2)
                .IsRequired(false);

            e.Property(x => x.Unidade)
                .HasColumnName("TP_UNIDADE")
                .HasMaxLength(15)
                .IsRequired(false);
        });

        modelBuilder.Entity<Conversa>(e =>
        {
            e.ToTable("CONVERSA");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id)
                .HasColumnName("ID_CONVERSA")
                .ValueGeneratedNever();

            e.Property(x => x.IdSolicitacao)
                .HasColumnName("SOLICITACAO_ID_SOLICITACAO");

            e.Property(x => x.IdDoador)
                .HasColumnName("DOADOR_ID_DOADOR");

            e.Property(x => x.IdReceptor)
                .HasColumnName("RECEPTOR_ID_RECEPTOR");

            e.Property(x => x.DataCriacao)
                .HasColumnName("DT_CRIACAO")
                .HasColumnType("DATE");

            e.HasIndex(x => new
            {
                x.IdSolicitacao,
                x.IdDoador
            }).IsUnique();

            e.HasOne(x => x.Solicitacao)
                .WithMany()
                .HasForeignKey(x => x.IdSolicitacao)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasMany(x => x.Mensagens)
                .WithOne(x => x.Conversa)
                .HasForeignKey(x => x.IdConversa)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Mensagem>(e =>
        {
            e.ToTable("MENSAGEM");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id)
                .HasColumnName("ID_MENSAGEM")
                .ValueGeneratedNever();

            e.Property(x => x.IdConversa)
                .HasColumnName("CONVERSA_ID_CONVERSA");

            e.Property(x => x.IdRemetenteUsuario)
                .HasColumnName("USUARIO_ID_REMETENTE");

            e.Property(x => x.Conteudo)
                .HasColumnName("DS_TEXTO")
                .HasMaxLength(1000)
                .IsRequired();

            e.Property(x => x.DataEnvio)
                .HasColumnName("DT_ENVIO")
                .HasColumnType("DATE");
        });

        modelBuilder.Entity<Doacao>(e =>
        {
            e.ToTable("DOACAO");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id)
                .HasColumnName("ID_DOACAO")
                .HasDefaultValueSql("SEQ_DOACAO.NEXTVAL")
                .ValueGeneratedOnAdd();

            e.Property(x => x.IdConversa)
                .HasColumnName("CONVERSA_ID_CONVERSA");

            e.Property(x => x.IdSolicitacao)
                .HasColumnName("SOLICITACAO_ID_SOLICITACAO");

            e.Property(x => x.IdDoador)
                .HasColumnName("DOADOR_ID_DOADOR");

            e.Property(x => x.Status)
                .HasColumnName("TP_STATUS")
                .HasMaxLength(20)
                .IsRequired();

            e.Property(x => x.DataCriacao)
                .HasColumnName("DT_INICIO_DOACAO")
                .HasColumnType("DATE");

            e.Property(x => x.DataAceiteAvisoDoador)
                .HasColumnName("DT_ACEITE_AVISO_DOADOR")
                .HasColumnType("DATE");

            e.Property(x => x.DataAceiteAvisoReceptor)
                .HasColumnName("DT_ACEITE_AVISO_RECEPTOR")
                .HasColumnType("DATE");

            e.Property(x => x.DataEntregueDoador)
                .HasColumnName("DT_ENTREGUE_DOADOR")
                .HasColumnType("DATE");

            e.Property(x => x.DataRecebidoReceptor)
                .HasColumnName("DT_RECEBIDO_RECEPTOR")
                .HasColumnType("DATE");

            e.Property(x => x.DataEncerramentoChat)
                .HasColumnName("DT_ENCERRAMENTO_CHAT")
                .HasColumnType("DATE");

            e.Property(x => x.DataCancelamento)
                .HasColumnName("DT_CANCELAMENTO")
                .HasColumnType("DATE");

            e.Property(x => x.MotivoCancelamento)
                .HasColumnName("DS_MOTIVO_CANCELAMENTO")
                .HasMaxLength(300);

            e.HasIndex(x => x.IdConversa).IsUnique();

            e.HasOne(x => x.Conversa)
                .WithMany()
                .HasForeignKey(x => x.IdConversa)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(x => x.Solicitacao)
                .WithMany()
                .HasForeignKey(x => x.IdSolicitacao)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}