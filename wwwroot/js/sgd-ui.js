/* SGD • Auxiliares de interface (ícones, toast, utilitários de formulário).
   Sem dados e sem autenticação: isso agora é feito pelo servidor (ASP.NET Core). */
(function () {
  const P = {
    home:'M3 11l9-8 9 8M5 10v10h14V10', grid:'M4 5h16v6H4zM4 14h16v6H4z',
    history:'M3 12a9 9 0 1 0 3-6.7L3 8M3 3v5h5M12 7v5l3 2', chart:'M5 20V10M12 20V4M19 20v-7',
    gear:'M12 9a3 3 0 1 0 0 6 3 3 0 0 0 0-6zM19 12l2-1-2-4-2 1-2-1-1-2h-4l-1 2-2 1-2-1-2 4 2 1v2l-2 1 2 4 2-1 2 1 1 2h4l1-2 2-1 2 1 2-4-2-1z',
    bell:'M6 16v-5a6 6 0 0 1 12 0v5l2 2H4zM10 21h4', search:'M11 4a7 7 0 1 0 0 14 7 7 0 0 0 0-14zM21 21l-5-5',
    wrench:'M14 6a4 4 0 0 0 5 5l-9 9a2 2 0 0 1-3-3l9-9a4 4 0 0 0-2-2z', cal:'M4 6h16v14H4zM4 10h16M8 3v4M16 3v4',
    file:'M6 3h8l4 4v14H6zM14 3v4h4M9 14l2 2 4-4', plus:'M12 5v14M5 12h14', back:'M19 12H5M11 6l-6 6 6 6',
    x:'M6 6l12 12M18 6L6 18', save:'M5 4h12l3 3v13H5zM8 4v5h7V4M8 20v-6h8v6', up:'M12 16V5M7 9l5-5 5 5M5 20h14',
    clock:'M12 4a8 8 0 1 0 0 16 8 8 0 0 0 0-16zM12 8v4l3 2', chev:'M9 6l6 6-6 6',
    mic:'M12 3a3 3 0 0 0-3 3v5a3 3 0 0 0 6 0V6a3 3 0 0 0-3-3zM6 11a6 6 0 0 0 12 0M12 17v4',
    trash:'M4 7h16M9 7V4h6v3M6 7l1 13h10l1-13M10 11v6M14 11v6', out:'M10 4H5v16h5M15 8l4 4-4 4M19 12H9', warn:'M12 3l10 18H2zM12 10v5M12 18v.5', target:'M12 4a8 8 0 1 0 0 16 8 8 0 0 0 0-16zM12 9a3 3 0 1 0 0 6 3 3 0 0 0 0-6z'
  };
  const esc = (v) => String(v == null ? '' : v).replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const ic = (n) => `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="${P[n]}"/></svg>`;
  const toast = (msg, ms = 2800) => { const t = Object.assign(document.createElement('div'), { className: 'toast', textContent: msg, role: 'status' }); document.body.append(t); setTimeout(() => t.remove(), ms); };
  const simple = (f) => { const o = {}; [...f.elements].forEach((e) => { if (!e.name || e.closest('[data-rows]')) return; if (e.type === 'radio') { if (e.checked) o[e.name] = e.value; } else if (e.type !== 'file') o[e.name] = e.value; }); return o; };
  const fill = (f, o) => Object.entries(o || {}).forEach(([k, v]) => { const e = f.elements[k]; if (e && typeof v === 'string') { if (e.tagName === 'SELECT' && v && ![...e.options].some((o) => o.value === v)) e.add(new Option(v, v)); e.value = v; } });
  const aviso = (f, msg) => f.insertAdjacentHTML('afterbegin', `<div class="banner" role="status">${esc(msg)}</div>`);
  const bloquear = (f, msg) => { aviso(f, msg); f.querySelectorAll('input,select,textarea,button').forEach((e) => (e.disabled = true)); };
  window.SGD = { ic, esc, toast, simple, fill, aviso, bloquear };
  document.querySelectorAll('[data-ic]').forEach((e) => (e.innerHTML = ic(e.dataset.ic)));
})();
