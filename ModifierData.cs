using System.Collections.Generic;

namespace ListFormatParser
{
    /// <summary>
    /// Static reference data for all FORMAT() modifier characters.
    /// Ported from Carl Barnes' HelpCls.Add1Q entries (ListFormatParser.clw lines 1475-1565).
    /// </summary>
    internal static class ModifierData
    {
        internal struct Entry
        {
            public string Char;
            public string Type;
            public string PropList;
            public string Name;
            public string Description;
        }

        private static Entry E(string ch, string type, string prop, string name, string desc)
        {
            return new Entry { Char = ch, Type = type, PropList = prop, Name = name, Description = desc };
        }

        internal static readonly List<Entry> All = new List<Entry>
        {
            // ── Alignment ────────────────────────────────────────────────────
            E("L",    "Align", "PROPLIST:Left",    "Justify Left",
                "Left alignment of column data. May be offset by an (Indent)."),
            E("R",    "Align", "PROPLIST:Right",   "Justify Right",
                "Right alignment of column data. May be offset by an (Indent)."),
            E("C",    "Align", "PROPLIST:Center",  "Justify Center",
                "Center alignment of column data. May be offset by an (Indent)."),
            E("D",    "Align", "PROPLIST:Decimal", "Justify Decimal",
                "Decimal alignment of column data. The offset to the decimal point is specified with an (Indent)."),
            E("()",   "Align", "PROPLIST:LeftOffset :RightOffset :CenterOffset :DecimalOffset", "Column Data Indent",
                "A number in parentheses following an alignment modifier sets the indent offset of column data from the justified edge."),
            E("0-9",  "Align", "PROPLIST:Width",   "Column Width (DLUs)",
                "Numbers in the FORMAT string outside delimiters ()~~@@## denote column width in dialog units. Numbers are the width of the DATA area; the column header may be wider."),

            // ── Header ───────────────────────────────────────────────────────
            E("~~",   "Head",  "PROPLIST:Header",  "Column Header ~Text~",
                "A string enclosed in ~tildes~ displays the header at the top of the list. The header text may include justification (L R C D) and indent after the closing tilde, e.g. ~Heading~L(2)."),
            E("~L",   "Head",  "PROPLIST:HeaderLeft",    "Header Justify Left",
                "Left alignment of header text. May be offset by an (Indent). Appears in Format as ~Header~L."),
            E("~R",   "Head",  "PROPLIST:HeaderRight",   "Header Justify Right",
                "Right alignment of header text. May be offset by an (Indent). Appears in Format as ~Header~R."),
            E("~C",   "Head",  "PROPLIST:HeaderCenter",  "Header Justify Center",
                "Center alignment of header text. May be offset by an (Indent). Appears in Format as ~Header~C."),
            E("~D",   "Head",  "PROPLIST:HeaderDecimal", "Header Justify Decimal",
                "Decimal alignment of header text. May be offset by an (Indent). Appears in Format as ~Header~D."),
            E("~()",  "Head",  "PROPLIST:HeaderLeftOffset :HeaderRightOffset :HeaderCenterOffset :HeaderDecimalOffset", "Header Indent",
                "A number in parentheses after the header justification sets the indent of the header text."),

            // ── Data ─────────────────────────────────────────────────────────
            E("@@",   "Data",  "PROPLIST:Picture",  "Picture @picture@",
                "The picture formats the field for display. The trailing @ is required to define the end of the picture, e.g. @n9.2@ or @s30@."),
            E("#",    "Data",  "PROPLIST:FieldNo",  "Field Number #number#",
                "A #number# enclosed in pound signs indicates the QUEUE field to display. Following queue fields are used for Color (*), Icon (I/J), Tree (T), Style (Y), and Tip (P) modifier data."),
            E("S()",  "Data",  "PROPLIST:Scroll",   "Scroll Bar S(width)",
                "An S(integer) adds a horizontal scroll bar to the field or group. The integer defines the total dialog units for the scrollable content."),

            // ── Color ────────────────────────────────────────────────────────
            E("*",    "Color", "PROPLIST:Color",    "Cell Color * (4x LONG in Queue)",
                "An asterisk * indicates color information for the cell is in 4 LONG fields after the data field in the queue: ForeColor, BackColor, SelectedFore, SelectedBack."),
            E("B()",  "Color", "PROPLIST:BarFrame",  "Selection Bar Frame Color B(color)",
                "A B(color) specifies the color of the selection bar frame."),
            E("E()",  "Color", "PROPLIST:TextColor :BackColor :TextSelected :BackSelected", "Column Default Colors E(f,b,sf,sb)",
                "An E(f,b,sf,sb) specifies column default colors: foreground, background, selected foreground, selected background. Per-cell queue colors (*) override these defaults."),
            E("HB()", "Head",  "PROPLIST:HdrBackColor", "Header Background Color HB(color)",
                "An HB(color) specifies the column header background color."),
            E("HT()", "Head",  "PROPLIST:HdrTextColor", "Header Text Color HT(color)",
                "An HT(color) specifies the column header text color."),

            // ── Icon ─────────────────────────────────────────────────────────
            E("I",    "Icon",  "PROPLIST:Icon",     "Cell Icon I (from Queue LONG)",
                "An I indicates an icon displays in the column. The icon number is in the next LONG field after the data field in the queue. Negative values use resource icons; positive values use the window ICON list."),
            E("J",    "Icon",  "PROPLIST:IconTrn",  "Cell Icon Transparent J",
                "A J indicates a transparent icon displays in the column. Same queue field conventions as I (Icon)."),

            // ── Tree ─────────────────────────────────────────────────────────
            E("T()",  "Tree",  "PROPLIST:Tree",     "Tree Control T(1)(B)(L)(I)(R)",
                "T(flags) enables tree-view display. Flags: 1=show tree, B=show boxes, L=show lines, I=show icons, R=root lines. Requires a LONG indent-level field after the data field in the queue."),

            // ── Style ────────────────────────────────────────────────────────
            E("Y",    "Style", "PROPLIST:CellStyle", "Cell Style Y (LONG in Queue)",
                "A Y indicates a Style Number for the cell is in a LONG field after the data field in the queue. Styles define font, size, and color overrides per cell."),
            E("Z()",  "Style", "PROPLIST:ColStyle",  "Column Default Style Z(number)",
                "A Z(number) sets the default style for the entire column. Individual cell styles set via Y override this column default."),

            // ── Tooltip ──────────────────────────────────────────────────────
            E("P",    "Tip",   "PROPLIST:Tip",       "Cell Tooltip P (STRING in Queue)",
                "A P adds a per-cell tooltip. The tip text is in the next STRING field after the data field in the queue."),
            E("Q''",  "Tip",   "PROPLIST:DefaultTip", "Column Default Tooltip Q'string'",
                "A Q followed by a 'string' in the FORMAT designates the default tooltip text for all cells in the column. If P is also present, the per-cell queue value overrides this default."),

            // ── Flags ────────────────────────────────────────────────────────
            E("?",    "Flag",  "PROPLIST:Locator",   "Locator Column ?",
                "A ? designates this as the locator column — the user can type to locate a row. Only one column can have the ? modifier; if none is present the first column is the default locator."),
            E("M",    "Flag",  "PROPLIST:Resize",    "Resizable Column M",
                "An M allows the column or group to be resized at runtime by dragging the right border."),
            E("F",    "Flag",  "PROPLIST:Fixed",     "Fixed Column F (no horizontal scroll)",
                "An F creates a fixed column that stays on screen when the user horizontally pages through the fields. Fixed columns appear to the left of scrollable columns."),
            E("_",    "Flag",  "PROPLIST:Underline", "Underline Cell _",
                "An underscore _ underlines the field."),
            E("/",    "Flag",  "PROPLIST:LastOnLine", "Last on Line / (multi-line list)",
                "A slash / causes the next field to appear on a new line. Used only in a multi-line list where fields are arranged on more than one row per record."),
            E("|",    "Flag",  "PROPLIST:RightBorder", "Right Border |",
                "A pipe | places a vertical line to the right of the field."),

            // ── Group ────────────────────────────────────────────────────────
            E("[]",   "Group", "PROPLIST:Group",     "Column Group [ ]",
                "Square brackets [ ] group multiple columns. A group may specify a spanning header. Groups can create multi-line lists using the / (LastOnLine) modifier inside the group."),
            E("[]()", "Group", "PROPLIST:Width of [Group]", "Group Width [](size)",
                "A number in parentheses after the closing ] specifies the total width of the group in dialog units."),
            E("]~~",  "Group", "PROPLIST:Header of [Group]", "Group Header ]~Text~",
                "A string in ~tildes~ after the closing ] sets the header text that spans all columns in the group."),
            E("]~()", "Group", "PROPLIST:HeaderLeftOffset :HeaderRightOffset :HeaderCenterOffset :HeaderDecimalOffset of [Group]", "Group Header Indent",
                "A number in parentheses after the group header justification sets the indent of the group header text."),
        };
    }
}
