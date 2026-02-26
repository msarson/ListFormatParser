# Clarion LIST FORMAT() Attribute — Complete Modifier Reference

Source: Clarion 11.1 help files (`C:\Clarion\Clarion11.1\bin\decoded\format__set_list_or_combo_layout_.htm` etc.)

---

## Column Spec Anatomy

Each column (or group) in a FORMAT string follows this structure:

```
width[align][(indent)]|[modifiers][~header~[align[(indent)]][@picture@]
```

Multiple columns are concatenated directly — there is no separator between them.
Groups are wrapped in `[ ]`.

---

## Width and Alignment

| Element | Meaning | PROPLIST equivalent |
|---|---|---|
| `width` | Column width in dialog units (integer) | `PROPLIST:Width` |
| `L` | Left-justify data | `PROPLIST:Left` |
| `R` | Right-justify data | `PROPLIST:Right` |
| `C` | Centre-justify data | `PROPLIST:Center` |
| `D` | Decimal-justify data | `PROPLIST:Decimal` |
| `(indent)` | Justification indent (dialog units). With L = left margin; R/D = right margin; C = offset from centre (negative = left) | `PROPLIST:LeftOffset` / `RightOffset` / `CenterOffset` / `DecimalOffset` |

---

## Header

```
~header text~[align[(indent)]]
```

- Header text is placed between tildes.
- Optional alignment (`L` `R` `C` `D`) and indent override the column's own alignment.
- PROPLIST: `PROPLIST:Header`, `HeaderLeft`, `HeaderRight`, `HeaderCenter`, `HeaderDecimal`

---

## Picture

```
@picture@
```

- Standard Clarion picture token (e.g. `@N12`, `@d17`, `@n4.2~Kr~`).
- Trailing `@` is required so the parser can handle pictures that contain `~tilde~` sequences.
- PROPLIST: `PROPLIST:Picture`

---

## Visual Modifiers

| Modifier | Syntax | Meaning | PROPLIST |
|---|---|---|---|
| `_` | `_` | Underline the column | `PROPLIST:Underline` |
| `\|` | `\|` | Right border (vertical line) on this column | `PROPLIST:RightBorder` |
| `/` | `/` | Line break — next field starts on new line (**groups only**) | — |

---

## Layout / Behaviour Modifiers

| Modifier | Syntax | Meaning | PROPLIST | Notes |
|---|---|---|---|---|
| `F` | `F` | Fixed column — stays visible during horizontal scroll (`HSCROLL`) | `PROPLIST:Fixed` | Must be at start of format; ignored inside a group; not valid in REPORT |
| `M` | `M` | Dynamic resize — user can drag the right edge at runtime | `PROPLIST:Resize` | Not valid in REPORT |
| `S` | `S(n)` | Scroll bar on group; `n` = total dialog units to scroll | `PROPLIST:Scroll` | Groups only; ignored inside group; not valid in REPORT |

---

## Colour Modifiers

| Modifier | Syntax | Meaning | PROPLIST | Notes |
|---|---|---|---|---|
| `*` | `*` | Per-cell colours — four supplemental LONG fields follow in QUEUE (fore normal, back normal, fore selected, back selected) | — | Not valid in REPORT |
| `E` | `E([c1][,c2][,c3][,c4])` | Default column colours: normal foreground, normal background, selected foreground, selected background | `PROPLIST:TextColor`, `BackColor`, `TextSelected`, `BackSelected` | Overridden by `*` per cell |
| `B` | `B(color)` | Selection bar frame colour | `PROPLIST:BarFrame` | — |

---

## Icon Modifiers

| Modifier | Syntax | Meaning | PROPLIST | Notes |
|---|---|---|---|---|
| `I` | `I` | Icon — LONG field in QUEUE after data (and after colour fields if `*` present); references `PROP:IconList` array | — | Not valid in REPORT |
| `J` | `J` | Transparent icon — same as `I` | — | Not valid in REPORT |

Use display picture `@P_PB` to show icon only (no text).

---

