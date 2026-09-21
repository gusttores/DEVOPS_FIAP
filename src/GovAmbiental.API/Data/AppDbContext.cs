using GovAmbiental.API.Models;
using Microsoft.EntityFrameworkCore;

namespace GovAmbiental.API.Data;

/// <summary>
/// Contexto do Entity Framework Core. Mapeia o domínio de conformidade ambiental
/// para o SQL Server e define índices voltados à otimização das consultas paginadas.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<NormaAmbiental> Normas => Set<NormaAmbiental>();
    public DbSet<RequisitoNorma> Requisitos => Set<RequisitoNorma>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();
    public DbSet<ItemAuditoria> ItensAuditoria => Set<ItemAuditoria>();
    public DbSet<NaoConformidade> NaoConformidades => Set<NaoConformidade>();
    public DbSet<AcaoCorretiva> AcoesCorretivas => Set<AcaoCorretiva>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<NormaAmbiental>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Codigo).HasMaxLength(40).IsRequired();
            e.Property(n => n.Titulo).HasMaxLength(200).IsRequired();
            e.Property(n => n.Descricao).HasMaxLength(1000);
            e.Property(n => n.OrgaoEmissor).HasMaxLength(80).IsRequired();
            e.Property(n => n.Categoria).HasConversion<string>().HasMaxLength(30);
            e.HasIndex(n => n.Codigo).IsUnique();
            // Índices que sustentam os filtros das listagens paginadas.
            e.HasIndex(n => n.Categoria);
            e.HasIndex(n => new { n.Ativa, n.Categoria });
        });

        modelBuilder.Entity<RequisitoNorma>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Descricao).HasMaxLength(500).IsRequired();
            e.Property(r => r.Criticidade).HasConversion<string>().HasMaxLength(20);
            e.HasOne(r => r.Norma)
                .WithMany(n => n.Requisitos)
                .HasForeignKey(r => r.NormaAmbientalId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(r => r.NormaAmbientalId);
        });

        modelBuilder.Entity<Auditoria>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Titulo).HasMaxLength(200).IsRequired();
            e.Property(a => a.UnidadeOperacional).HasMaxLength(120).IsRequired();
            e.Property(a => a.Responsavel).HasMaxLength(120).IsRequired();
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(a => a.Resultado).HasConversion<string>().HasMaxLength(30);
            e.Property(a => a.ScoreConformidade).HasPrecision(5, 2);
            // Índices para filtros e ordenação por data nas listagens.
            e.HasIndex(a => a.Status);
            e.HasIndex(a => a.DataInicio);
            e.HasIndex(a => new { a.Status, a.UnidadeOperacional });
        });

        modelBuilder.Entity<ItemAuditoria>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Observacao).HasMaxLength(500);
            e.Property(i => i.Evidencia).HasMaxLength(300);
            e.HasOne(i => i.Auditoria)
                .WithMany(a => a.Itens)
                .HasForeignKey(i => i.AuditoriaId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Requisito)
                .WithMany(r => r.Itens)
                .HasForeignKey(i => i.RequisitoNormaId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(i => new { i.AuditoriaId, i.RequisitoNormaId }).IsUnique();
        });

        modelBuilder.Entity<NaoConformidade>(e =>
        {
            e.HasKey(nc => nc.Id);
            e.Property(nc => nc.Descricao).HasMaxLength(500).IsRequired();
            e.Property(nc => nc.Criticidade).HasConversion<string>().HasMaxLength(20);
            e.HasOne(nc => nc.Auditoria)
                .WithMany(a => a.NaoConformidades)
                .HasForeignKey(nc => nc.AuditoriaId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(nc => nc.Requisito)
                .WithMany()
                .HasForeignKey(nc => nc.RequisitoNormaId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(nc => nc.AuditoriaId);
        });

        modelBuilder.Entity<AcaoCorretiva>(e =>
        {
            e.HasKey(ac => ac.Id);
            e.Property(ac => ac.Descricao).HasMaxLength(500).IsRequired();
            e.Property(ac => ac.Responsavel).HasMaxLength(120).IsRequired();
            e.Property(ac => ac.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(ac => ac.NaoConformidade)
                .WithOne(nc => nc.AcaoCorretiva)
                .HasForeignKey<AcaoCorretiva>(ac => ac.NaoConformidadeId)
                .OnDelete(DeleteBehavior.Cascade);
            // Índice para os alertas de ações vencidas / a vencer.
            e.HasIndex(ac => new { ac.Status, ac.PrazoLimite });
        });
    }
}
