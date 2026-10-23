using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PluviAlerta.Domain.Entidades;

namespace PluviAlerta.Infrastructure.Dados;

public class PluviAlertaDbContext(DbContextOptions<PluviAlertaDbContext> options) : DbContext(options)
{
    public DbSet<Regiao> Regioes => Set<Regiao>();
    public DbSet<LeituraChuva> LeiturasChuva => Set<LeituraChuva>();
    public DbSet<ScoreRisco> ScoresRisco => Set<ScoreRisco>();
    public DbSet<Relato> Relatos => Set<Relato>();
    public DbSet<Inscricao> Inscricoes => Set<Inscricao>();
    public DbSet<AlertaEnviado> AlertasEnviados => Set<AlertaEnviado>();

    // Gravamos tudo em UTC, mas o MySQL devolve a data sem fuso. Marcamos como UTC na leitura
    // para o JSON sair com "Z" e o front converter para o horário de Brasília corretamente.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<DataUtcConverter>();
    }

    private class DataUtcConverter() : ValueConverter<DateTime, DateTime>(
        v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Regiao>(e =>
        {
            e.Property(r => r.Nome).HasMaxLength(50);
            e.HasIndex(r => r.Nome).IsUnique();
            e.HasData(SeedRegioes.Todas);
        });

        // Índices por (regional, momento): as consultas são sempre "o último" ou "as últimas 24 h" de uma regional.
        modelBuilder.Entity<LeituraChuva>(e =>
        {
            e.HasOne<Regiao>().WithMany().HasForeignKey(l => l.RegiaoId);
            e.HasIndex(l => new { l.RegiaoId, l.Momento });
            e.Property(l => l.Fonte).HasMaxLength(20);
        });

        modelBuilder.Entity<ScoreRisco>(e =>
        {
            e.HasOne<Regiao>().WithMany().HasForeignKey(s => s.RegiaoId);
            e.HasIndex(s => new { s.RegiaoId, s.Momento });
            e.Property(s => s.Nivel).HasConversion<string>().HasMaxLength(10);
            e.Property(s => s.Fonte).HasMaxLength(20);
        });

        modelBuilder.Entity<Relato>(e =>
        {
            e.HasOne<Regiao>().WithMany().HasForeignKey(r => r.RegiaoId);
            e.HasIndex(r => new { r.RegiaoId, r.ExpiraEm });
            e.Property(r => r.Tipo).HasMaxLength(30);
            e.Property(r => r.Descricao).HasMaxLength(280);
        });

        modelBuilder.Entity<Inscricao>(e =>
        {
            e.HasOne<Regiao>().WithMany().HasForeignKey(i => i.RegiaoId);
            e.HasIndex(i => new { i.TelegramChatId, i.RegiaoId }).IsUnique();
        });

        modelBuilder.Entity<AlertaEnviado>(e =>
        {
            e.HasOne<Regiao>().WithMany().HasForeignKey(a => a.RegiaoId);
            e.HasIndex(a => new { a.RegiaoId, a.EnviadoEm });
            e.Property(a => a.Nivel).HasConversion<string>().HasMaxLength(10);
        });
    }
}
