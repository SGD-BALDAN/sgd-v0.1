using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SgdBaldan.Models;

namespace SgdBaldan.Data.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("usuarios");
        b.HasKey(x => x.IdUsuario);
        b.Property(x => x.Nome).HasMaxLength(150).IsRequired();
        b.Property(x => x.Re).HasMaxLength(30).IsRequired();
        b.Property(x => x.SenhaHash).HasMaxLength(255).IsRequired();
        b.Property(x => x.Perfil).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.DataCriacao).HasColumnType("datetime(6)");
        b.HasIndex(x => x.Re).IsUnique();
    }
}

public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> b)
    {
        b.ToTable("produtos");
        b.HasKey(x => x.IdProduto);
        b.Property(x => x.Nome).HasMaxLength(40).IsRequired();
        b.HasIndex(x => x.Nome).IsUnique();
    }
}

public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> b)
    {
        b.ToTable("categorias");
        b.HasKey(x => x.IdCategoria);
        b.Property(x => x.Nome).HasMaxLength(30).IsRequired();
        b.Property(x => x.Cor).HasColumnType("char(7)").IsRequired();
        b.HasIndex(x => x.Nome).IsUnique();
        b.HasIndex(x => x.Cor).IsUnique();
    }
}

public class DispositivoConfiguration : IEntityTypeConfiguration<Dispositivo>
{
    public void Configure(EntityTypeBuilder<Dispositivo> b)
    {
        b.ToTable("dispositivos");
        b.HasKey(x => x.IdDispositivo);
        b.Property(x => x.Codigo).HasMaxLength(50).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.DataCriacao).HasColumnType("datetime(6)");
        b.HasIndex(x => x.Codigo).IsUnique();
        b.HasOne(x => x.Produto).WithMany().HasForeignKey(x => x.IdProduto).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Categoria).WithMany().HasForeignKey(x => x.IdCategoria).OnDelete(DeleteBehavior.Restrict);
    }
}
