/* SGD • Tela de login — apenas comportamento de interface.
   A autenticação é feita pelo servidor (POST /login, Marco 3). */
(function () {
  'use strict';

  const form = document.getElementById('loginForm');
  if (!form) return;
  const inputUser = document.getElementById('usuario');
  const inputPass = document.getElementById('senha');
  const toggleBtn = document.getElementById('togglePassword');
  const rememberBox = document.getElementById('lembrar');
  const errorBox = document.getElementById('formError');
  const submitBtn = document.getElementById('btnEntrar');
  const submitText = submitBtn.querySelector('.btn__text');
  const STORAGE_KEY = 'sgd:usuario'; // guarda só o RE (nunca a senha)

  /* ---------- Modal: sobre o projeto ---------- */
  const aboutModal = document.getElementById('aboutModal');
  const openAbout = document.getElementById('openAbout');
  const closeAbout = document.getElementById('closeAbout');
  const aboutDialog = aboutModal.querySelector('.about-dialog');
  let previousFocus = null;
  function showAbout() {
    previousFocus = document.activeElement;
    aboutModal.classList.add('is-open');
    aboutModal.setAttribute('aria-hidden', 'false');
    document.body.classList.add('about-open');
    aboutDialog.focus(); closeAbout.focus();
  }
  function hideAbout() {
    aboutModal.classList.remove('is-open');
    aboutModal.setAttribute('aria-hidden', 'true');
    document.body.classList.remove('about-open');
    if (previousFocus) previousFocus.focus();
  }
  openAbout.addEventListener('click', showAbout);
  closeAbout.addEventListener('click', hideAbout);
  aboutModal.querySelectorAll('[data-close-about]').forEach((el) => el.addEventListener('click', hideAbout));
  document.addEventListener('keydown', (e) => { if (e.key === 'Escape' && aboutModal.classList.contains('is-open')) hideAbout(); });

  /* ---------- Mostrar / ocultar senha ---------- */
  toggleBtn.addEventListener('click', () => {
    const show = inputPass.type === 'password';
    inputPass.type = show ? 'text' : 'password';
    toggleBtn.setAttribute('aria-pressed', String(show));
    toggleBtn.setAttribute('aria-label', show ? 'Ocultar senha' : 'Mostrar senha');
    inputPass.focus();
  });

  /* ---------- Lembrar RE ---------- */
  try {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved && !inputUser.value) { inputUser.value = saved; rememberBox.checked = true; inputPass.focus(); }
  } catch (_) {}

  /* ---------- Validação ---------- */
  const setInvalid = (input, invalid) => {
    input.closest('.field').classList.toggle('is-invalid', invalid);
    input.setAttribute('aria-invalid', String(invalid));
  };
  const showError = (message) => { errorBox.textContent = message; errorBox.hidden = !message; };
  if (errorBox.textContent.trim()) { setInvalid(inputUser, true); setInvalid(inputPass, true); }
  [inputUser, inputPass].forEach((input) => input.addEventListener('input', () => { setInvalid(input, false); showError(''); }));

  form.addEventListener('submit', (event) => {
    showError('');
    const user = inputUser.value.trim();
    setInvalid(inputUser, !user);
    setInvalid(inputPass, !inputPass.value);
    if (!user) { event.preventDefault(); showError('Informe seu RE.'); inputUser.focus(); return; }
    if (!inputPass.value) { event.preventDefault(); showError('Informe sua senha.'); inputPass.focus(); return; }
    try { rememberBox.checked ? localStorage.setItem(STORAGE_KEY, user) : localStorage.removeItem(STORAGE_KEY); } catch (_) {}
    submitBtn.classList.add('is-loading');
    submitText.textContent = 'Entrando...';
    // O envio segue normalmente (POST /login); o botão volta ao normal quando a página recarrega.
  });

  /* ---------- Esqueci minha senha ---------- */
  const esqueci = document.querySelector('.options .link');
  if (esqueci) esqueci.addEventListener('click', (e) => { e.preventDefault(); showError('Para redefinir sua senha, fale com o administrador do sistema.'); });
})();
