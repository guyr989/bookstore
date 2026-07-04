# UI/UX Polish — Review & Plan

Experimental branch `feat/ui-ux-polish`. Vanilla CSS only, no new dependencies,
no behaviour changes. The goal is a calmer, more cohesive, more professional
look that is easier to scan and operate — not a redesign. Everything here is
reversible by dropping the branch.

## What was already good (kept)

- Visible keyboard focus rings, `:focus-visible` throughout.
- Responsive data tables that become labelled cards on small screens.
- Accessible toasts (`aria-live`, `role="alert"` on errors) and a real confirm
  dialog with focus management and Escape/backdrop dismissal.

## Findings → fixes

| # | Finding | Fix |
|---|---------|-----|
| 1 | Colours and spacing were hardcoded hex repeated across 9 files — no single source of truth. | A design-token system in `:root` (semantic colours, 4/8px spacing scale, radius, elevation). Every component references tokens. |
| 2 | Emoji used as UI icons (📚 ⟳ ✓ ! ℹ) — inconsistent across platforms, reads as unpolished. | Replaced with minimal inline SVG icons (book, refresh, check, alert, info). |
| 3 | Data tables were heavily boxed, prices/years left-aligned, no tabular figures. | Lighter row separators in a rounded surface card; numeric columns right-aligned with `tabular-nums`. |
| 4 | Buttons were ~32px tall — cramped, below the comfortable touch target. | 40px min height, consistent padding, clearer hover/disabled states. |
| 5 | Content sat flat on the page background. | Form and data grouped into subtle white "panel" surfaces. |
| 6 | No typography rhythm (line-height/scale). | Base line-height 1.6, modern system-font stack, consistent heading scale. |
| 7 | Entrance animations always ran. | Kept subtle, now guarded by `prefers-reduced-motion`; no new/flashy motion added. |

## Palette (calm, professional — no loud colours)

| Token | Value | Use |
|-------|-------|-----|
| `--primary` | `#1d4ed8` | Primary actions, links, focus ring |
| `--header-bg` | `#1e293b` | Top bar (dark slate) |
| `--danger` | `#dc2626` | Destructive actions |
| `--success` | `#15803d` | Success toasts |
| `--bg` / `--surface` | `#f5f7fa` / `#ffffff` | Page / cards |
| `--text` / `--text-muted` | `#1e293b` / `#64748b` | Body / secondary |

All foreground/background pairs meet WCAG AA (4.5:1) for text.

## Constraints honoured

- **Vanilla CSS only** — no Tailwind, no component library, no web fonts (system
  stack, so it works offline).
- **No logic changes** — the DOM contracts the unit tests rely on
  (`h1` text, nav labels, `.dialog`/`.backdrop`/`.buttons`, `.author-row input`)
  are unchanged; the frontend suite stays green.
