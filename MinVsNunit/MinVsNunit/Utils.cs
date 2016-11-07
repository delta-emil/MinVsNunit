using System;
using System.IO;
using EnvDTE;
using VSLangProj;

namespace MinVsNunit
{
    internal static class Utils
    {
        public static string GetAssemblyPath(Project project)
        {
            string fullPath = project.Properties.Item("FullPath").Value.ToString();
            string outputPath = project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();
            string outputFileName = project.Properties.Item("OutputFileName").Value.ToString();
            return Path.Combine(fullPath, outputPath, outputFileName);
        }

        public static bool IsTestProject(Project project)
        {
            if (project.CodeModel.Language != CodeModelLanguageConstants.vsCMLanguageCSharp)
            {
                return false;
            }

            var vsProject = (VSProject)project.Object;
            foreach (Reference reference in vsProject.References)
            {
                if (string.Equals(reference.Name, "nunit.framework", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static void BuildProject(SolutionBuild solutionBuild, Project project)
        {
            solutionBuild.BuildProject(
                solutionBuild.ActiveConfiguration.Name,
                project.UniqueName,
                WaitForBuildToFinish: true);
        }
    }
}
