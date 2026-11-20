using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SgdBaldan.Infrastructure;
using SgdBaldan.Data;
using SgdBaldan.Models;
using SgdBaldan.Models.Enums;
using SgdBaldan.ViewModels;

namespace SgdBaldan.Services;

public interface IDispositivoService
{
    string NormalizarCodigo(string? codigo);
    Task<DispositivosIndexViewModel> ListarAsync();
    Task<DispositivoDetalheViewModel?> ObterDetalheAsync(string codigo);
    Task<Dispositivo?> ObterPorCodigoAsync(string codigo);

    /// <summary>
    /// Devolve o dispositivo do código ou, se não existir, cria um Pendente (produto e categoria obrigatórios).
    /// NÃO salva: quem chama salva junto com o laudo, para não sobrar dispositivo sem laudo.
    /// </summary>
    Task<Resultado<Dispositivo>> ObterOuCriarPendenteAsync(string? codigo, int? idProduto, int? idCategoria);

    Task AtivarAsync(int idDispositivo);

    /// <summary>Remove o dispositivo se ele ainda está Pendente e ficou sem nenhum laudo.</summary>
    Task RemoverSePendenteSemLaudosAsync(int idDispositivo);
}

public class DispositivoService : IDispositivoService
{
    private static readonly Regex CodigoValido = new(@"^[A-Z0-9][A-Z0-9._/-]{0,49}$", RegexOptions.Compiled);

    private readonly SgdDbContext _db;

    public DispositivoService(SgdDbContext db) => _db = db;

    public string NormalizarCodigo(string? codigo) =>
        Regex.Replace(codigo ?? string.Empty, @"\s+", string.Empty).ToUpperInvariant();

    public async Task<Dispositivo?> ObterPorCodigoAsync(string codigo)
    {
        codigo = NormalizarCodigo(codigo);
        return await _db.Dispositivos.Include(d => d.Produto).Include(d => d.Categoria)
            .FirstOrDefaultAsync(d => d.Codigo == codigo);
    }

    public async Task<DispositivosIndexViewModel> ListarAsync()
    {
        var dispositivos = await _db.Dispositivos.AsNoTracking()
            .Include(d => d.Produto).Include(d => d.Categoria)
            .OrderBy(d => d.Codigo).ToListAsync();
        var laudos = await _db.Laudos.AsNoTracking()
            .Select(l => new { l.IdLaudo, l.IdDispositivo, l.Tipo, l.Status, l.DataAtualizacao })
            .ToListAsync();
        var aprovacoes = await _db.LaudoHistorico.AsNoTracking()
            .Where(h => h.Status == StatusLaudo.Aprovado)
            .Select(h => new { h.IdLaudo, h.DataHora })
            .ToListAsync();
        var ultimaAprovacao = aprovacoes.GroupBy(a => a.IdLaudo).ToDictionary(g => g.Key, g => g.Max(x => x.DataHora));

        var itens = new List<DispositivoListaItemViewModel>();
        foreach (var d in dispositivos)
        {
            var ls = laudos.Where(l => l.IdDispositivo == d.IdDispositivo).OrderByDescending(l => l.DataAtualizacao).ToList();
            var aprovados = ls.Where(l => l.Status == StatusLaudo.Aprovado).ToList();
            DateTime? ultima = aprovados.Count == 0 ? null
                : aprovados.Where(l => ultimaAprovacao.ContainsKey(l.IdLaudo)).Select(l => (DateTime?)ultimaAprovacao[l.IdLaudo]).Max();
            int? Ultimo(TipoLaudo t) => ls.Where(l => l.Tipo == t).Select(l => (int?)l.IdLaudo).FirstOrDefault();

            itens.Add(new DispositivoListaItemViewModel
            {
                Codigo = d.Codigo,
                Produto = d.Produto.Nome,
                Categoria = d.Categoria.Nome,
                CategoriaCor = d.Categoria.Cor,
                Status = d.Status,
                UltimaAtualizacao = ultima,
                StatusMaisRecente = ls.Count == 0 ? null : ls[0].Status,
                AprovadosLiberacao = aprovados.Count(l => l.Tipo == TipoLaudo.Liberacao),
                AprovadosPreventiva = aprovados.Count(l => l.Tipo == TipoLaudo.Preventiva),
                AprovadosCorretiva = aprovados.Count(l => l.Tipo == TipoLaudo.Corretiva),
                UltimoLiberacao = Ultimo(TipoLaudo.Liberacao),
                UltimoPreventiva = Ultimo(TipoLaudo.Preventiva),
                UltimoCorretiva = Ultimo(TipoLaudo.Corretiva)
            });
        }

        var categorias = await _db.Categorias.AsNoTracking().Where(c => c.Ativo).OrderBy(c => c.Nome).ToListAsync();
        return new DispositivosIndexViewModel
        {
            Itens = itens.OrderByDescending(i => i.UltimaAtualizacao ?? DateTime.MinValue).ThenBy(i => i.Codigo).ToList(),
            Categorias = categorias.Select(c => new CategoriaOpcaoViewModel { Nome = c.Nome, Cor = c.Cor }).ToList()
        };
    }

