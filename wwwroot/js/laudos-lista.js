/* SGD - Lista "Meus laudos": busca local nas linhas já renderizadas pelo servidor. */
(() => {
  'use strict';
  function iniciar() {
    const q = document.getElementById('q'), rows = document.getElementById('rows');
    if (!q || !rows) return;
    const linhas = [...rows.querySelectorAll('tr[data-busca]')];
    const vazio = document.getElementById('vazio'), n = document.getElementById('n');
    q.addEventListener('input', () => {
      const t = q.value.trim().toLowerCase();
      let visiveis = 0;
      linhas.forEach(tr => { const ok = !t || tr.dataset.busca.includes(t); tr.hidden = !ok; if (ok) visiveis++; });
      if (n) n.textContent = visiveis;
      if (vazio) vazio.hidden = visiveis > 0;
    });
  }
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', iniciar); else iniciar();
})();
