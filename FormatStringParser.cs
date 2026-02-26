using System.Collections.Generic;
using System.Text;

namespace ListFormatParser
{
    /// <summary>
    /// Parses a Clarion LIST FORMAT() string into individual column definitions.
    ///
    /// FORMAT() string syntax per column:
    ///   [width][L|R|C|D][(indent)]|[modifiers][~header~][@picture@]
    ///
    /// Columns are not delimited by a separator — they are identified by walking
    /// the string with a state machine.  The | within each column spec separates
    /// the width/alignment half from the modifier/header/picture half.
    ///
    /// Special cases handled:
    ///   [ ... ]          group columns (bracket pair wrapping child columns)
    ///   Z(n)  T(n) B(n)  modifier arguments in parens
    ///   Q'tip text'      tooltip modifier with a quoted string
    ///   ~tilde~ inside @picture@   e.g. @n4.2~%~@
    ///   ''               doubled quote inside a string literal
    /// </summary>
    public static class FormatStringParser
    {
        public static List<FormatColumn> Parse(string rawFormatString)
        {
            var result = new List<FormatColumn>();
            if (string.IsNullOrEmpty(rawFormatString)) return result;

            int i = 0, n = rawFormatString.Length, colNum = 0;

            while (i < n)
            {
                // Skip inter-column whitespace
                while (i < n && rawFormatString[i] == ' ') i++;
                if (i >= n) break;

                int   colStart = i;
                var   col      = new FormatColumn { ColumnNumber = ++colNum };

                // --- Group open / close ---
                if (rawFormatString[i] == '[') { col.IsGroupStart = true; i++; }
                else if (rawFormatString[i] == ']') { col.IsGroupEnd = true; i++; }

                // --- Width (leading digits) ---
                col.Width = ReadWhile(rawFormatString, ref i, char.IsDigit);

                // --- Alignment character (L R C D) ---
                if (i < n && "LRCDlrcd".IndexOf(rawFormatString[i]) >= 0)
                    col.Alignment = char.ToUpper(rawFormatString[i++]).ToString();

                // --- Indent / border: (n) ---
                if (i < n && rawFormatString[i] == '(')
                {
                    i++; // skip opening (
                    col.Indent = ReadBalancedParens(rawFormatString, ref i);
                    if (i < n && rawFormatString[i] == ')') i++; // skip closing )
                }

                // --- Column separator pipe ---
                if (i < n && rawFormatString[i] == '|') i++;

                // --- Modifiers (up to ~ or @, but respecting parens and Q'...' ) ---
                col.Modifiers = ReadModifiers(rawFormatString, ref i).Trim();

                // --- Header ~...~ ---
                if (i < n && rawFormatString[i] == '~')
                {
                    i++; // skip opening ~
                    col.Header = ReadUntilUnescaped(rawFormatString, ref i, '~');
                    if (i < n) i++; // skip closing ~

                    // Optional header alignment (L R C D) immediately after closing ~
                    if (i < n && "LRCDlrcd".IndexOf(rawFormatString[i]) >= 0)
                        col.HeaderAlignment = char.ToUpper(rawFormatString[i++]).ToString();

                    // Optional header indent (n) after header alignment
                    if (i < n && rawFormatString[i] == '(')
                    {
                        i++; // skip opening (
                        col.HeaderIndent = ReadBalancedParens(rawFormatString, ref i);
                        if (i < n && rawFormatString[i] == ')') i++; // skip closing )
                    }
                }

                // --- Picture @...@ (may contain ~tilde~ inside) ---
                if (i < n && rawFormatString[i] == '@')
                {
                    i++; // skip opening @
                    col.Picture = ReadPicture(rawFormatString, ref i);
                    if (i < n) i++; // skip closing @
                }

                col.RawSpec = rawFormatString.Substring(colStart, i - colStart);
                result.Add(col);
            }

            return result;
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private static string ReadWhile(string s, ref int i, System.Func<char, bool> pred)
        {
            int start = i;
            while (i < s.Length && pred(s[i])) i++;
            return s.Substring(start, i - start);
        }

        /// <summary>Reads the content inside already-opened '(' up to the matching ')'.</summary>
        private static string ReadBalancedParens(string s, ref int i)
        {
            var sb    = new StringBuilder();
            int depth = 1;
            while (i < s.Length && depth > 0)
            {
                char c = s[i];
                if      (c == '(') { depth++; sb.Append(c); i++; }
                else if (c == ')') { depth--; if (depth > 0) { sb.Append(c); i++; } }
                else               { sb.Append(c); i++; }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Reads modifier characters up to ~ or @.
        /// Handles Z(n), T(n), B(n) modifier arguments in parens
        /// and Q'tooltip text' modifier with a quoted string.
        /// </summary>
        private static string ReadModifiers(string s, ref int i)
        {
            var sb = new StringBuilder();
            while (i < s.Length)
            {
                char c = s[i];
                if (c == '~' || c == '@') break;

                if (c == '(')
                {
                    // modifier argument in parens e.g. Z(6), T(0), B(00FF00h)
                    sb.Append(c); i++;
                    string inner = ReadBalancedParens(s, ref i);
                    sb.Append(inner);
                    if (i < s.Length && s[i] == ')') { sb.Append(')'); i++; }
                    continue;
                }

                if (c == '\'')
                {
                    // Q'tooltip text' — read the quoted string
                    sb.Append(c); i++;
                    while (i < s.Length)
                    {
                        char q = s[i];
                        sb.Append(q); i++;
                        if (q == '\'')
                        {
                            // doubled quote inside tooltip
                            if (i < s.Length && s[i] == '\'') { sb.Append('\''); i++; }
                            else break;
                        }
                    }
                    continue;
                }

                sb.Append(c); i++;
            }
            return sb.ToString();
        }

        /// <summary>Reads until <paramref name="delimiter"/> (unescaped).</summary>
        private static string ReadUntilUnescaped(string s, ref int i, char delimiter)
        {
            var sb = new StringBuilder();
            while (i < s.Length && s[i] != delimiter)
                sb.Append(s[i++]);
            return sb.ToString();
        }

        /// <summary>
        /// Reads picture content between the already-consumed opening @ and the
        /// closing @. Handles ~tilde~ sequences inside the picture (e.g. @n4~%~@).
        /// </summary>
        private static string ReadPicture(string s, ref int i)
        {
            var sb = new StringBuilder();
            while (i < s.Length && s[i] != '@')
            {
                if (s[i] == '~')
                {
                    sb.Append('~'); i++;
                    while (i < s.Length && s[i] != '~') sb.Append(s[i++]);
                    if (i < s.Length) { sb.Append('~'); i++; }
                    continue;
                }
                sb.Append(s[i++]);
            }
            return sb.ToString();
        }
    }
}