    public async Task<DispositivoDetalheViewModel?> ObterDetalheAsync(string codigo)
    {
        var dispositivo = await ObterPorCodigoAsync(codigo);
        if (dispositivo == null) return null;

        var laudos = await _db.Laudos.AsNoTracking()
            .Where(l => l.IdDispositivo == dispositivo.IdDispositivo)
            .Include(l => l.ResponsavelProjeto).Include(l => l.Conferente)
            .Include(l => l.Historico).ThenInclude(h => h.Usuario)
            .OrderByDescending(l => l.DataAtualizacao)
            .ToListAsync();

        LaudoHistorico? Aprovacao(Laudo l) =>
            l.Historico.Where(h => h.Status == StatusLaudo.Aprovado).OrderByDescending(h => h.DataHora).FirstOrDefault();

        var aprovados = laudos.Where(l => l.Status == StatusLaudo.Aprovado).ToList();
        var itens = aprovados.Select(l =>
        {
            var ap = Aprovacao(l);
            return new LaudoAprovadoItemViewModel
            {
                IdLaudo = l.IdLaudo,
                Tipo = l.Tipo,
                OrdemServico = l.OrdemServico,
                Responsavel = (l.Conferente ?? l.ResponsavelProjeto)?.Nome,
                AprovadoPor = ap?.Usuario.Nome,
                AprovadoEm = ap?.DataHora
            };
        }).OrderByDescending(i => i.AprovadoEm).ToList();

        var modelo = new DispositivoDetalheViewModel
        {
            IdDispositivo = dispositivo.IdDispositivo,
            Codigo = dispositivo.Codigo,
            Produto = dispositivo.Produto.Nome,
            Categoria = dispositivo.Categoria.Nome,
            CategoriaCor = dispositivo.Categoria.Cor,
            Status = dispositivo.Status,
            UltimaAtualizacao = itens.Count == 0 ? null : itens.Max(i => i.AprovadoEm),
            StatusMaisRecente = laudos.Count == 0 ? null : laudos[0].Status,
            TemLiberacao = laudos.Any(l => l.Tipo == TipoLaudo.Liberacao),
            Aprovados = itens
        };

        var vigente = aprovados.FirstOrDefault(l => l.Tipo == TipoLaudo.Liberacao);
        if (vigente != null)
        {
            var completo = await _db.Laudos.AsNoTracking()
                .Include(l => l.Medidas).Include(l => l.Checklist)
                .FirstAsync(l => l.IdLaudo == vigente.IdLaudo);
            var ap = Aprovacao(vigente);
            modelo.LiberacaoVigente = new LiberacaoVigenteViewModel
            {
                IdLaudo = vigente.IdLaudo,
                DataInicio = vigente.DataInicio,
                OrdemServico = vigente.OrdemServico,
                ResponsavelProjeto = vigente.ResponsavelProjeto?.Nome,
                Conferente = vigente.Conferente?.Nome,
                AprovadoPor = ap?.Usuario.Nome,
                AprovadoEm = ap?.DataHora,
                Medidas = completo.Medidas.OrderBy(m => m.Numero).Select(m => new MedidaLeituraViewModel
                {
                    Numero = m.Numero,
                    Descricao = m.Descricao?.Rotulo(),
                    Especificacao = m.Especificacao,
                    Minimo = m.LimiteMin,
                    Maximo = m.LimiteMax,
                    Metodo = m.MetodoInstrumento?.Rotulo(),
                    Medicao = m.Medicao,
                    Resultado = m.Resultado.Rotulo()
                }).ToList(),
                Checklist = completo.Checklist.OrderBy(c => c.Numero).Select(c => new ChecklistLeituraViewModel
                {
                    Numero = c.Numero,
                    Descricao = c.Descricao,
                    Resultado = c.Resultado.Rotulo()
                }).ToList()
            };
        }
        return modelo;
    }

    public async Task<Resultado<Dispositivo>> ObterOuCriarPendenteAsync(string? codigo, int? idProduto, int? idCategoria)
    {
        var normalizado = NormalizarCodigo(codigo);
        if (normalizado.Length == 0) return Resultado<Dispositivo>.Falha("Informe o número do dispositivo.");
        if (!CodigoValido.IsMatch(normalizado))
            return Resultado<Dispositivo>.Falha("Número do dispositivo inválido. Use letras, números, ponto, hífen, barra ou sublinhado (até 50 caracteres).");

        var existente = await ObterPorCodigoAsync(normalizado);
        if (existente != null)
        {
            if (existente.Status == StatusDispositivo.Inativo)
                return Resultado<Dispositivo>.Falha("Este dispositivo está inativo. Procure a Gestão.");
            return Resultado<Dispositivo>.Ok(existente);
        }

        if (idProduto is null || idCategoria is null)
            return Resultado<Dispositivo>.Falha("Dispositivo novo: selecione o produto e a categoria.");
        var produto = await _db.Produtos.FirstOrDefaultAsync(p => p.IdProduto == idProduto && p.Ativo);
        var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.IdCategoria == idCategoria && c.Ativo);
        if (produto == null) return Resultado<Dispositivo>.Falha("Produto inválido ou desativado.");
        if (categoria == null) return Resultado<Dispositivo>.Falha("Categoria inválida ou desativada.");

        var novo = new Dispositivo
        {
            Codigo = normalizado,
            Produto = produto,
            Categoria = categoria,
            Status = StatusDispositivo.Pendente
        };
        _db.Dispositivos.Add(novo);
        return Resultado<Dispositivo>.Ok(novo);
    }

    public async Task AtivarAsync(int idDispositivo)
    {
        var d = await _db.Dispositivos.FindAsync(idDispositivo);
        if (d == null || d.Status != StatusDispositivo.Pendente) return;
        d.Ativar();
        await _db.SaveChangesAsync();
    }

    public async Task RemoverSePendenteSemLaudosAsync(int idDispositivo)
    {
        var d = await _db.Dispositivos.FirstOrDefaultAsync(x => x.IdDispositivo == idDispositivo);
        if (d == null || d.Status != StatusDispositivo.Pendente) return;
        if (await _db.Laudos.AnyAsync(l => l.IdDispositivo == idDispositivo)) return;
        _db.Dispositivos.Remove(d);
        await _db.SaveChangesAsync();
    }
}
