using Microsoft.EntityFrameworkCore;
using System.Text;
using SgdBaldan.Models;

namespace SgdBaldan.Data;

public class SgdDbContext : DbContext
{
    public SgdDbContext(DbContextOptions<SgdDbContext> opcoes) : base(opcoes) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Dispositivo> Dispositivos => Set<Dispositivo>();
    public DbSet<Laudo> Laudos => Set<Laudo>();
    public DbSet<LaudoCorretiva> LaudosCorretiva => Set<LaudoCorretiva>();
    public DbSet<MedidaEspecificacao> Medidas => Set<MedidaEspecificacao>();
    public DbSet<ItemChecklist> Checklist => Set<ItemChecklist>();
    public DbSet<Ishikawa> Ishikawa => Set<Ishikawa>();
    public DbSet<AcaoCorretiva> AcoesCorretivas => Set<AcaoCorretiva>();
    public DbSet<Anexo> Anexos => Set<Anexo>();
    public DbSet<LaudoHistorico> LaudoHistorico => Set<LaudoHistorico>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SgdDbContext).Assembly);

        // Colunas em snake_case, como no DER (IdLaudo -> id_laudo).
        foreach (var entidade in modelBuilder.Model.GetEntityTypes())
            foreach (var propriedade in entidade.GetProperties())
                propriedade.SetColumnName(ParaSnakeCase(propriedade.Name));
    }

    private static string ParaSnakeCase(string nome)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < nome.Length; i++)
        {
            var c = nome[i];
            if (char.IsUpper(c) && i > 0) sb.Append('_');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
