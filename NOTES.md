# ListFormatParser — Notes & Future Work

## Carl Barnes' Original Tool (List-Format-Parser)

Carl's Clarion tool is **bi-directional** — it both parses and generates FORMAT() strings.

### What it does (beyond our current addin)

**Parsing (v1 — implemented)**
- Reads an existing `FORMAT('...')` attribute and breaks it into individual column specs
- Displays width, alignment, indent, modifiers, header, picture per column

**Generation (v2 — not yet implemented)**

| Method | What it does |
|---|---|
| `GenFmt.SimpleGen()` | Builds a FORMAT string from configurable column parameters |
| `GenFmt.QueueGenFormat()` | Builds FORMAT string from a parsed QUEUE/FILE field list |
| `GenFmt.CopyFormatBtn()` | Copies generated FORMAT to clipboard with proper Clarion `|` line continuations |
| `GenFmt.CopyWindowAndListBtn()` | Generates entire `WINDOW + LIST + FORMAT` block, copies to clipboard |
| `GenFmt.CopyFieldsEqualBtn()` | Generates `Queue.Field =` assignment statements |

Carl's UI lets you:
- Edit column values (width, justification, header text, picture, modifiers)
- Reorder fields via drag-and-drop
- Auto-regenerate the FORMAT string on every change
- Preview the result in a live LIST control
- Save/load column configs to an INI file

### Potential v2 Scope for this Addin

1. **Write-back (easy):** Make the grid editable, add a "Copy FORMAT" button that rebuilds the
   FORMAT string from the grid rows and copies it to clipboard (or replaces in editor via `IDocument`).
   We already know how to replace document text from `FlattenCode`.

2. **Generate from QUEUE (hard):** Parse a `QUEUE` structure from the source, map fields to
   columns automatically. Requires parsing Clarion data structures — a much larger undertaking.

3. **Shortcut key:** No shortcut registered yet in `ListFormatParser.addin`. Could add e.g. `Ctrl+Shift+L`.

### Reference Files in List-Format-Parser/

| File | Contents |
|---|---|
| `CBCodeParse.clw` | `CBCodeParseClass` — string masking, attribute finding (ported to `ClarionCodeParser.cs`) |
| `ListForP-Main_Data.clw` | Data structures: `FormatQ`, `ColumnzQ`, `Format2QCls`, `GQFieldsQ` |
| `ListForP-Main_Wind.clw` | Main window + `GenFmt` procedure (generation logic) |
| `ListForP-Utility.clw` | Helper utilities |
| `ListForP-PreviewList.clw` | Live LIST preview window |
