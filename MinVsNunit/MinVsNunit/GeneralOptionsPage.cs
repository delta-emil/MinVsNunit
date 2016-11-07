using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace MinVsNunit
{
    public class GeneralOptionsPage : DialogPage
    {
        public GeneralOptionsPage()
        {
            this.NunitRunnerPath = string.Empty;
        }

        [Category("General")]
        [DisplayName("NUnit runner path")]
        [Description("The path to nunit-console.exe or nunit-console-x86.exe.")]
        public string NunitRunnerPath { get; set; }
    }
}
