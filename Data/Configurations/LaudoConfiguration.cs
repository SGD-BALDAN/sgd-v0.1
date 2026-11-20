using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SgdBaldan.Models;

namespace SgdBaldan.Data.Configurations;

public class LaudoConfiguration : IEntityTypeConfiguration<Laudo>
{
    public void Configure(EntityTypeBuilder<Laudo> b)
    {
        b.ToTable("laudos");
        b.HasKey(x => x.IdLaudo);
        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Modo).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(x => x.DataInicio).HasColumnType("date");
        b.Property(x => x.OrdemServico).HasMaxLength(30).IsRequired();
        b.Property(x => x.DataCriacao).HasColumnType("datetime(6)");
        b.Property(x => x.DataAtualizacao).HasColumnType("datetime(6)");
        b.Property(x => x.DataEnvio).HasColumnType("datetime(6)");
        b.Property(x => x.CustoMaterial).HasColumnType("decimal(10,2)");
        b.Property(x => x.CustoMaoObra).HasColumnType("decimal(10,2)");

        b.HasOne(x => x.Dispositivo).WithMany(d => d.Laudos).HasForeignKey(x => x.IdDispositivo).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ResponsavelProjeto).WithMany().HasForeignKey(x => x.IdResponsavelProjeto).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Conferente).WithMany().HasForeignKey(x => x.IdConferente).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.IdDispositivo, x.Tipo });
        b.HasIndex(x => x.Status);

        // Regra: no máximo 1 laudo de Liberação por dispositivo.
        // Coluna gerada: vale id_dispositivo só para Liberação (nos demais é NULL, e NULLs não colidem no UNIQUE).
        b.Property<int?>("ChaveLiberacaoUnica")
            .HasComputedColumnSql("IF(`tipo` = 'Liberacao', `id_dispositivo`, NULL)", stored: true);
        b.HasIndex("ChaveLiberacaoUnica").IsUnique();
    }
}

public class LaudoCorretivaConfiguration : IEntityTypeConfiguration<LaudoCorretiva>
{
    public void Configure(EntityTypeBuilder<LaudoCorretiva> b)
    {
        b.ToTable("laudo_corretiva");
        b.HasKey(x => x.IdLaudo);
        b.Property(x => x.IdLaudo).ValueGeneratedNever();
        b.HasOne(x => x.Laudo).WithOne(l => l.Corretiva).HasForeignKey<LaudoCorretiva>(x => x.IdLaudo).OnDelete(DeleteBehavior.Cascade);
        b.Property(x => x.DescricaoFalha).HasMaxLength(1500);
        b.Property(x => x.ComponenteAfetado).HasMaxLength(150);
        b.Property(x => x.TipoFalha).HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.CausasSugeridas).HasMaxLength(1500);
        b.Property(x => x.RelatoIa).HasColumnType("text");
        b.Property(x => x.ResumoIa).HasColumnType("text");
        b.Property(x => x.OutrasInformacoes).HasMaxLength(1500);
    }
}

public class MedidaEspecificacaoConfiguration : IEntityTypeConfiguration<MedidaEspecificacao>
{
    public void Configure(EntityTypeBuilder<MedidaEspecificacao> b)
    {
        b.ToTable("medidas_especificacoes");
        b.HasKey(x => x.IdMedida);
        b.Property(x => x.Descricao).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Especificacao).HasMaxLength(30);
        b.Property(x => x.LimiteMin).HasColumnType("decimal(10,4)");
        b.Property(x => x.LimiteMax).HasColumnType("decimal(10,4)");
        b.Property(x => x.MetodoInstrumento).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Medicao).HasColumnType("decimal(10,4)");
        b.Property(x => x.Resultado).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.HasOne(x => x.Laudo).WithMany(l => l.Medidas).HasForeignKey(x => x.IdLaudo).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.IdLaudo, x.Numero });
    }
}

public class ItemChecklistConfiguration : IEntityTypeConfiguration<ItemChecklist>
{
    public void Configure(EntityTypeBuilder<ItemChecklist> b)
    {
        b.ToTable("checklist");
        b.HasKey(x => x.IdChecklist);
        b.Property(x => x.Descricao).HasMaxLength(255).IsRequired();
        b.Property(x => x.Resultado).HasConversion<string>().HasMaxLength(10).IsRequired();
        b.HasOne(x => x.Laudo).WithMany(l => l.Checklist).HasForeignKey(x => x.IdLaudo).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.IdLaudo, x.Numero });
    }
}

public class IshikawaConfiguration : IEntityTypeConfiguration<Ishikawa>
{
    public void Configure(EntityTypeBuilder<Ishikawa> b)
    {
        b.ToTable("ishikawa");
        b.HasKey(x => x.IdLaudo);
        b.Property(x => x.IdLaudo).ValueGeneratedNever();
        b.HasOne(x => x.LaudoCorretiva).WithOne(c => c.Ishikawa).HasForeignKey<Ishikawa>(x => x.IdLaudo).OnDelete(DeleteBehavior.Cascade);
        b.Property(x => x.Metodo).HasMaxLength(1500);
        b.Property(x => x.Maquina).HasMaxLength(1500);
        b.Property(x => x.Material).HasMaxLength(1500);
        b.Property(x => x.Medicao).HasMaxLength(1500);
        b.Property(x => x.MeioAmbiente).HasMaxLength(1500);
        b.Property(x => x.MaoDeObra).HasMaxLength(1500);
        b.Property(x => x.CausaRaiz).HasMaxLength(500);
    }
}

public class AcaoCorretivaConfiguration : IEntityTypeConfiguration<AcaoCorretiva>
{
    public void Configure(EntityTypeBuilder<AcaoCorretiva> b)
    {
        b.ToTable("acao_corretiva");
        b.HasKey(x => x.IdAcao);
        b.Property(x => x.Oque).HasMaxLength(1000);
        b.Property(x => x.Como).HasMaxLength(1000);
        b.HasOne(x => x.LaudoCorretiva).WithMany(c => c.Acoes).HasForeignKey(x => x.IdLaudo).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.IdLaudo, x.Numero });
    }
}

public class AnexoConfiguration : IEntityTypeConfiguration<Anexo>
{
    public void Configure(EntityTypeBuilder<Anexo> b)
    {
        b.ToTable("anexos");
        b.HasKey(x => x.IdAnexo);
        b.Property(x => x.Categoria).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(x => x.NomeOriginal).HasMaxLength(255).IsRequired();
        b.Property(x => x.TipoMime).HasMaxLength(100).IsRequired();
        b.Property(x => x.CaminhoArquivo).HasMaxLength(500).IsRequired();
        b.Property(x => x.DataEnvio).HasColumnType("datetime(6)");
        b.HasOne(x => x.Laudo).WithMany(l => l.Anexos).HasForeignKey(x => x.IdLaudo).OnDelete(DeleteBehavior.Cascade);
    }
}

public class LaudoHistoricoConfiguration : IEntityTypeConfiguration<LaudoHistorico>
{
    public void Configure(EntityTypeBuilder<LaudoHistorico> b)
    {
        b.ToTable("laudo_historico");
        b.HasKey(x => x.IdHistorico);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(x => x.DataHora).HasColumnType("datetime(6)");
        b.Property(x => x.Motivo).HasMaxLength(500);
        b.HasOne(x => x.Laudo).WithMany(l => l.Historico).HasForeignKey(x => x.IdLaudo).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.IdLaudo, x.DataHora });
    }
}
