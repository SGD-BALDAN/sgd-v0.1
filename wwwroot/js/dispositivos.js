/* SGD • Lista de dispositivos: busca e filtros no navegador (as linhas já vêm do servidor). */
(function () {
  'use strict';
  const q = document.getElementById('q');
  const fCategoria = document.getElementById('fCategoria');
  const fStatus = document.getElementById('fStatus');
  const cnt = document.getElementById('cnt');
  const vazio = document.getElementById('semResultado');
  const linhas = Array.from(document.querySelectorAll('#rows tr[data-codigo]'));

  q.value = new URLSearchParams(location.search).get('q') || '';

  function filtrar() {
    const t = q.value.trim().toLowerCase();
    let n = 0;
    linhas.forEach((tr) => {
      const ok = (!fCategoria.value || tr.dataset.categoria === fCategoria.value) &&
                 (!fStatus.value || tr.dataset.status === fStatus.value) &&
                 tr.dataset.busca.includes(t);
      tr.hidden = !ok;
      if (ok) n += 1;
    });
    vazio.hidden = n > 0;
    cnt.textContent = 'Mostrando ' + n + ' de ' + linhas.length + ' dispositivos';
  }

  q.addEventListener('input', filtrar);
  fCategoria.addEventListener('change', filtrar);
  fStatus.addEventListener('change', filtrar);
  filtrar();

  const abrir = (tr) => { if (tr) location.href = tr.dataset.href; };
  document.getElementById('rows').addEventListener('click', (e) => {
    if (e.target.closest('a')) return;
    abrir(e.target.closest('tr[data-codigo]'));
  });
  document.getElementById('rows').addEventListener('keydown', (e) => {
    if (e.key === 'Enter' && !e.target.closest('a')) abrir(e.target.closest('tr[data-codigo]'));
  });
})();