## Tree Modifier

```
T([options])
```

- LONG field in QUEUE (after colour/icon fields) holds the tree level.
- Positive = expanded node; negative = contracted node.
- PROPLIST: `PROPLIST:Tree`
- Not valid in REPORT.

| Option | Meaning |
|---|---|
| `1` | Root is level 1 (instead of 0); allows −1 for contracted root |
| `R` | Suppress connecting lines at root level |
| `L` | Suppress connecting lines between all levels |
| `B` | Suppress expansion boxes |
| `I` | Suppress level indentation (also suppresses lines and boxes) |

Example: `T(RL)` — suppress root lines and all inter-level lines.

---

## Style Modifiers

| Modifier | Syntax | Meaning | PROPLIST | Notes |
|---|---|---|---|---|
| `Z` | `Z(n)` | Default style for entire column; `n` = style number | `PROPLIST:ColStyle` | No per-cell LONG needed in QUEUE |
| `Y` | `Y` | Per-cell style — LONG field in QUEUE (after colour/icon/tree fields) | — | Not valid in REPORT |

---

## Tooltip Modifiers

| Modifier | Syntax | Meaning | PROPLIST | Notes |
|---|---|---|---|---|
| `P` | `P` | Tooltip from QUEUE — next QUEUE field after data provides tip text | `PROPLIST:Tip` | Not valid in REPORT; also works with VLB |
| `Q` | `Q'text'` | Default tooltip text used when `P` field is empty | `PROPLIST:DefaultTip` | Not valid in REPORT |

---

## Data Selection Modifiers

| Modifier | Syntax | Meaning | PROPLIST | Notes |
|---|---|---|---|---|
| `#` | `#n#` | Explicit QUEUE field number — start reading from field `n`. Subsequent columns take fields in order from `n`. If format fields ≥ queue fields, wraps around. | `PROPLIST:FieldNo` | Not valid on field groups |
| `?` | `?` | Locator column (COMBO only) — value shown in current-selection box; only **one** per LIST/COMBO; if none, first column assumed | `PROPLIST:Locator` | Not valid in REPORT |

---

## Groups

```
[width[modifiers]|[modifiers]column column column ...]
```

- Square brackets group multiple columns into one logical unit.
- Group-level modifiers (`M`, `F`, `S`, `P`, `Q`, `B`, `_`, `/`, `|`, `R`, `C`, `D`) apply to the whole group.
- `*`, `I`, `T`, `Y`, `#` are **not valid** on field groups.
- PROPLIST: `PROPLIST:Group`, `PROPLIST:GroupNo`

---

## Supplemental QUEUE Field Order

When a column uses modifiers that consume extra QUEUE fields, the fields must appear in this fixed order after the data field:

| Position | Modifier required | QUEUE field content |
|---|---|---|
| 1 | `*` or `E` | Normal foreground colour (LONG) |
| 2 | `*` or `E` | Normal background colour (LONG) |
| 3 | `*` or `E` | Selected foreground colour (LONG) |
| 4 | `*` or `E` | Selected background colour (LONG) |
| 5 | `I` or `J` | Icon number (LONG) |
| 6 | `T` | Tree level (LONG) |
| 7 | `Y` | Cell style number (LONG) |
| 8 | `P` | Tooltip text (STRING) |

---

## REPORT Restrictions

The following modifiers are **not valid in REPORT** (LIST/COMBO only):

`*`, `I`, `J`, `T`, `Y`, `F`, `M`, `S`, `P`, `Q`, `B`

---

## Example

```clarion
FORMAT('10L(2)|FY~Name~@s30@' &
       '8R|*~Amount~@n10.2@' &
       '[30L|MF~Details~12L|~First~@s20@18L|~Last~@s20@]')
```

- Column 1: 10du left, 2du left-margin, fixed, per-cell style, header "Name", picture `@s30@`
- Column 2: 8du right, per-cell colours, header "Amount", picture `@n10.2@`
- Group: 30du, resizable, fixed; contains two sub-columns (First, Last)
