using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.DefaultEditor.Gui.Editor;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.TextEditor.Document;
using System.Collections.Generic;
using System.Text;

namespace ListFormatParser
{
    /// <summary>
    /// "Format FROM Selections" command — finds the FROM() attribute in the current
    /// continuation block, parses the pipe-delimited string into display/value pairs,
    /// and reformats it one entry per line.
    ///
    /// Handles: FROM('Mr.|#1|Mrs.|#2|Ms.|#3|Dr.|#4')
    /// Items without a #value alternate are also supported.
    /// Everything outside FROM('...') is left completely untouched.
    /// </summary>
    public class CleanFromCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            var window = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow;
            if (window == null) return;
            var provider = window.ActiveViewContent as ITextEditorControlProvider;
            if (provider == null) return;

            var tec       = provider.TextEditorControl;
            var doc       = tec.Document;
            var area      = tec.ActiveTextAreaControl;
            int caretLine = area.Caret.Line;

            string[] lines = doc.TextContent.Split(
                new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);

            // 1. Find the continuation block the caret sits in
            int blockStart = FindGroupStart(lines, caretLine);
            int blockEnd   = FindGroupEnd(lines, blockStart);

            // 2. Build a flat version with position map
            FlatMap map;
            string flat = BuildFlatWithMap(lines, blockStart, blockEnd, out map);

            // 3. Locate FROM(...) in the flat string
            string codeOnly = ClarionCodeParser.MakeCodeOnlyLine(flat);
            int begAtt, begParen, endParen;
            if (!ClarionCodeParser.FindAttrParen(codeOnly, "FROM",
                    out begAtt, out begParen, out endParen))
            {
                MessageService.ShowMessage("No FROM() attribute found in the current block.");
                return;
            }

            // 4. Extract the string value from FROM('...')
            string parenContent = flat.Substring(begParen, endParen - begParen + 1);
            string fromValue    = ExtractStringValue(parenContent);
            if (string.IsNullOrEmpty(fromValue))
            {
                MessageService.ShowMessage(
                    "FROM() does not contain a string literal — nothing to format.\n\n" +
                    "This command only applies to FROM('item1|item2|...') string forms.");
                return;
            }

            // 5. Parse the pipe-delimited entries into display/value pairs
            var entries = ParseFromEntries(fromValue);
            if (entries.Count == 0)
            {
                MessageService.ShowMessage("No entries found in FROM() string.");
                return;
            }

            if (entries.Count == 1)
            {
                MessageService.ShowMessage("FROM() has only one entry — nothing to reformat.");
                return;
            }

            // 6. Generate clean replacement aligned to FROM('s opening quote
            int    fromCol     = map.FlatToDocColumn(begAtt);
            // FROM( is 5 chars, so the opening ' aligns continuation lines at fromCol+5
            string contIndent  = new string(' ', fromCol >= 0 ? fromCol + 5 : 5);
            string cleanFrom   = GenerateCleanFrom(entries, contIndent);

            // 7. Map flat positions back to document offsets
            int docOffsetStart = map.FlatToDocOffset(begAtt, doc);
            int docOffsetEnd   = map.FlatToDocOffset(endParen, doc) + 1; // +1 to include ')'

            if (docOffsetStart < 0 || docOffsetEnd < 0 || docOffsetEnd <= docOffsetStart)
            {
                MessageService.ShowMessage("Could not map FROM() position back to source.");
                return;
            }

            // 8. Replace in document — one undo action
            doc.UndoStack.StartUndoGroup();
            try
            {
                doc.Replace(docOffsetStart, docOffsetEnd - docOffsetStart, cleanFrom);
            }
            finally
            {
                doc.UndoStack.EndUndoGroup();
            }

            tec.Refresh();
        }

        // -----------------------------------------------------------------------
        // FROM string parsing
        // -----------------------------------------------------------------------

        private struct FromEntry
        {
            public string Display; // The visible label
            public string Value;   // The #alternate value, or null if not present
        }

        /// <summary>
        /// Parses a pipe-delimited FROM string into display/value pairs.
        /// A token starting with '#' is the alternate value for the previous display token.
        /// Example: "Mr.|#1|Mrs.|#2" → [(Mr., 1), (Mrs., 2)]
        /// </summary>
        private static List<FromEntry> ParseFromEntries(string fromValue)
        {
            var entries = new List<FromEntry>();
            string[] tokens = fromValue.Split('|');

            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];
                if (token.Length > 0 && token[0] == '#')
                {
                    // Alternate value — attach to the previous entry
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

        // -----------------------------------------------------------------------
        // Clean FROM generation
        // -----------------------------------------------------------------------

        /// <summary>
        /// Generates the clean FROM('...') replacement, one entry per line,
        /// with &| continuation markers aligned at the same column.
        /// </summary>
        private static string GenerateCleanFrom(List<FromEntry> entries, string contIndent)
        {
            // Build content string for each entry (including trailing pipe separator)
            var contents = new string[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                string s = EscapeString(entries[i].Display);
                if (entries[i].Value != null)
                    s += "|#" + EscapeString(entries[i].Value);
                if (i < entries.Count - 1)
                    s += "|"; // pipe separator before next entry
                contents[i] = s;
            }

            // Max content length across non-last lines (last has no &|)
            int maxLen = 0;
            for (int i = 0; i < entries.Count - 1; i++)
                if (contents[i].Length > maxLen) maxLen = contents[i].Length;

            var sb = new StringBuilder();
            sb.Append("FROM('");

            for (int i = 0; i < entries.Count; i++)
            {
                bool isLast = (i == entries.Count - 1);

                if (i > 0)
                    sb.Append(contIndent).Append("'");

                sb.Append(contents[i]);

                if (!isLast)
                {
                    sb.Append("'"); // close quote first
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

        private static string EscapeString(string s)
        {
            return s.Replace("'", "''");
        }

        // -----------------------------------------------------------------------
        // Block navigation (mirrors CleanListFormatCommand)
        // -----------------------------------------------------------------------

        private static int FindGroupStart(string[] lines, int caretLine)
        {
            int i = caretLine;
            while (i > 0 && ClarionCodeParser.HasContinuation(lines[i - 1]))
                i--;
            return i;
        }

        private static int FindGroupEnd(string[] lines, int start)
        {
            int i = start;
            while (i < lines.Length - 1 && ClarionCodeParser.HasContinuation(lines[i]))
                i++;
            return i;
        }

        // -----------------------------------------------------------------------
        // Position-tracked flatten (mirrors CleanListFormatCommand)
        // -----------------------------------------------------------------------

        internal static string BuildFlatWithMap(
            string[] lines, int start, int end, out FlatMap map)
        {
            map = new FlatMap();
            var sb = new StringBuilder();

            for (int i = start; i <= end; i++)
            {
                string line = lines[i];
                int    pipe = ClarionCodeParser.FindContinuationPipe(line);
                int    len  = pipe >= 0 ? pipe : line.Length;

                int colStart = (i > start) ? (line.Length - line.TrimStart().Length) : 0;
                if (colStart > len) colStart = len;

                string part = line.Substring(colStart, len - colStart).TrimEnd();
                if (part.Length == 0) continue;

                if (sb.Length > 0 && sb[sb.Length - 1] != ' ' && part[0] != ' ')
                    sb.Append(' ');

                map.AddSegment(sb.Length, i, colStart, part.Length);
                sb.Append(part);
            }

            return sb.ToString();
        }

        // -----------------------------------------------------------------------
        // String value extraction (mirrors CleanListFormatCommand)
        // -----------------------------------------------------------------------

        private static string ExtractStringValue(string parenContent)
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
    }
}
