'use strict';

/* ── Inline SVG icon map (dynamic-only icons) ─────────────── */
const ICONS = {
  sun: `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M6.34 17.66l-1.41 1.41M19.07 4.93l-1.41 1.41"/></svg>`,
  moon: `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 3a6 6 0 0 0 9 9 9 9 0 1 1-9-9Z"/></svg>`,
  menu: `<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><line x1="4" x2="20" y1="12" y2="12"/><line x1="4" x2="20" y1="6" y2="6"/><line x1="4" x2="20" y1="18" y2="18"/></svg>`,
  x: `<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M18 6 6 18"/><path d="m6 6 12 12"/></svg>`,
  check: `<svg xmlns="http://www.w3.org/2000/svg" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M20 6 9 17l-5-5"/></svg>`,
};

/* ── Page loader ──────────────────────────────────────────── */
function dismissLoader() {
  const loader = document.getElementById('page-loader');
  if (!loader) return;
  requestAnimationFrame(() => {
    loader.classList.add('hidden');
    document.body.classList.add('page-ready');
    loader.addEventListener('transitionend', () => loader.remove(), { once: true });
  });
}

/* ── Theme ───────────────────────────────────────────────── */
const THEME_KEY = 'chatapi-theme';

function applyTheme(theme) {
  document.documentElement.setAttribute('data-theme', theme);
  localStorage.setItem(THEME_KEY, theme);
}

function initTheme() {
  const saved = localStorage.getItem(THEME_KEY)
    || (window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark');
  applyTheme(saved);
}

/* ── Hamburger menu ──────────────────────────────────────── */
function initHamburger() {
  const btn  = document.getElementById('hamburger');
  const menu = document.getElementById('mobile-menu');
  if (!btn || !menu) return;

  function closeMenu() {
    menu.classList.remove('open');
    btn.classList.remove('open');
    btn.setAttribute('aria-expanded', 'false');
    document.body.style.overflow = '';
  }

  btn.addEventListener('click', () => {
    const open = menu.classList.toggle('open');
    btn.classList.toggle('open', open);
    btn.setAttribute('aria-expanded', String(open));
    document.body.style.overflow = open ? 'hidden' : '';
  });

  menu.querySelectorAll('a').forEach(a => a.addEventListener('click', closeMenu));

  document.addEventListener('click', e => {
    if (!menu.contains(e.target) && !btn.contains(e.target)) closeMenu();
  });
}

/* ── Active nav link ─────────────────────────────────────── */
function initNav() {
  const path = window.location.pathname;
  document.querySelectorAll('.nav-links a, .mobile-menu a').forEach(a => {
    const href = a.getAttribute('href');
    if (!href) return;
    const match = (href === '/' && (path === '/' || path === '/index.html'))
               || (href !== '/' && path.startsWith(href));
    a.classList.toggle('active', match);
  });
}

/* ── Docs tab navigation ─────────────────────────────────── */
function initDocsTabs() {
  const tabs    = document.querySelectorAll('.dtab');
  const panels  = document.querySelectorAll('.tab-panel');
  if (!tabs.length || !panels.length) return;

  const STORAGE_KEY = 'chatapi-docs-tab';

  function activateTab(tabId) {
    tabs.forEach(t => {
      const active = t.dataset.tab === tabId;
      t.classList.toggle('active', active);
      t.setAttribute('aria-selected', String(active));
    });
    panels.forEach(p => {
      p.classList.toggle('active', p.id === 'tab-' + tabId);
    });
    localStorage.setItem(STORAGE_KEY, tabId);

    // Scroll active tab into view in the tab bar
    const activeTab = document.querySelector(`.dtab[data-tab="${tabId}"]`);
    if (activeTab) {
      activeTab.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });
    }

    // Scroll to top of content area
    const content = document.querySelector('.docs-content');
    if (content) content.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  tabs.forEach(tab => {
    tab.addEventListener('click', () => activateTab(tab.dataset.tab));
  });

  // Restore last active tab or use URL hash
  const hash    = window.location.hash.replace('#', '');
  const saved   = localStorage.getItem(STORAGE_KEY);
  const validIds = Array.from(tabs).map(t => t.dataset.tab);
  const initial = validIds.includes(hash) ? hash
                : validIds.includes(saved) ? saved
                : validIds[0];
  activateTab(initial);
}

/* ── Endpoint accordion ──────────────────────────────────── */
function initEndpointAccordion() {
  document.querySelectorAll('.ep-toggle').forEach(toggle => {
    toggle.addEventListener('click', () => {
      const item = toggle.closest('.ep-item');
      if (!item) return;
      // Close siblings in the same list
      const list = item.closest('.ep-list');
      if (list) {
        list.querySelectorAll('.ep-item.open').forEach(open => {
          if (open !== item) open.classList.remove('open');
        });
      }
      item.classList.toggle('open');
    });
  });
}

