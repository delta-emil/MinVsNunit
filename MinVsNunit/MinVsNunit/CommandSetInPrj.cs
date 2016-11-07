using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace MinVsNunit
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class CommandSetInPrj
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandRunId = 0x0100;
        public const int CommandDebugId = 0x0101;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("de43d31d-a22c-4e5f-8071-8b35e079bace");
        public static readonly Guid CommandSetMulti = new Guid("c6c177a8-d764-47e5-95ed-03d868decb6e");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly PackageMinVsNunit package;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandSetInPrj"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private CommandSetInPrj(PackageMinVsNunit package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            var commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                this.AddMenuItem(commandService, CommandSet, CommandRunId, attachDebugger: false);
                this.AddMenuItem(commandService, CommandSet, CommandDebugId, attachDebugger: true);
                this.AddMenuItem(commandService, CommandSetMulti, CommandRunId, attachDebugger: false);
                this.AddMenuItem(commandService, CommandSetMulti, CommandDebugId, attachDebugger: true);
            }
        }

        private void AddMenuItem(OleMenuCommandService commandService, Guid commandSet, int id, bool attachDebugger)
        {
            var menuItem = new OleMenuCommand((o, e) => this.MenuItemCallback(attachDebugger: attachDebugger), new CommandID(commandSet, id));
            menuItem.BeforeQueryStatus += (o, e) => MenuItemBeforeQueryStatus(menuItem);
            commandService.AddCommand(menuItem);
        }

        private void MenuItemBeforeQueryStatus(OleMenuCommand menuItem)
        {
            var selectedProjects = (Array)this.package.DTE.ActiveSolutionProjects;
            foreach (Project project in selectedProjects)
            {
                if (Utils.IsTestProject(project))
                {
                    menuItem.Visible = true;
                    return;
                }
            }

            menuItem.Visible = false;
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static CommandSetInPrj Instance
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
            Instance = new CommandSetInPrj(package);
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

            var selectedProjects = (Array)this.package.DTE.ActiveSolutionProjects;
            var selectedTestProjects = new List<Project>(selectedProjects.Length);
            foreach (Project project in selectedProjects)
            {
                if (Utils.IsTestProject(project))
                {
                    selectedTestProjects.Add(project);
                }
            }

            if (selectedTestProjects.Count == 0)
            {
                return;
            }

            var assemblyPaths = new List<string>(selectedTestProjects.Count);
            foreach (Project project in selectedTestProjects)
            {
                Utils.BuildProject(this.package.DTE.Solution.SolutionBuild, project);
                assemblyPaths.Add(Utils.GetAssemblyPath(project));
            }

            if (!this.package.ProcessManager.StartNunit(nunitRunnerPath, assemblyPaths, null, attachDebugger))
            {
                MessageBox.Show("A test is aleady running.");
            }
        }
    }
}
