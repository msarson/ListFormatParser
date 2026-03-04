using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.DefaultEditor.Gui.Editor;
using ICSharpCode.SharpDevelop.Gui;
using System.Collections.Generic;

namespace ListFormatParser
{
    /// <summary>
    /// "Parse FROM" — opens the tabbed dialog in FROM-only mode when the caret
    /// is on a SPIN, COMBO, or DROP-list that has FROM('...') but no FORMAT().
    /// If FORMAT() is also present the full dialog opens with both sections populated.
    /// </summary>
    public class ParseFromCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            var window = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow;
            if (window == null) return;
            var provider = window.ActiveViewContent as ITextEditorControlProvider;
            if (provider == null) return;

            var area  = provider.TextEditorControl.ActiveTextAreaControl;
            string[] lines = provider.TextEditorControl.Document.TextContent.Split(
                new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);

            // Try to get FROM entries
            List<FromParser.FromEntry> fromEntries;
            string useVar, error;
            if (!FromParser.TryGetFromEntries(lines, area.Caret.Line, out fromEntries, out useVar, out error))
            {
                MessageService.ShowMessage(error);
                return;
            }

            // Also try FORMAT() — may not be present (e.g. SPIN)
            List<FormatColumn> columns = new List<FormatColumn>();
            string flatLine = "";
            {
                int blockStart = FromParser.FindGroupStart(lines, area.Caret.Line);
                int blockEnd   = FromParser.FindGroupEnd(lines, blockStart);
                flatLine       = FromParser.BuildFlat(lines, blockStart, blockEnd);
                string codeOnly = ClarionCodeParser.MakeCodeOnlyLine(flatLine);

                int begAtt, begParen, endParen;
                if (ClarionCodeParser.FindAttrParen(codeOnly, "FORMAT", out begAtt, out begParen, out endParen))
                {
                    string parenContent = flatLine.Substring(begParen, endParen - begParen + 1);
                    string formatString = FromParser.ExtractStringValue(parenContent);
                    if (!string.IsNullOrEmpty(formatString))
                        columns = FormatStringParser.Parse(formatString);
                }
            }

            using (var form = new ColumnDisplayForm(columns, flatLine, fromEntries, useVar, fromFirst: true))
                form.ShowDialog();
        }
    }
}
