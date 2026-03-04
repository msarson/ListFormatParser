# ListFormatParser

A Clarion IDE addin that parses the `FORMAT()` attribute of a `LIST` control and displays the column definitions in a readable grid. Also provides formatting and code-generation tools for the `FROM()` attribute.

## Features

### FORMAT() tools
- Right-click any line inside a `LIST` control definition → **Parse List Format**
- Handles line continuations automatically — works across multi-line definitions
- Displays each column's width, alignment, indent, modifiers, header, header alignment, header indent, and picture
- Hover the **Modifiers** cell for a plain-English description of each modifier flag
- **Copy FORMAT** button regenerates a clean, normalised `FORMAT()` string with one column per line, ready to paste back into source
- **Edit → Format → Clean List Format** — rewrites the `FORMAT()` attribute in-place, one column per line with aligned `&|` continuation markers

### FROM() tools (string literal form only — `FROM('item1|item2|...')`)

All three commands appear only when the caret is inside a block containing a string-form `FROM('...')`.

| Menu location | Command | Description |
|---|---|---|
| Edit → Format | **Format FROM Selections** | Rewrites `FROM('...')` in-place, one entry per line with aligned `&|` |
| Edit / right-click | **Copy FROM as CASE** | Copies a `CASE` statement to clipboard, using `#value` alternates as `OF` targets |
| Edit / right-click | **Copy FROM as CHOOSE** | Copies a `CHOOSE()` call to clipboard using the display labels |

#### FROM string format
Clarion `FROM` strings use `|` as a separator. An entry starting with `#` is the alternate value for the preceding display item:

```
FROM('Mr.|#1|Mrs.|#2|Ms.|#3|Dr.|#4')
```

#### Format FROM Selections output
```
FROM('Mr.|#1|'  &|
     'Mrs.|#2|' &|
     'Ms.|#3|'  &|
     'Dr.|#4')
```

#### Copy FROM as CASE output
```
CASE UseVar
OF '1'    !  1  Mr.
OF '2'    !  2  Mrs.
OF '3'    !  3  Ms.
OF '4'    !  4  Dr.
END

!Choices: 'Mr.','Mrs.','Ms.','Dr.'
!Values:  '1',  '2',   '3',  '4'
```
The `USE()` variable from the `LIST` is used as the `CASE` target automatically. `!Values:` is omitted when there are no `#` alternates.

#### Copy FROM as CHOOSE output
```
CHOOSE(UseVar,'Mr.','Mrs.','Ms.','Dr.')
```

### General
- Available in both the CLW source editor and the Embeditor (PWEE)
- Context menu items appear only when the caret is inside a relevant block (FORMAT or FROM condition-gated)

## Installation

The easiest way to install is via **[Addin Finder](https://github.com/msarson/ClarionAddinFinder)** — the Clarion IDE addin manager. Search for *List Format Parser* and click Install.

**Manual install:**

1. Download `ListFormatParser.dll` and `ListFormatParser.addin` from [Releases](../../releases)
2. Copy both files into `C:\Clarion\Clarion11.1\accessory\addins\ListFormatParser\`  
   _(adjust path for your Clarion version)_
3. Restart the Clarion IDE

## Usage

### FORMAT()
Open any `.clw` file containing a `LIST` control with a `FORMAT()` attribute.  
Place the caret anywhere on the `LIST` definition (any continuation line), right-click and choose **Parse List Format**.

The dialog shows:

| Column | Description |
|---|---|
| # | Column number |
| Width | Column width in dialog units |
| Align | Data alignment (L/R/C/D) |
| Indent | Data justification offset |
| Modifiers | Raw modifier flags — hover for meaning |
| Header | Header text |
| Hdr Align | Header alignment override |
| Hdr Indent | Header indent override |
| Picture | Display picture |
| Format Spec | Original raw column spec |

Click **Copy FORMAT** to put a normalised `FORMAT()` string on the clipboard, or use **Edit → Format → Clean List Format** to rewrite it in-place.

### FROM()
Place the caret anywhere on a line (or continuation block) that contains `FROM('...')` and use:
- **Edit → Format → Format FROM Selections** to reformat in-place
- **Edit → Copy FROM as CASE** (or right-click) for a `CASE` statement on the clipboard
- **Edit → Copy FROM as CHOOSE** (or right-click) for a `CHOOSE()` call on the clipboard

## Compatibility

Targets Clarion 10, 11, 11.1, and 12 (all ship `ICSharpCode.*.dll` v2.1.0.2447).

## Building from Source

Requires .NET SDK and a Clarion installation at `C:\Clarion\Clarion11.1`.

```
cd list-format-addin
dotnet build ListFormatParser.csproj -c Release
```

Copy `bin\x86\Release\net40\ListFormatParser.dll` and `ListFormatParser.addin` to your addin folder.

## Credits

FORMAT() parsing logic ported from [Carl Barnes' List-Format-Parser](https://github.com/CarlTBarnes/List-Format-Parser) (MIT licence).

## Licence

[MIT](LICENSE)
