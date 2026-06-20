---
name: Frontend icon strategy
description: How icons are handled in the Chat API static frontend (wwwroot)
---

# Frontend Icon Strategy

## Static page icons
Use Lucide CDN loaded with `defer` at end of body:
```html
<script src="https://unpkg.com/lucide@latest/dist/umd/lucide.min.js" defer></script>
```
Declare icons with `<i data-lucide="icon-name" class="icon-md"></i>`. Call `lucide.createIcons()` once in `initIcons()` on DOMContentLoaded.

Both deferred scripts load in DOM order so Lucide is always available before main.js runs.

## Dynamic icons (theme toggle, hamburger, sidebar)
Avoid re-calling `lucide.createIcons()` for toggled icons. Instead:
- **Theme toggle**: Two `<span>` elements (`.icon-sun` / `.icon-moon`) toggled via CSS `[data-theme="dark/light"]` selector — zero JS needed.
- **Hamburger**: `.hamburger.open .icon-open { display:none }` CSS pattern; JS only toggles `.open` class.
- **Sidebar**: `.sidebar-collapsed .st-hide / .st-show` CSS classes; mobile state uses `.st-mobile`.

## Copy button feedback
Use inline SVG check string from `ICONS.check` map + "Copied" text; reset after 2s timeout.

**Why:** Calling `lucide.createIcons()` repeatedly is expensive and causes layout flicker. CSS-based state is instant and zero-cost.
