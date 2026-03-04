# Changelog

All notable changes to List Format Parser are documented here.

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
