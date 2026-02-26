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
        private const string Indent2 = "        '";  // continuation alignment

        public static string Generate(List<FormatColumn> columns)
        {
            if (columns == null || columns.Count == 0) return "FORMAT('')";

            var sb = new StringBuilder();

            for (int i = 0; i < columns.Count; i++)
            {
                string colSpec = BuildColumnSpec(columns[i]);

                if (i == 0)
                {
                    sb.Append(Indent1).Append(colSpec);
                }
                else
                {
                    // Close previous literal, add continuation, open new literal
                    sb.Append("' &\r\n").Append(Indent2).Append(colSpec);
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
