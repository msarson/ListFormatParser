using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Gui;

namespace ListFormatParser
{
    /// <summary>
    /// Opens the Queue/File → FORMAT generator dialog.
    /// Always available — not gated on cursor position.
    /// </summary>
    public class Queue2FormatCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            var mainForm = WorkbenchSingleton.MainForm;
            if (mainForm == null) return;
            using (var form = new Queue2FormatForm())
                form.ShowDialog(mainForm);
        }
    }
}
