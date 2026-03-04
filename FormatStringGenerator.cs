using System.Collections.Generic;
using System.Text;

namespace ListFormatParser
{
    /// <summary>
    /// Rebuilds a Clarion FORMAT() attribute string from a list of parsed columns.
    /// Output is wrapped with line continuations at column boundaries, matching the
    /// style of Carl Barnes' CopyFormatBtn / GetLinesFmt.
    ///
    /// Example output:
    ///   FORMAT('51L(2)|FY~Name~@s30@' &
    ///          '8R|~Amount~@n10.2@')
    /// </summary>
    public static class FormatStringGenerator
    {
        private const string Indent1 = "FORMAT('";   // opening — 8 chars

        /// <param name="continuationIndent">
        /// Spaces to place before each continuation line's opening quote.
        /// Should equal the column of FORMAT( in the source so all quotes align.
        /// Defaults to 8 spaces (same width as "FORMAT('").
        /// </param>
        public static string Generate(List<FormatColumn> columns, string continuationIndent = "        ")
        {
            if (columns == null || columns.Count == 0) return "FORMAT('')";

            // Pre-build all column specs to find max length for &| alignment
            var specs = new string[columns.Count];
            for (int i = 0; i < columns.Count; i++)
                specs[i] = BuildColumnSpec(columns[i]);

            int maxLen = 0;
            for (int i = 0; i < columns.Count - 1; i++)
                if (specs[i].Length > maxLen) maxLen = specs[i].Length;

            var sb = new StringBuilder();

            for (int i = 0; i < columns.Count; i++)
            {
                bool isLast = (i == columns.Count - 1);

                if (i == 0)
                    sb.Append(Indent1).Append(specs[i]);
                else
                    sb.Append(continuationIndent).Append('\'').Append(specs[i]);

                if (!isLast)
                {
                    sb.Append("'"); // close quote first
                    int pad = maxLen - specs[i].Length;
                    if (pad > 0) sb.Append(new string(' ', pad));
                    sb.Append(" &|\r\n");
                }
            }

            sb.Append("')");
            return sb.ToString();
        }

        /// <summary>
        /// Rebuilds one column's spec string from its parsed properties.
        /// Mirrors the QueueGenFormat / SimpleGen field-build loop in Carl's code.
        /// </summary>
        private static string BuildColumnSpec(FormatColumn col)
        {
            var sb = new StringBuilder();

            // Group open
            if (col.IsGroupStart) sb.Append('[');

            // Width
            sb.Append(col.Width ?? "");

            // Data alignment
            sb.Append(col.Alignment ?? "");

            // Data indent (n)
            if (!string.IsNullOrEmpty(col.Indent))
                sb.Append('(').Append(col.Indent).Append(')');

            // Column separator pipe
            sb.Append('|');

            // Modifiers
            sb.Append(col.Modifiers ?? "");

            // Header ~text~[align[(indent)]]
            if (col.Header != null)  // empty string is valid (blank header)
            {
                sb.Append('~').Append(col.Header).Append('~');
                if (!string.IsNullOrEmpty(col.HeaderAlignment))
                {
                    sb.Append(col.HeaderAlignment);
                    if (!string.IsNullOrEmpty(col.HeaderIndent))
                        sb.Append('(').Append(col.HeaderIndent).Append(')');
                }
            }

            // Picture @...@
            if (!string.IsNullOrEmpty(col.Picture))
                sb.Append('@').Append(col.Picture).Append('@');

            // Group close
            if (col.IsGroupEnd) sb.Append(']');

            return sb.ToString();
        }
    }
}
