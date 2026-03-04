using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.DefaultEditor.Gui.Editor;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.TextEditor;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ListFormatParser
{
    /// <summary>
    /// IDE command: find the LIST control under (or near) the caret, extract its
    /// FORMAT() string, parse it into columns and show the ColumnDisplayForm.
    /// </summary>
    public class ParseListFormatCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            var window = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow;
            if (window == null) return;

            var provider = window.ActiveViewContent as ITextEditorControlProvider;
            if (provider == null) return;

            var tec      = provider.TextEditorControl;
            var doc      = tec.Document;
            var area     = tec.ActiveTextAreaControl;
            int caretLine = area.Caret.Line;

            // 1. Collect the continuation block that contains the caret
            string[] lines     = doc.TextContent.Split(new[] { "\r\n", "\r", "\n" },
                                    System.StringSplitOptions.None);
            int blockStart     = FindGroupStart(lines, caretLine);
            var blockLines     = CollectBlock(lines, blockStart);

            // 2. Flatten continuation lines into one logical line
            string flatLine    = FlattenBlock(blockLines);

            // 3. Build a code-only (string-masked) version for safe attribute scanning
            string codeOnly    = ClarionCodeParser.MakeCodeOnlyLine(flatLine);

            // 4. Locate FORMAT(...)
            int begAtt, begParen, endParen;
            if (!ClarionCodeParser.FindAttrParen(codeOnly, "FORMAT",
                    out begAtt, out begParen, out endParen))
            {
                MessageService.ShowMessage(
                    "No FORMAT() attribute found in the current line/block.\n\n" +
                    "Place the caret on or inside a LIST control that has a FORMAT() attribute.");
                return;
            }

            // 5. Extract the concatenated string value from FORMAT('...')
            string parenContent  = flatLine.Substring(begParen, endParen - begParen + 1);
            string formatString  = ExtractStringValue(parenContent);

            if (string.IsNullOrEmpty(formatString))
            {
                MessageService.ShowMessage("Could not extract the FORMAT() string value.");
                return;
            }

            // 6. Parse the FORMAT string into column definitions
            var columns = FormatStringParser.Parse(formatString);

            if (columns.Count == 0)
            {
                MessageService.ShowMessage("No columns found in the FORMAT() string.");
                return;
            }

            // 7. Also try to parse FROM() if present (populates the FROM tab)
            List<FromParser.FromEntry> fromEntries = null;
            string useVar = null;
            {
                string dummy;
                FromParser.TryGetFromEntries(lines, caretLine, out fromEntries, out useVar, out dummy);
                // If FROM() is a queue-form (not a string literal), TryGetFromEntries returns false — fine
            }

            // 8. Show the result dialog
            using (var form = new ColumnDisplayForm(columns, flatLine, fromEntries, useVar))
                form.ShowDialog();
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        /// <summary>Walk backwards from caretLine to find the first line of the continuation block.</summary>
        private static int FindGroupStart(string[] lines, int caretLine)
        {
            int i = caretLine;
            while (i > 0 && ClarionCodeParser.HasContinuation(lines[i - 1]))
                i--;
            return i;
        }

        /// <summary>Collect lines starting at blockStart until one has no continuation pipe.</summary>
        private static List<string> CollectBlock(string[] lines, int start)
        {
            var block = new List<string>();
            for (int i = start; i < lines.Length; i++)
            {
                block.Add(lines[i]);
                if (!ClarionCodeParser.HasContinuation(lines[i])) break;
            }
            return block;
        }

        /// <summary>
        /// Joins continuation lines into one, stripping the | and trailing content,
        /// then collapses adjacent string literals ('a' & 'b' → 'ab').
        /// </summary>
        private static string FlattenBlock(List<string> blockLines)
        {
            var sb = new StringBuilder();
            foreach (string line in blockLines)
            {
                int pipePos = ClarionCodeParser.FindContinuationPipe(line);
                string part = pipePos >= 0 ? line.Substring(0, pipePos).TrimEnd() : line;

                if (sb.Length > 0 && part.Length > 0)
                {
                    string joined = sb.ToString().TrimEnd() + " " + part.TrimStart();
                    sb.Clear();
                    sb.Append(CollapseStringConcat(joined));
                }
                else
                {
                    sb.Append(part);
                }
            }
            return sb.ToString();
        }

        private static string CollapseStringConcat(string s)
        {
            string prev = null;
            while (prev != s) { prev = s; s = Regex.Replace(s, @"'\s*&\s*'", ""); }
            return s;
        }

        /// <summary>
        /// Extracts and concatenates all string literal values from within a
        /// paren expression such as <c>('51L(2)|...' & '...more...')</c>.
        /// Handles Clarion's doubled-quote escape ('' = literal quote).
        /// </summary>
        private static string ExtractStringValue(string parenContent)
        {
            var  sb     = new StringBuilder();
            bool inStr  = false;
            for (int i = 0; i < parenContent.Length; i++)
            {
                char c = parenContent[i];
                if (!inStr)
                {
                    if (c == '\'') inStr = true;
                }
                else
                {
                    if (c == '\'' && i + 1 < parenContent.Length && parenContent[i + 1] == '\'')
                    { sb.Append('\''); i++; }
                    else if (c == '\'')
                    { inStr = false; }
                    else
                    { sb.Append(c); }
                }
            }
            return sb.ToString();
        }
    }
}
