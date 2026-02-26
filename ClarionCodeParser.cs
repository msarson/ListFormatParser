using System.Text;

namespace ListFormatParser
{
    /// <summary>
    /// Ported and extended from Carl Barnes' CBCodeParseClass (Clarion, MIT licence).
    ///
    /// Key methods:
    ///   MakeCodeOnlyLine  — masks string literal content with spaces so attribute
    ///                       names can be found safely without matching text inside strings.
    ///   FindAttrParen     — locates a named attribute and its balanced parentheses.
    ///   FindContinuationPipe — returns position of the first | outside a string literal.
    /// </summary>
    public static class ClarionCodeParser
    {
        /// <summary>
        /// Returns a version of <paramref name="line"/> where all content inside
        /// string literals is replaced with spaces, and the whole result is upper-cased.
        /// Safe to search for attribute keywords in the result.
        /// </summary>
        public static string MakeCodeOnlyLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return line;

            var result = new char[line.Length + 1];
            bool inQuote = false;
            int len = 0;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '\'')
                {
                    // Doubled quote '' inside a string = escaped quote, not end-of-string
                    if (inQuote && i + 1 < line.Length && line[i + 1] == '\'')
                    {
                        result[len++] = ' '; // treat both chars as masked content
                        i++;
                        continue;
                    }
                    inQuote = !inQuote;
                    result[len++] = c;
                    continue;
                }

                // Comment or continuation ends the useful part of the line
                if (!inQuote && (c == '!' || c == '|'))
                {
                    result[len++] = ' ';
                    break;
                }

                if (c == '\t') c = ' ';
                if (!inQuote && (c == '.' || c == ';')) c = ' ';

                result[len++] = inQuote ? ' ' : char.ToUpper(c);
            }

            return new string(result, 0, len);
        }

        /// <summary>
        /// Finds the position of <paramref name="attrName"/> followed by '(' in
        /// <paramref name="codeOnly"/> (output of MakeCodeOnlyLine), and locates
        /// the matching closing ')'.
        ///
        /// Returns true if found. All positions are 0-based indices into codeOnly
        /// and are equally valid as indices into the original (unmasked) line.
        /// </summary>
        public static bool FindAttrParen(string codeOnly, string attrName,
            out int begAttPos, out int begParen, out int endParen)
        {
            begAttPos = -1; begParen = -1; endParen = -1;

            string upper   = codeOnly.ToUpper();
            string attr    = attrName.ToUpper();
            int    len     = upper.Length;
            int    attrLen = attr.Length;
            int    inQuote = 0;

            for (int i = 0; i < len; i++)
            {
                if (upper[i] == '\'') { inQuote = 1 - inQuote; continue; }
                if (inQuote != 0) continue;
                if (upper[i] != '(') continue;

                // Walk back past optional spaces to find the attribute name
                int attEnd = i - 1;
                while (attEnd >= 0 && upper[attEnd] == ' ') attEnd--;
                int attBeg = attEnd - attrLen + 1;
                if (attBeg < 0) continue;
                if (upper.Substring(attBeg, attrLen) != attr) continue;

                // Must be preceded by a delimiter (comma, space, or start)
                if (attBeg > 0)
                {
                    char pre = upper[attBeg - 1];
                    if (pre != ',' && pre != ' ' && pre != '(' && pre != '\t') continue;
                }

                begAttPos = attBeg;
                begParen  = i;

                // Find the balanced closing paren
                int depth  = 1;
                int iq     = 0;
                for (int j = i + 1; j < len; j++)
                {
                    if (upper[j] == '\'') { iq = 1 - iq; continue; }
                    if (iq != 0) continue;
                    if      (upper[j] == '(') depth++;
                    else if (upper[j] == ')') { depth--; if (depth == 0) { endParen = j; return true; } }
                }
                return false; // unmatched paren
            }
            return false;
        }

        /// <summary>
        /// Returns the 0-based index of the first | that is outside a string literal,
        /// or -1 if none found. Handles Clarion's '' doubled-quote escape.
        /// </summary>
        public static int FindContinuationPipe(string line)
        {
            bool inString = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\'')
                {
                    if (inString && i + 1 < line.Length && line[i + 1] == '\'')
                        i++; // skip doubled quote — stay in string
                    else
                        inString = !inString;
                }
                else if (c == '|' && !inString)
                    return i;
            }
            return -1;
        }

        public static bool HasContinuation(string line) => FindContinuationPipe(line) >= 0;
    }
}
