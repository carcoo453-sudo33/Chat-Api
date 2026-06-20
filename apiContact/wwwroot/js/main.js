'use strict';

/* ── Inline SVG icon map (dynamic-only icons) ─────────────── */
const ICONS = {
  sun: `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M6.34 17.66l-1.41 1.41M19.07 4.93l-1.41 1.41"/></svg>`,
  moon: `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 3a6 6 0 0 0 9 9 9 9 0 1 1-9-9Z"/></svg>`,
  menu: `<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><line x1="4" x2="20" y1="12" y2="12"/><line x1="4" x2="20" y1="6" y2="6"/><line x1="4" x2="20" y1="18" y2="18"/></svg>`,
  x: `<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M18 6 6 18"/><path d="m6 6 12 12"/></svg>`,
  'chevron-left': `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m15 18-6-6 6-6"/></svg>`,
  'chevron-right': `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m9 18 6-6-6-6"/></svg>`,
  check: `<svg xmlns="http://www.w3.org/2000/svg" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M20 6 9 17l-5-5"/></svg>`,
};

/* ── Page loader ──────────────────────────────────────────── */
function dismissLoader() {
  const loader = document.getElementById('page-loader');
  if (!loader) return;
  // Slight delay so the loader is visible for at least one frame
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
  // Icons switch via CSS [data-theme] selectors — no JS needed
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

  document.addEventListener('click', (e) => {
    if (!menu.contains(e.target) && !btn.contains(e.target)) closeMenu();
  });
}

/* ── Docs sidebar collapse ───────────────────────────────── */
function initSidebar() {
  const layout  = document.querySelector('.docs-layout');
  const sidebar = document.querySelector('.docs-sidebar');
  const toggle  = document.getElementById('sidebar-toggle');
  if (!layout || !sidebar || !toggle) return;

  const COLLAPSED_KEY = 'chatapi-sidebar';

  function setSidebar(collapsed) {
    layout.classList.toggle('sidebar-collapsed', collapsed);
    sidebar.classList.remove('mobile-open');
    toggle.title = collapsed ? 'Show sidebar' : 'Hide sidebar';
    localStorage.setItem(COLLAPSED_KEY, collapsed ? '1' : '0');
  }

  function isMobile() { return window.innerWidth <= 768; }

  toggle.addEventListener('click', () => {
    if (isMobile()) {
      const open = sidebar.classList.toggle('mobile-open');
      toggle.classList.toggle('mobile-sidebar-open', open);
    } else {
      setSidebar(!layout.classList.contains('sidebar-collapsed'));
    }
  });

  if (!isMobile()) {
    setSidebar(localStorage.getItem(COLLAPSED_KEY) === '1');
  }

  window.addEventListener('resize', () => {
    if (!isMobile()) {
      sidebar.classList.remove('mobile-open');
      toggle.classList.remove('mobile-sidebar-open');
    }
  }, { passive: true });
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
        setTimeout(() => {
          btn.innerHTML = orig;
          btn.style.color = '';
        }, 2000);
      } catch {
        // fallback: select text
        const range = document.createRange();
        range.selectNodeContents(block);
        window.getSelection()?.removeAllRanges();
        window.getSelection()?.addRange(range);
      }
    });
  });
}

/* ── Scroll-reveal (IntersectionObserver) ────────────────── */
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
      if (r.ok) {
        el.textContent = 'API Online';
        el.className = 'badge badge-green';
      } else throw 0;
    } catch {
      el.textContent = 'API Offline';
      el.className = 'badge badge-orange';
    }
  };

  check();
  setInterval(check, 20000);
}

/* ── Stat counters (requestAnimationFrame) ───────────────── */
function initCounters() {
  const els = document.querySelectorAll('[data-count]');
  if (!els.length) return;

  const io = new IntersectionObserver(entries => {
    entries.forEach(e => {
      if (!e.isIntersecting) return;
      io.unobserve(e.target);
      const el     = e.target;
      const target = parseInt(el.dataset.count, 10);
      const suffix = el.dataset.suffix || '';
      const dur    = 900; // ms
      const start  = performance.now();

      const tick = (now) => {
        const elapsed = now - start;
        const progress = Math.min(elapsed / dur, 1);
        // ease-out cubic
        const eased = 1 - Math.pow(1 - progress, 3);
        el.textContent = Math.round(eased * target) + suffix;
        if (progress < 1) requestAnimationFrame(tick);
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

/* ── Docs sidebar scroll-spy ─────────────────────────────── */
function initScrollSpy() {
  const sections = document.querySelectorAll('h2[id]');
  const links    = document.querySelectorAll('.docs-sidebar a[href^="#"]');
  if (!sections.length || !links.length) return;

  let ticking = false;
  const onScroll = () => {
    if (ticking) return;
    ticking = true;
    requestAnimationFrame(() => {
      let cur = '';
      sections.forEach(s => {
        if (window.scrollY + 120 >= s.offsetTop) cur = s.id;
      });
      links.forEach(a => a.classList.toggle('active', a.getAttribute('href') === '#' + cur));
      ticking = false;
    });
  };
  window.addEventListener('scroll', onScroll, { passive: true });
  onScroll();
}

/* ── Lucide icon init (static page icons) ────────────────── */
function initIcons() {
  if (typeof lucide !== 'undefined') {
    lucide.createIcons();
  }
}

/* ── Init ────────────────────────────────────────────────── */
document.addEventListener('DOMContentLoaded', () => {
  // Apply theme first (prevents flash)
  initTheme();

  // Dismiss page loader
  dismissLoader();

  // UI
  initHamburger();
  initSidebar();
  initNav();
  initCopyBtns();
  initReveal();
  initStatusBadge();
  initCounters();
  initTypewriter();
  initScrollSpy();

  // Render Lucide icons
  initIcons();

  // Theme toggle
  document.getElementById('theme-toggle')?.addEventListener('click', () => {
    const cur = document.documentElement.getAttribute('data-theme') || 'dark';
    applyTheme(cur === 'dark' ? 'light' : 'dark');
  });
});
