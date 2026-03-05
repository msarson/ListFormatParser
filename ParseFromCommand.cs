using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.DefaultEditor.Gui.Editor;
using ICSharpCode.SharpDevelop.Gui;
using System;
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

            var tec  = provider.TextEditorControl;
            var area = tec.ActiveTextAreaControl;
            string[] lines = tec.Document.TextContent.Split(
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
            int blockStart, blockEnd;
            {
                blockStart = FromParser.FindGroupStart(lines, area.Caret.Line);
                blockEnd   = FromParser.FindGroupEnd(lines, blockStart);
                flatLine   = FromParser.BuildFlat(lines, blockStart, blockEnd);
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

            // Build write-back delegate — replaces FROM('...') in the editor at dialog commit
            var capturedTec   = tec;
            int capturedStart = blockStart;
            int capturedEnd   = blockEnd;
            Action<string> writeBack = (newFromStr) =>
            {
                var doc = capturedTec.Document;
                string[] current = doc.TextContent.Split(
                    new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
                FlatMap map;
                string flat = CleanFromCommand.BuildFlatWithMap(current, capturedStart, capturedEnd, out map);
                string code = ClarionCodeParser.MakeCodeOnlyLine(flat);
                int ba, bp, ep;
                if (!ClarionCodeParser.FindAttrParen(code, "FROM", out ba, out bp, out ep)) return;
                int off0    = map.FlatToDocOffset(ba, doc);
                int off1    = map.FlatToDocOffset(ep, doc) + 1;
                if (off0 < 0 || off1 <= off0) return;
                int fromCol = map.FlatToDocColumn(ba);
                int indent  = (fromCol >= 0 ? fromCol : 0) + 5; // FROM( = 5 chars to opening quote
                // newFromStr already contains the entries as a FROM string at default indent;
                // re-parse entries then regenerate with the correct source indent
                var entries = FromParser.ParseFromString(newFromStr);
                string aligned = FromParser.GenerateFromLines(entries, indent);
                doc.UndoStack.StartUndoGroup();
                doc.Replace(off0, off1 - off0, aligned);
                doc.UndoStack.EndUndoGroup();
                capturedTec.Refresh();
            };

            using (var form = new ColumnDisplayForm(columns, flatLine, fromEntries, useVar,
                                                    fromFirst: true, writeBackFrom: writeBack))
                form.ShowDialog();
        }
    }
}
