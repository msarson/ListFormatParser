# Changelog

All notable changes to List Format Parser are documented here.

## [1.2.0] - 2026-03-04

### Added
- **Tabbed dialog** — Parse List Format now opens a resizable tabbed window
  - **Columns grid** (top pane) — same grid as before with hover modifier tooltips; Copy FORMAT button
  - **Explain tab** — per-column plain-English breakdown of width, alignment, header, picture, and modifier flags
  - **FORMAT Lines tab** — FORMAT one column per line (left) + `#FIELDS` field numbers (right) in a split pane; Copy FORMAT and Copy #FIELDS buttons
  - **FROM tab** — FROM entries one-per-line (left) + CASE output (right); only shown when `FROM('...')` is present
- **Parse FROM command** — right-click on any `SPIN`, `COMBO`, or drop-list with `FROM('...')` opens the dialog in FROM-only mode, showing a simple `# / Display / Value` grid instead of the columns grid; FROM tab is selected automatically

### Changed
- Dialog is now resizable with a draggable splitter between the grid and tabs
- Modifier detail text box removed from Columns tab — Explain tab supersedes it

## [1.1.0] - 2026-03-04

### Added
- **Clean List Format** (Edit → Format) — rewrites `FORMAT()` in-place, one column per line with aligned `&|` continuation markers
- **Format FROM Selections** (Edit → Format) — rewrites `FROM('...')` in-place, one entry per line with aligned `&|` continuation markers; only appears when caret is inside a string-form `FROM()` block
- **Copy FROM as CASE** (Edit menu + right-click) — copies a ready-to-use `CASE` statement to the clipboard, using `#value` alternates as `OF` targets; auto-detects the `USE()` variable for the `CASE` header
- **Copy FROM as CHOOSE** (Edit menu + right-click) — copies a `CHOOSE()` call to the clipboard using the display labels
- `HasFromStringCondition` — FROM commands are condition-gated and only appear when `FROM('...')` is present in the current block
- `!Choices:` / `!Values:` comment lines in CASE output are column-aligned so each value lines up under its display label

### Changed
- `FORMAT()` and `FROM()` continuation lines now have `&|` markers aligned at the same column across all lines

## [1.0.0] - 2026-03-01

### Added
- Initial release
- Parse LIST FORMAT() attribute into a readable column grid
- Hover modifier flags for plain-English descriptions
- Copy FORMAT button — regenerates a normalised FORMAT() string
- Available in CLW source editor and Embeditor (PWEE)
- Context menu only appears when caret is inside a FORMAT() block
- MIT licence


### Added
- **Clean List Format** (Edit → Format) — rewrites `FORMAT()` in-place, one column per line with aligned `&|` continuation markers
- **Format FROM Selections** (Edit → Format) — rewrites `FROM('...')` in-place, one entry per line with aligned `&|` continuation markers; only appears when caret is inside a string-form `FROM()` block
- **Copy FROM as CASE** (Edit menu + right-click) — copies a ready-to-use `CASE` statement to the clipboard, using `#value` alternates as `OF` targets; auto-detects the `USE()` variable for the `CASE` header
- **Copy FROM as CHOOSE** (Edit menu + right-click) — copies a `CHOOSE()` call to the clipboard using the display labels
- `HasFromStringCondition` — FROM commands are condition-gated and only appear when `FROM('...')` is present in the current block
- `!Choices:` / `!Values:` comment lines in CASE output are column-aligned so each value lines up under its display label

### Changed
- `FORMAT()` and `FROM()` continuation lines now have `&|` markers aligned at the same column across all lines

## [1.0.0] - 2026-03-01

### Added
- Initial release
- Parse LIST FORMAT() attribute into a readable column grid
- Hover modifier flags for plain-English descriptions
- Copy FORMAT button — regenerates a normalised FORMAT() string
- Available in CLW source editor and Embeditor (PWEE)
- Context menu only appears when caret is inside a FORMAT() block
- MIT licence
