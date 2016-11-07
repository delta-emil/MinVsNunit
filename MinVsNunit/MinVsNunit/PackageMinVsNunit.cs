using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MinVsNunit
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageMinVsNunit.PackageGuidString)]
    [ProvideOptionPage(typeof(GeneralOptionsPage), "MinVsNunit", "General", 110, 120, false)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class PackageMinVsNunit : Package
    {
        /// <summary>
        /// FirstCommandPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "9909ae46-70b4-4881-9b9e-4a03b47eda14";
        
        private DTE2 dte;
        private OutputWindowPane outputWindowPane;
        private ProcessManager processManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandSetInEditor"/> class.
        /// </summary>
        public PackageMinVsNunit()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        public DTE2 DTE
        {
            get { return this.dte; }
        }

        public OutputWindowPane OutputWindowPane
        {
            get { return this.outputWindowPane; }
        }

        public ProcessManager ProcessManager
        {
            get { return this.processManager; }
        }

        public GeneralOptionsPage GetGeneralOptionsPage()
        {
            return (GeneralOptionsPage)this.GetDialogPage(typeof(GeneralOptionsPage));
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            this.dte = (DTE2)this.GetService(typeof(DTE));

            this.outputWindowPane = this.dte.ToolWindows.OutputWindow.OutputWindowPanes.Add("MinVsNunit");

            this.processManager = new ProcessManager(this.outputWindowPane, (Debugger2)this.dte.Debugger);

            CommandSetInEditor.Initialize(this);
            CommandSetInPrj.Initialize(this);
            CommandSetInSln.Initialize(this);

            base.Initialize();
        }
    }
}
