using System.Collections.Generic;
using System.Text;

namespace ListFormatParser
{
    /// <summary>
    /// Shared parsing helpers for FROM('...') string commands.
    /// </summary>
    internal static class FromParser
    {
        internal struct FromEntry
        {
            public string Display; // The visible label
            public string Value;   // The #alternate value, or null if not present
        }

        /// <summary>
        /// Parses a pipe-delimited FROM string into display/value pairs.
        /// A token starting with '#' is the alternate value for the previous display token.
        /// Example: "Mr.|#1|Mrs.|#2" → [(Mr., 1), (Mrs., 2)]
        /// </summary>
        internal static List<FromEntry> ParseEntries(string fromValue)
        {
            var entries = new List<FromEntry>();
            foreach (string token in fromValue.Split('|'))
            {
                if (token.Length > 0 && token[0] == '#')
                {
                    if (entries.Count > 0)
                    {
                        var last = entries[entries.Count - 1];
                        last.Value = token.Substring(1);
                        entries[entries.Count - 1] = last;
                    }
                }
                else
                {
                    entries.Add(new FromEntry { Display = token, Value = null });
                }
            }
            return entries;
        }

        /// <summary>Flattens a continuation block into a single string.</summary>
        internal static string BuildFlat(string[] lines, int start, int end)
        {
            var sb = new StringBuilder();
            for (int i = start; i <= end; i++)
            {
                string line = lines[i];
                int    pipe = ClarionCodeParser.FindContinuationPipe(line);
                int    len  = pipe >= 0 ? pipe : line.Length;
                int    col  = (i > start) ? (line.Length - line.TrimStart().Length) : 0;
                if (col > len) col = len;
                string part = line.Substring(col, len - col).TrimEnd();
                if (part.Length == 0) continue;
                if (sb.Length > 0 && sb[sb.Length - 1] != ' ' && part[0] != ' ')
                    sb.Append(' ');
                sb.Append(part);
            }
            return sb.ToString();
        }

        /// <summary>Extracts the string literal content from a paren expression.</summary>
        internal static string ExtractStringValue(string parenContent)
        {
            var  sb    = new StringBuilder();
            bool inStr = false;
            for (int i = 0; i < parenContent.Length; i++)
            {
                char c = parenContent[i];
                if (!inStr) { if (c == '\'') inStr = true; }
                else
                {
                    if (c == '\'' && i + 1 < parenContent.Length && parenContent[i + 1] == '\'')
                    { sb.Append('\''); i++; }
                    else if (c == '\'') inStr = false;
                    else sb.Append(c);
                }
            }
            return sb.ToString();
        }

        internal static int FindGroupStart(string[] lines, int caretLine)
        {
            int i = caretLine;
            while (i > 0 && ClarionCodeParser.HasContinuation(lines[i - 1]))
                i--;
            return i;
        }

        internal static int FindGroupEnd(string[] lines, int start)
        {
            int i = start;
            while (i < lines.Length - 1 && ClarionCodeParser.HasContinuation(lines[i]))
                i++;
            return i;
        }

        /// <summary>
        /// Finds the FROM string entries and USE variable from the current block.
        /// Returns false and sets an error message if not applicable.
        /// </summary>
        internal static bool TryGetFromEntries(
            string[] lines, int caretLine,
            out List<FromEntry> entries, out string useVar, out string error)
        {
            entries = null;
            useVar  = "ListUseVariable";
            error   = null;

            int blockStart = FindGroupStart(lines, caretLine);
            int blockEnd   = FindGroupEnd(lines, blockStart);
            string flat    = BuildFlat(lines, blockStart, blockEnd);
            string code    = ClarionCodeParser.MakeCodeOnlyLine(flat);

            int begAtt, begParen, endParen;
            if (!ClarionCodeParser.FindAttrParen(code, "FROM", out begAtt, out begParen, out endParen))
            {
                error = "No FROM() attribute found in the current block.";
                return false;
            }

            string fromValue = ExtractStringValue(flat.Substring(begParen, endParen - begParen + 1));
            if (string.IsNullOrEmpty(fromValue))
            {
                error = "FROM() does not contain a string literal.\n\nThis command only applies to FROM('item1|item2|...') string forms.";
                return false;
            }

            entries = ParseEntries(fromValue);
            if (entries.Count == 0)
            {
                error = "No entries found in FROM() string.";
                return false;
            }

            // Try to resolve the USE() variable
            int uBeg, uBegP, uEndP;
            if (ClarionCodeParser.FindAttrParen(code, "USE", out uBeg, out uBegP, out uEndP)
                && uEndP > uBegP)
            {
                string name = flat.Substring(uBegP + 1, uEndP - uBegP - 1).Trim();
                if (name.Length > 0 && name[0] == '?') name = name.Substring(1);
                if (name.Length > 0) useVar = name;
            }

            return true;
        }
    }
}