/* ── Copy buttons ────────────────────────────────────────── */
function initCopyBtns() {
  document.querySelectorAll('.copy-btn').forEach(btn => {
    btn.addEventListener('click', async () => {
      const block = btn.closest('.code-block');
      const clone = block.cloneNode(true);
      clone.querySelectorAll('button').forEach(b => b.remove());
      const text = clone.innerText.trim();
      try {
        await navigator.clipboard.writeText(text);
        const orig = btn.innerHTML;
        btn.innerHTML = `${ICONS.check} Copied`;
        btn.style.color = 'var(--accent2)';
        setTimeout(() => { btn.innerHTML = orig; btn.style.color = ''; }, 2000);
      } catch {
        const range = document.createRange();
        range.selectNodeContents(block);
        window.getSelection()?.removeAllRanges();
        window.getSelection()?.addRange(range);
      }
    });
  });
}

/* ── Scroll-reveal ───────────────────────────────────────── */
function initReveal() {
  const els = document.querySelectorAll('.card, .endpoint-card, .stat, .tech-chip, .ws-demo');
  if (!els.length) return;

  const io = new IntersectionObserver(entries => {
    entries.forEach(e => {
      if (e.isIntersecting) {
        e.target.classList.add('reveal', 'visible');
        io.unobserve(e.target);
      }
    });
  }, { threshold: 0.08, rootMargin: '0px 0px -20px 0px' });

  els.forEach(el => { el.classList.add('reveal'); io.observe(el); });
}

/* ── API status badge ────────────────────────────────────── */
function initStatusBadge() {
  const el = document.getElementById('api-status');
  if (!el) return;
  const check = async () => {
    try {
      const r = await fetch('/health', { signal: AbortSignal.timeout(4000) });
      if (r.ok) { el.textContent = 'API Online'; el.className = 'badge badge-green'; }
      else throw 0;
    } catch {
      el.textContent = 'API Offline'; el.className = 'badge badge-orange';
    }
  };
  check();
  setInterval(check, 20000);
}

/* ── Stat counters ───────────────────────────────────────── */
function initCounters() {
  const els = document.querySelectorAll('[data-count]');
  if (!els.length) return;
  const io = new IntersectionObserver(entries => {
    entries.forEach(e => {
      if (!e.isIntersecting) return;
      io.unobserve(e.target);
      const el = e.target;
      const target = parseInt(el.dataset.count, 10);
      const suffix = el.dataset.suffix || '';
      const dur = 900, start = performance.now();
      const tick = now => {
        const p = Math.min((now - start) / dur, 1);
        el.textContent = Math.round((1 - Math.pow(1 - p, 3)) * target) + suffix;
        if (p < 1) requestAnimationFrame(tick);
      };
      requestAnimationFrame(tick);
    });
  }, { threshold: 0.5 });
  els.forEach(el => io.observe(el));
}

/* ── Typewriter ──────────────────────────────────────────── */
function initTypewriter() {
  const el = document.getElementById('typewriter');
  if (!el) return;
  const texts = ['Real-time Messaging', 'WebSocket Events', 'MongoDB Persistence', 'Redis Pub/Sub', 'JWT Authentication', 'File Uploads'];
  let i = 0, j = 0, del = false;
  const tick = () => {
    const txt = texts[i];
    el.textContent = del ? txt.slice(0, j--) : txt.slice(0, j++);
    if (!del && j > txt.length) { del = true; setTimeout(tick, 1400); return; }
    if (del && j < 0) { del = false; i = (i + 1) % texts.length; j = 0; }
    setTimeout(tick, del ? 38 : 72);
  };
  tick();
}

/* ── Lucide icon init ────────────────────────────────────── */
function initIcons() {
  if (typeof lucide !== 'undefined') lucide.createIcons();
}

/* ── Init ────────────────────────────────────────────────── */
document.addEventListener('DOMContentLoaded', () => {
  initTheme();
  dismissLoader();
  initHamburger();
  initNav();
  initDocsTabs();
  initEndpointAccordion();
  initCopyBtns();
  initReveal();
  initStatusBadge();
  initCounters();
  initTypewriter();
  initIcons();

  document.getElementById('theme-toggle')?.addEventListener('click', () => {
    const cur = document.documentElement.getAttribute('data-theme') || 'dark';
    applyTheme(cur === 'dark' ? 'light' : 'dark');
  });
});
