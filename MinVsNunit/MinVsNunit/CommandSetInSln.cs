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
    internal sealed class CommandSetInSln
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandRunId = 0x0100;
        public const int CommandDebugId = 0x0101;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("25a13c8a-2fcc-4973-9a3e-9f5dfae5f0e0");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly PackageMinVsNunit package;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandSetInSln"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private CommandSetInSln(PackageMinVsNunit package)
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
            menuItem.BeforeQueryStatus += (o, e) => MenuItemBeforeQueryStatus(menuItem);
            commandService.AddCommand(menuItem);
        }

        private void MenuItemBeforeQueryStatus(OleMenuCommand menuItem)
        {
            foreach (Project project in this.package.DTE.Solution.Projects)
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
        public static CommandSetInSln Instance
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
            Instance = new CommandSetInSln(package);
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

            var selectedProjects = this.package.DTE.Solution.Projects;
            var selectedTestProjects = new List<Project>(selectedProjects.Count);
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
