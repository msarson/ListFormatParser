using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.DefaultEditor.Gui.Editor;
using ICSharpCode.SharpDevelop.Gui;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace ListFormatParser
{
    /// <summary>
    /// "Copy FROM as CASE" — parses FROM('...') and copies a CASE statement to the clipboard.
    ///
    ///   CASE UseVar
    ///   OF '1'    !  1  Mr.
    ///   OF '2'    !  2  Mrs.
    ///   END
    ///   !Choices: 'Mr.','Mrs.','Ms.','Dr.'
    ///   !Values:  '1','2','3','4'
    /// </summary>
    public class CopyFromCaseCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            var window = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow;
            if (window == null) return;
            var provider = window.ActiveViewContent as ITextEditorControlProvider;
            if (provider == null) return;

            var area = provider.TextEditorControl.ActiveTextAreaControl;
            string[] lines = provider.TextEditorControl.Document.TextContent.Split(
                new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);

            List<FromParser.FromEntry> entries;
            string useVar, error;
            if (!FromParser.TryGetFromEntries(lines, area.Caret.Line, out entries, out useVar, out error))
            {
                MessageService.ShowMessage(error);
                return;
            }

            string code = GenerateCaseCode(entries, useVar);
            Clipboard.SetText(code);
            MessageService.ShowMessage(code, "Copy FROM as CASE");
        }

        internal static string GenerateCaseCode(List<FromParser.FromEntry> entries, string useVar)
        {
            var sb        = new StringBuilder();

            int maxValLen = 0;
            foreach (var e in entries)
            {
                string v = e.Value ?? e.Display;
                if (v.Length > maxValLen) maxValLen = v.Length;
            }

            sb.AppendLine("CASE " + useVar);

            // Build per-column widths for aligned !Choices / !Values lines
            var labelCols = new string[entries.Count];
            var valueCols = new string[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                labelCols[i] = "'" + entries[i].Display + "'";
                valueCols[i] = "'" + (entries[i].Value ?? entries[i].Display) + "'";
            }
            bool hasAlternates = false;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].Value != null) { hasAlternates = true; break; }

            for (int i = 0; i < entries.Count; i++)
            {
                string label = entries[i].Display;
                string value = entries[i].Value ?? entries[i].Display;
                int    pad   = maxValLen - value.Length + 2;

                sb.Append("OF '").Append(value).Append("'");
                sb.Append(new string(' ', pad < 2 ? 2 : pad));
                sb.Append("!").Append(System.String.Format("{0,3}", i + 1)).Append("  ").AppendLine(label);
            }

            sb.AppendLine("END");
            sb.AppendLine();

            // Build aligned !Choices / !Values lines
            var choicesLine = new StringBuilder("!Choices: ");
            var valuesLine  = new StringBuilder("!Values:  ");
            for (int i = 0; i < entries.Count; i++)
            {
                bool   isLast  = (i == entries.Count - 1);
                string lToken  = labelCols[i] + (isLast ? "" : ",");
                string vToken  = valueCols[i] + (isLast ? "" : ",");
                int    colWidth = System.Math.Max(lToken.Length, vToken.Length);
                choicesLine.Append(lToken.PadRight(colWidth));
                valuesLine.Append(vToken.PadRight(colWidth));
            }

            sb.AppendLine(choicesLine.ToString().TrimEnd());
            if (hasAlternates)
                sb.AppendLine(valuesLine.ToString().TrimEnd());

            return sb.ToString().TrimEnd();
        }
    }
}
