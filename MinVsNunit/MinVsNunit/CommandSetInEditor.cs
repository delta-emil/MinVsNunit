using System;
using System.ComponentModel.Design;
using System.IO;
using System.Windows;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace MinVsNunit
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class CommandSetInEditor
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandRunId = 0x0100;
        public const int CommandDebugId = 0x0101;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("32c62c4a-adbd-4572-a2ff-4912e5c08bc6");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly PackageMinVsNunit package;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandSetInEditor"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private CommandSetInEditor(PackageMinVsNunit package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            var commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                this.AddMenuItem(commandService, CommandRunId, attachDebugger: false);
                this.AddMenuItem(commandService, CommandDebugId, attachDebugger: true);
            }
        }

        private void AddMenuItem(OleMenuCommandService commandService, int id, bool attachDebugger)
        {
            var menuItem = new OleMenuCommand((o, e) => this.MenuItemCallback(attachDebugger: attachDebugger), new CommandID(CommandSet, id));
            menuItem.BeforeQueryStatus +=
                (o, e) => menuItem.Visible = this.package.DTE.ActiveDocument.ProjectItem.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp;
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static CommandSetInEditor Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(PackageMinVsNunit package)
        {
            Instance = new CommandSetInEditor(package);
        }
        
        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        private void MenuItemCallback(bool attachDebugger)
        {
            var nunitRunnerPath = this.package.GetGeneralOptionsPage().NunitRunnerPath;
            if (string.IsNullOrEmpty(nunitRunnerPath) || !File.Exists(nunitRunnerPath))
            {
                MessageBox.Show("Please provide a valid path to nunit-console.exe or nunit-console-x86.exe in the options.");
                return;
            }

            var activeDocument = this.package.DTE.ActiveDocument;

            string testToRun = this.GetTestToRun(activeDocument);
            if (testToRun == null)
            {
                MessageBox.Show("Not inside a test or test fixture.");
                return;
            }

            string assemblyPath = Utils.GetAssemblyPath(activeDocument.ProjectItem.ContainingProject);

            Utils.BuildProject(this.package.DTE.Solution.SolutionBuild, activeDocument.ProjectItem.ContainingProject);

            if (!this.package.ProcessManager.StartNunit(nunitRunnerPath, new[] { assemblyPath }, testToRun, attachDebugger))
            {
                MessageBox.Show("A test is aleady running.");
            }
        }

        private string GetTestToRun(Document activeDocument)
        {
            var sel = (TextSelection)activeDocument.Selection;

            {
                var fun = (CodeFunction2)sel.ActivePoint.get_CodeElement(vsCMElement.vsCMElementFunction);
                if (fun != null
                    && !this.HasAttribute(fun.Attributes, "NUnit.Framework.TestAttribute")
                    && !this.HasAttribute(fun.Attributes, "NUnit.Framework.TestCaseAttribute"))
                {
                    fun = null;
                }

                if (fun != null)
                {
                    //MessageBox.Show("Test method: " + fun.FullName);
                    return fun.FullName;
                }
            }

            {
                var @class = (CodeClass2)sel.ActivePoint.get_CodeElement(vsCMElement.vsCMElementClass);
                if (@class != null && !this.HasAttribute(@class.Attributes, "NUnit.Framework.TestFixtureAttribute"))
                {
                    @class = null;
                }

                if (@class != null)
                {
                    //MessageBox.Show("Test fixture: " + @class.FullName);
                    return @class.FullName;
                }
            }

            {
                var @namespace = (CodeNamespace)sel.ActivePoint.get_CodeElement(vsCMElement.vsCMElementNamespace);
                if (@namespace != null)
                {
                    bool found = false;
                    foreach (CodeElement2 element in @namespace.Members)
                    {
                        if (element.Kind == vsCMElement.vsCMElementClass)
                        {
                            var @class = (CodeClass2)element;
                            if (this.HasAttribute(@class.Attributes, "NUnit.Framework.TestFixtureAttribute"))
                            {
                                found = true;
                                break;
                            }
                        }
                    }

                    if (found == false)
                    {
                        @namespace = null;
                    }
                }

                if (@namespace != null)
                {
                    //MessageBox.Show("Test suite: " + @namespace.FullName);
                    return @namespace.FullName;
                }
            }

            return null;
        }

        private bool HasAttribute(CodeElements attributes, string attributeFullName)
        {
            foreach (CodeAttribute2 attr in attributes)
            {
                if (attr.FullName == attributeFullName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
