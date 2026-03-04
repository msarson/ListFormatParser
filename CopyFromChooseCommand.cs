using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.DefaultEditor.Gui.Editor;
using ICSharpCode.SharpDevelop.Gui;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace ListFormatParser
{
    /// <summary>
    /// "Copy FROM as CHOOSE" — parses FROM('...') and copies a CHOOSE() call to the clipboard.
    ///
    ///   CHOOSE(UseVar,'Mr.','Mrs.','Ms.','Dr.')
    /// </summary>
    public class CopyFromChooseCommand : AbstractMenuCommand
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

            string code = GenerateChooseCode(entries, useVar);
            Clipboard.SetText(code);
            MessageService.ShowMessage(code, "Copy FROM as CHOOSE");
        }

        private static string GenerateChooseCode(List<FromParser.FromEntry> entries, string useVar)
        {
            var sb = new StringBuilder();
            sb.Append("CHOOSE(").Append(useVar);
            foreach (var e in entries)
                sb.Append(",'").Append(e.Display).Append("'");
            sb.Append(")");
            return sb.ToString();
        }
    }
}
