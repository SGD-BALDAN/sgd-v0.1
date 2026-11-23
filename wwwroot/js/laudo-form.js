/* SGD - Formulário de laudo (liberação, preventiva e corretiva; criação e edição).
   Mantém o layout das telas e envia os dados ao servidor (nada de alertas de "fluxo do Marco 6"). */
(() => {
  'use strict';
  const $ = (s, r = document) => r.querySelector(s);
  const $$ = (s, r = document) => [...r.querySelectorAll(s)];
  const num = v => { const t = String(v ?? '').trim(); return Number(t.includes(',') ? t.replace(/\./g, '').replace(',', '.') : t); };
  const fmt = v => v.toLocaleString('pt-BR', { maximumFractionDigits: 4 });

  function iniciar() {
    const form = $('#f');
    if (!form) return;
    const leitura = form.dataset.leitura === 'true';
    const estrutura = form.dataset.estrutura === 'true';

    // ---- Evita envio por Enter (o botão padrão poderia ser Excluir/Assumir etc.)
    form.addEventListener('keydown', e => {
      if (e.key === 'Enter' && e.target.matches('input:not([type=submit])')) e.preventDefault();
    });

    // ---- Nome dos campos: lista sem "buracos" (Medidas[0], Medidas[1]...) antes de enviar
    const grupos = [['#medidas', 'tr', 'Medidas'], ['#check', 'tr', 'Checklist'], ['#acts', '.action', 'Acoes']];
    const reindexar = () => grupos.forEach(([sel, item, prefixo]) => {
      const c = $(sel); if (!c) return;
      $$(`:scope > ${item}`, c).forEach((linha, i) => $$('[data-f]', linha).forEach(el => { el.name = `${prefixo}[${i}].${el.dataset.f}`; }));
    });

    // ---- Linhas novas a partir dos <template> renderizados pelo servidor
    const adicionar = (container, tplId) => {
      const tpl = document.getElementById(tplId);
      if (!container || !tpl) return null;
      container.insertAdjacentHTML('beforeend', tpl.innerHTML.replace(/__I__/g, container.children.length));
      const linha = container.lastElementChild;
      const n = $('[data-f=Numero]', linha);
      if (n) n.value = container.children.length;
      reindexar();
      return linha;
    };
    $('#addM')?.addEventListener('click', () => adicionar($('#medidas'), 'tplMedida'));
    $('#addC')?.addEventListener('click', () => adicionar($('#check'), 'tplChecklist'));
    $('#addA')?.addEventListener('click', () => adicionar($('#acts'), 'tplAcao'));

    form.addEventListener('click', e => {
      const remover = e.target.closest('.remover-linha');
      if (remover) { (remover.closest('tr') || remover.closest('.action'))?.remove(); reindexar(); }
    });

    // ---- Especificação "nominal ± tolerância" → mínimo/máximo; medição → resultado
    const calcularLinha = tr => {
      const espec = $('.medida-especificacao', tr), min = $('.medida-min', tr), max = $('.medida-max', tr);
      const m = espec.value.trim().replace('+/-', '±').replace(/,/g, '.').match(/^(-?\d+(?:\.\d+)?)\s*±\s*(\d+(?:\.\d+)?)$/);
      if (m) { min.value = fmt(Number(m[1]) - Number(m[2])); max.value = fmt(Number(m[1]) + Number(m[2])); }
      else { min.value = ''; max.value = ''; }
      const val = $('.medida-valor', tr), out = $('.medida-resultado', tr);
      let txt = '—', cls = 'pendente';
      if (m && val.value.trim() !== '') {
        const v = num(val.value), lo = Number(m[1]) - Number(m[2]), hi = Number(m[1]) + Number(m[2]);
        if (Number.isFinite(v)) { const ok = v >= lo - 1e-9 && v <= hi + 1e-9; txt = ok ? 'Aprovado' : 'Reprovado'; cls = ok ? 'aprovado' : 'reprovado'; }
      }
      out.textContent = txt;
      out.classList.remove('pendente', 'aprovado', 'reprovado'); out.classList.add(cls);
    };
    const medidas = $('#medidas');
    medidas?.addEventListener('input', e => { const tr = e.target.closest('tr'); if (tr && e.target.matches('.medida-especificacao, .medida-valor')) calcularLinha(tr); });
    $$('tr', medidas || document.createElement('tbody')).forEach(calcularLinha);

    // ---- Número do dispositivo existente → pré-seleciona produto e categoria
    const cod = $('#cod');
    cod?.addEventListener('input', () => {
      const opt = $$('#dispositivos-existentes option').find(o => o.value.toUpperCase() === cod.value.trim().toUpperCase());
      if (!opt) return;
      const p = $('#prod'), c = $('#categoria');
      if (p && !p.disabled) p.value = opt.dataset.produto || p.value;
      if (c && !c.disabled) c.value = opt.dataset.categoria || c.value;
    });

    // ---- Pré-visualização de arquivos (o envio acontece ao salvar)
    const file = $('#file'), prev = $('#prev');
    file?.addEventListener('change', () => {
      if (!prev) return;
      const f = file.files?.[0];
      prev.replaceChildren();
      if (!f) { prev.textContent = prev.dataset.vazio || 'Nenhum desenho importado.'; return; }
      if (f.type.startsWith('image/')) { const img = document.createElement('img'); img.alt = f.name; img.src = URL.createObjectURL(f); prev.appendChild(img); }
      else prev.textContent = f.name;
    });
    [['uploadOcorrencia', 'previewOcorrencia'], ['uploadTrabalho', 'previewTrabalho']].forEach(([inputId, alvoId]) => {
      const input = document.getElementById(inputId), alvo = document.getElementById(alvoId);
      input?.addEventListener('change', () => {
        if (!alvo) return;
        $$('.foto-nova', alvo).forEach(x => x.remove());
        [...(input.files || [])].forEach(f => {
          if (!f.type.startsWith('image/')) return;
          const img = document.createElement('img');
          img.className = 'foto-nova'; img.alt = f.name; img.title = f.name; img.src = URL.createObjectURL(f);
          img.style.cssText = 'max-width:120px;max-height:100px;object-fit:cover;border-radius:8px';
          alvo.appendChild(img);
        });
      });
    });

    // ---- Ishikawa 6M (corretiva): causas vêm do servidor e voltam em campos repetidos (CausasMetodo, ...)
    const fish = $('#fish');
    if (fish) {
      const dados = (() => { try { return JSON.parse(fish.dataset.causas || '{}'); } catch { return {}; } })();
      const editaFish = fish.dataset.edita === 'true';
      const categorias = [
        ['Método', '＋', 'sgd-ishikawa__category--1', 'top', 'metodo', 'CausasMetodo'],
        ['Máquina', '⚙', 'sgd-ishikawa__category--2', 'top', 'maquina', 'CausasMaquina'],
        ['Material', '◆', 'sgd-ishikawa__category--3', 'top', 'material', 'CausasMaterial'],
        ['Medição', '⌖', 'sgd-ishikawa__category--4', 'bottom', 'medicao', 'CausasMedicao'],
        ['Meio ambiente', '☼', 'sgd-ishikawa__category--5', 'bottom', 'meioAmbiente', 'CausasMeioAmbiente'],
        ['Mão de obra', '♙', 'sgd-ishikawa__category--6', 'bottom', 'maoDeObra', 'CausasMaoDeObra']
      ];
      const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
      const causa = (campo, valor) => `<div class="cause"><input name="${campo}" value="${esc(valor)}" placeholder="Possível causa..." maxlength="300"${editaFish ? '' : ' readonly'}>${editaFish ? '<button type="button" class="btn-icon remover-causa" aria-label="Remover causa">×</button>' : ''}</div>`;
      fish.innerHTML = categorias.map(([nome, icone, classe, pos, chave, campo], i) => {
        const lista = (dados[chave] && dados[chave].length) ? dados[chave] : [''];
        return `
        <section class="cat ${classe} cat--${pos}">
          <header class="sgd-ishikawa__category-header"><span class="sgd-ishikawa__category-icon">${icone}</span><h3>${nome}</h3>${editaFish ? `<button type="button" class="adicionar-causa" data-categoria="${i}" aria-label="Adicionar causa em ${nome}">+</button>` : ''}</header>
          <div class="sgd-ishikawa__causes" data-causes="${i}" data-campo="${campo}">${lista.map(v => causa(campo, v)).join('')}</div>
        </section>`;
      }).join('') + `
        <section class="cat cat--root"><span class="sgd-ishikawa__root-icon">!</span><h3>CAUSA RAIZ</h3><p>Defina a causa principal após analisar os fatores identificados.</p><textarea id="causaRaiz" name="CausaRaiz" placeholder="Causa raiz principal..."${editaFish ? '' : ' readonly'}>${esc(dados.causaRaiz)}</textarea><small class="sgd-ishikawa__root-counter">0 / 500</small></section>`;
      fish.addEventListener('click', e => {
        const add = e.target.closest('.adicionar-causa');
        if (add) { const area = $(`[data-causes="${add.dataset.categoria}"]`, fish); area.insertAdjacentHTML('beforeend', causa(area.dataset.campo, '')); }
        e.target.closest('.remover-causa')?.closest('.cause')?.remove();
      });
      const raiz = $('#causaRaiz', fish), cont = $('.sgd-ishikawa__root-counter', fish);
      if (raiz) { raiz.maxLength = 500; const atualiza = () => { cont.textContent = `${raiz.value.length} / 500`; }; raiz.addEventListener('input', atualiza); atualiza(); }
    }

    // ---- Botões com confirmação / motivo obrigatório (formaction)
    form.addEventListener('click', e => {
      const b = e.target.closest('button[data-confirmar], button[data-exige]');
      if (!b) return;
      if (b.dataset.exige) {
        const campo = document.getElementById(b.dataset.exige);
        if (campo && !campo.value.trim()) { e.preventDefault(); campo.focus(); alert('Informe o motivo da reprovação.'); return; }
      }
      if (b.dataset.confirmar && !confirm(b.dataset.confirmar)) e.preventDefault();
    });

    // ---- Envio: reindexa, evita clique duplo
    form.addEventListener('submit', e => {
      reindexar();
      // select desabilitado não é enviado: nada a fazer, o servidor só usa o que a pessoa pode alterar
      const botao = e.submitter;
      if (botao) setTimeout(() => $$('button[type=submit]', form).forEach(b => { b.disabled = true; }), 0);
    });
    window.addEventListener('pageshow', () => $$('button[type=submit]', form).forEach(b => { b.disabled = false; }));

    if (leitura) $$('.remover-linha, .adicionar-causa, .remover-causa', form).forEach(b => b.remove());
    reindexar();
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', iniciar); else iniciar();
})();
