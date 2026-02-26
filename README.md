# ListFormatParser

A Clarion IDE addin that parses the `FORMAT()` attribute of a `LIST` control and displays the column definitions in a readable grid.

## Features

- Right-click any line inside a `LIST` control definition → **Parse List Format**
- Handles line continuations automatically — works across multi-line definitions
- Displays each column's width, alignment, indent, modifiers, header, header alignment, header indent, and picture
- Hover the **Modifiers** cell for a plain-English description of each modifier flag
- **Copy FORMAT** button regenerates a clean, normalised `FORMAT()` string with one column per line, ready to paste back into source
- Available in both the CLW source editor and the Embeditor (PWEE)
- Only appears in the context menu when the caret is inside a block containing `FORMAT()`

## Installation

1. Download `ListFormatParser-vX.X.X.zip` from [Releases](../../releases)
2. Extract into `C:\Clarion\Clarion11.1\accessory\addins\ListFormatParser\`  
   _(adjust path for your Clarion version)_
3. Restart the Clarion IDE

## Usage

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

Click **Copy FORMAT** to put a normalised `FORMAT()` string on the clipboard.

## Compatibility

Targets Clarion 10, 11, 11.1, and 12 (all ship `ICSharpCode.*.dll` v2.1.0.2447).

## Building from Source

Requires .NET SDK and a Clarion installation at `C:\Clarion\Clarion11.1`.

```
cd list-format-addin
dotnet build ListFormatParser.csproj -c Release
```

Copy `bin\Release\net40\ListFormatParser.dll` and `ListFormatParser.addin` to your addin folder.

## Credits

FORMAT() parsing logic ported from [Carl Barnes' List-Format-Parser](https://github.com/CarlTBarnes/List-Format-Parser) (MIT licence).

## Licence

[MIT](LICENSE)
