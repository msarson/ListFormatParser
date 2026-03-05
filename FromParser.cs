using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

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

        /// <summary>
        /// Re-parses a formatted FROM('...' &|\n...) string (as produced by GenerateFromLines)
        /// back into a list of entries.  Strips the FROM('...') wrapper, collapses continuation
        /// markers, then delegates to ParseEntries.
        /// </summary>
        internal static List<FromEntry> ParseFromString(string fromLines)
        {
            // Strip FROM(' prefix and ') suffix, collapse &| continuation markers
            if (string.IsNullOrEmpty(fromLines)) return new List<FromEntry>();
            string s = fromLines.Trim();
            // Remove FROM( and trailing )
            if (s.StartsWith("FROM(", System.StringComparison.OrdinalIgnoreCase))
                s = s.Substring(5);
            if (s.EndsWith(")"))
                s = s.Substring(0, s.Length - 1);
            // Collapse continuation: ' &|\r\n<spaces>' → nothing (string continues)
            s = System.Text.RegularExpressions.Regex.Replace(s, @"'\s*&\|\s*\r?\n\s*'", "");
            // Now s is like 'Mr.|#1|Mrs.|#2' — extract the string value
            string inner = ExtractStringValue("(" + s + ")");
            return ParseEntries(inner);
        }


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

        /// <summary>
        /// Generates a Clarion-style multi-line FROM('...') string — one entry per line,
        /// with &| continuation markers aligned.  contIndent is the number of spaces
        /// to use on continuation lines (typically the column of FROM( + 5).
        /// </summary>
        internal static string GenerateFromLines(List<FromEntry> entries, int contIndent = 5)
        {
            if (entries == null || entries.Count == 0) return "FROM('')";

            string indent = new string(' ', contIndent < 0 ? 0 : contIndent);

            // Build the content token for each entry
            var contents = new string[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                string s = entries[i].Display.Replace("'", "''");
                if (entries[i].Value != null)
                    s += "|#" + entries[i].Value.Replace("'", "''");
                if (i < entries.Count - 1)
                    s += "|";
                contents[i] = s;
            }

            // Align the &| markers
            int maxLen = 0;
            for (int i = 0; i < entries.Count - 1; i++)
                if (contents[i].Length > maxLen) maxLen = contents[i].Length;

            var sb = new StringBuilder();
            sb.Append("FROM('");
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0) sb.Append(indent).Append("'");
                sb.Append(contents[i]);
                if (i < entries.Count - 1)
                {
                    sb.Append("'");
                    int pad = maxLen - contents[i].Length;
                    if (pad > 0) sb.Append(new string(' ', pad));
                    sb.Append(" &|\r\n");
                }
                else
                {
                    sb.Append("')");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Reconstructs the flat source line with a new FROM('...') substituted in.
        /// Returns null if FROM() cannot be located in the flat line.
        /// </summary>
        internal static string BuildReformattedSourceLine(string flatLine, List<FromEntry> entries)
        {
            if (string.IsNullOrEmpty(flatLine) || entries == null) return null;
            string code = ClarionCodeParser.MakeCodeOnlyLine(flatLine);
            int begAtt, begParen, endParen;
            if (!ClarionCodeParser.FindAttrParen(code, "FROM", out begAtt, out begParen, out endParen))
                return null;
            int indent = begAtt + 5; // FROM( = 5 chars to the opening quote
            string cleanFrom = GenerateFromLines(entries, indent);
            return flatLine.Substring(0, begAtt) + cleanFrom + flatLine.Substring(endParen + 1);
        }
    }
}
