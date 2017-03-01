using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Evaluation;
using System.Linq;
using Microsoft.Build.Construction;
using System.Text;
using System.Xml;

namespace Microsoft.DotNet.Build.Tasks
{
    public class UpdateVSConfigurations : BuildTask
    {
        public ITaskItem[] ProjectsToUpdate { get; set; }

        public ITaskItem[] SolutionsToUpdate { get; set; }

        private const string ConfigurationPropsFilename = "Configurations.props";
        private static Regex s_configurationConditionRegex = new Regex(@"'\$\(Configuration\)\|\$\(Platform\)' ?== ?'(?<config>.*)'");
        private static string[] s_configurationSuffixes = new [] { "Debug|AnyCPU", "Release|AnyCPU" };

        public override bool Execute()
        {
            if (ProjectsToUpdate == null) ProjectsToUpdate = new ITaskItem[0];
            if (SolutionsToUpdate == null) SolutionsToUpdate = new ITaskItem[0];

            foreach (var item in ProjectsToUpdate)
            {
                string projectFile = item.ItemSpec;
                string projectConfigurationPropsFile = Path.Combine(Path.GetDirectoryName(projectFile), ConfigurationPropsFilename);

                string[] expectedConfigurations = s_configurationSuffixes;
                if (File.Exists(projectConfigurationPropsFile))
                {
                    expectedConfigurations = GetConfigurationStrings(projectConfigurationPropsFile);
                }

                var project = ProjectRootElement.Open(projectFile);
                ICollection<ProjectPropertyGroupElement> propertyGroups;
                var actualConfigurations = GetConfigurationFromPropertyGroups(project, out propertyGroups);

                bool addedGuid = EnsureProjectGuid(project);

                if (!actualConfigurations.SequenceEqual(expectedConfigurations))
                {
                    ReplaceConfigurationPropertyGroups(project, propertyGroups, expectedConfigurations);
                }

                if (addedGuid || !actualConfigurations.SequenceEqual(expectedConfigurations))
                {
                    project.Save();
                }
            }

            foreach (var solutionRoot in SolutionsToUpdate)
            {
                UpdateSolution(solutionRoot);
            }

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets a sorted list of configuration strings from a Configurations.props file
        /// </summary>
        /// <param name="configurationProjectFile">Path to Configuration.props file</param>
        /// <returns>Sorted list of configuration strings</returns>
        private static string[] GetConfigurationStrings(string configurationProjectFile, bool addSuffixes = true)
        {
            var configurationProject = new Project(configurationProjectFile);

            var buildConfigurations = configurationProject.GetPropertyValue("BuildConfigurations");

            ProjectCollection.GlobalProjectCollection.UnloadProject(configurationProject);

            var configurations = buildConfigurations.Trim()
                                      .Split(';')
                                      .Select(c => c.Trim())
                                      .Where(c => !String.IsNullOrEmpty(c));

            if (addSuffixes)
            {
                configurations = configurations.SelectMany(c => s_configurationSuffixes.Select(s => c + "-" + s));
            }

            return configurations.OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        /// <summary>
        /// Gets a sorted list of configuration strings from a project file's PropertyGroups
        /// </summary>
        /// <param name="project">Project</param>
        /// <param name="propertyGroups">collection that accepts the list of property groups representing configuration strings</param>
        /// <returns>Sorted list of configuration strings</returns>
        private static string[] GetConfigurationFromPropertyGroups(ProjectRootElement project, out ICollection<ProjectPropertyGroupElement> propertyGroups)
        {
            propertyGroups = new List<ProjectPropertyGroupElement>();
            var configurations = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var propertyGroup in project.PropertyGroups)
            {
                var match = s_configurationConditionRegex.Match(propertyGroup.Condition);

                if (match.Success)
                {
                    configurations.Add(match.Groups["config"].Value);
                    propertyGroups.Add(propertyGroup);
                }
            }

            return configurations.ToArray();
        }

        /// <summary>
        /// Replaces all configuration propertygroups with empty property groups corresponding to the expected configurations.
        /// Doesn't attempt to preserve any content since it can all be regenerated.
        /// Does attempt to preserve the ordering in the project file.
        /// </summary>
        /// <param name="project">Project</param>
        /// <param name="oldPropertyGroups">PropertyGroups to remove</param>
        /// <param name="newConfigurations"></param>
        private static void ReplaceConfigurationPropertyGroups(ProjectRootElement project, IEnumerable<ProjectPropertyGroupElement> oldPropertyGroups, IEnumerable<string> newConfigurations)
        {
            ProjectElement insertAfter = null, insertBefore = null;

            foreach (var oldPropertyGroup in oldPropertyGroups)
            {
                insertBefore = oldPropertyGroup.NextSibling;
                project.RemoveChild(oldPropertyGroup);
            }

            if (insertBefore == null)
            {
                // find first itemgroup after imports
                var insertAt = project.Imports.FirstOrDefault()?.NextSibling;

                while (insertAt != null)
                {
                    if (insertAt is ProjectItemGroupElement)
                    {
                        insertBefore = insertAt;
                        break;
                    }

                    insertAt = insertAt.NextSibling;
                }
            }

            if (insertBefore == null)
            {
                // find last propertygroup after imports, defaulting to after imports
                insertAfter = project.Imports.FirstOrDefault();

                while (insertAfter?.NextSibling != null && insertAfter.NextSibling is ProjectPropertyGroupElement)
                {
                    insertAfter = insertAfter.NextSibling;
                }
            }

            foreach (var newConfiguration in newConfigurations)
            {
                var newPropertyGroup = project.CreatePropertyGroupElement();
                newPropertyGroup.Condition = $"'$(Configuration)|$(Platform)' == '{newConfiguration}'";
                if (insertBefore != null)
                {
                    project.InsertBeforeChild(newPropertyGroup, insertBefore);
                }
                else if (insertAfter != null)
                {
                    project.InsertAfterChild(newPropertyGroup, insertAfter);
                }
                else
                {
                    project.AppendChild(newPropertyGroup);
                }
                insertBefore = null;
                insertAfter = newPropertyGroup;
            }
        }

        private static Dictionary<string, string> _guidMap = new Dictionary<string, string>();

        private bool EnsureProjectGuid(ProjectRootElement project)
        {
            ProjectPropertyElement projectGuid = project.Properties.FirstOrDefault(p => p.Name == "ProjectGuid");
            string guid = string.Empty;

            if (projectGuid != null)
            {
                guid = projectGuid.Value;
                string projectName;
                if (_guidMap.TryGetValue(guid, out projectName))
                {
                    Log.LogMessage($"The ProjectGuid='{guid}' is duplicated across projects '{projectName}' and '{project.FullPath}', so creating a new one for project '{project.FullPath}'");
                    guid = Guid.NewGuid().ToString("B").ToUpper();
                    _guidMap.Add(guid, project.FullPath);
                    projectGuid.Value = guid;
                    return true;
                }
                else
                {
                    _guidMap.Add(guid, project.FullPath);
                }
            }

            if (projectGuid == null)
            {
                guid = Guid.NewGuid().ToString("B").ToUpper();

                var propertyGroup = project.Imports.FirstOrDefault()?.NextSibling as ProjectPropertyGroupElement;

                if (propertyGroup == null || !string.IsNullOrEmpty(propertyGroup.Condition))
                {
                    propertyGroup = project.CreatePropertyGroupElement();
                    project.InsertAfterChild(propertyGroup, project.Imports.First());
                }

                propertyGroup.AddProperty("ProjectGuid", guid);
                return true;
            }

            return false;
        }

        private void UpdateSolution(ITaskItem solutionRootItem)
        {
            string solutionRootPath = Path.GetFullPath(solutionRootItem.ItemSpec);
            string projectExclude = solutionRootItem.GetMetadata("ExcludePattern");
            List<ProjectFolder> projectFolders = new List<ProjectFolder>();

            if (!solutionRootPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                solutionRootPath += Path.DirectorySeparatorChar;
            }

            ProjectFolder testFolder = new ProjectFolder(solutionRootPath, "tests", "{1A2F9F4A-A032-433E-B914-ADD5992BB178}", projectExclude, true);
            if (testFolder.FolderExists)
            {
                projectFolders.Add(testFolder);
            }

            ProjectFolder srcFolder = new ProjectFolder(solutionRootPath, "src", "{E107E9C1-E893-4E87-987E-04EF0DCEAEFD}", projectExclude);
            if (srcFolder.FolderExists)
            {
                testFolder.DependsOn.Add(srcFolder);
                projectFolders.Add(srcFolder);
            };

            ProjectFolder refFolder = new ProjectFolder(solutionRootPath, "ref", "{2E666815-2EDB-464B-9DF6-380BF4789AD4}", projectExclude);
            if (refFolder.FolderExists)
            {
                srcFolder.DependsOn.Add(refFolder);
                projectFolders.Add(refFolder);
            }

            if (projectFolders.Count == 0)
            {
                Log.LogMessage($"Directory '{solutionRootPath}' does not contain a 'src', 'tests', or 'ref' directory so skipping solution generation.");
                return;
            }

            Log.LogMessage($"Generating solution for '{solutionRootPath}'...");

            StringBuilder slnBuilder = new StringBuilder();
            slnBuilder.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
            slnBuilder.AppendLine("# Visual Studio 14");
            slnBuilder.AppendLine("VisualStudioVersion = 14.0.25420.1");
            slnBuilder.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");


            // Output project items
            foreach (var projectFolder in projectFolders)
            {
                foreach (var slnProject in projectFolder.Projects)
                {
                    string projectName = Path.GetFileNameWithoutExtension(slnProject.ProjectPath);
                    // Normalize the directory separators to the windows version given these are projects for VS and only work on windows.
                    string relativePathFromCurrentDirectory = slnProject.ProjectPath.Replace(solutionRootPath, "").Replace("/", "\\");

                    slnBuilder.AppendLine($"Project(\"{slnProject.SolutionGuid}\") = \"{projectName}\", \"{relativePathFromCurrentDirectory}\", \"{slnProject.ProjectGuid}\"");

                    bool writeEndProjectSection = false;
                    foreach (var dependentFolder in projectFolder.DependsOn)
                    {
                        foreach (var depProject in dependentFolder.Projects)
                        {
                            string depProjectId = depProject.ProjectGuid;
                            slnBuilder.AppendLine(
                                $"\tProjectSection(ProjectDependencies) = postProject\r\n\t\t{depProjectId} = {depProjectId}");
                            writeEndProjectSection = true;
                        }
                    }
                    if (writeEndProjectSection)
                    {
                        slnBuilder.AppendLine("\tEndProjectSection");
                    }

                    slnBuilder.AppendLine("EndProject");
                }
            }

            // Output the solution folder items
            foreach (var projectFolder in projectFolders)
            {
                slnBuilder.AppendLine($"Project(\"{projectFolder.SolutionGuid}\") = \"{projectFolder.Name}\", \"{projectFolder.Name}\", \"{projectFolder.ProjectGuid}\"\r\nEndProject");
            }

            string anyCPU = "Any CPU";
            string slnDebug = "Debug|" + anyCPU;
            string slnRelease = "Release|" + anyCPU;

            // Output the solution configurations
            slnBuilder.AppendLine("Global");
            slnBuilder.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
            slnBuilder.AppendLine($"\t\t{slnDebug} = {slnDebug}");
            slnBuilder.AppendLine($"\t\t{slnRelease} = {slnRelease}");
            slnBuilder.AppendLine("\tEndGlobalSection");

            slnBuilder.AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");

            // Output the solution to project configuration mappings
            foreach (var projectFolder in projectFolders)
            {
                foreach (var slnProject in projectFolder.Projects)
                {
                    string projectConfig = slnProject.GetBestConfiguration("netcoreapp-Windows_NT");
                    if (!string.IsNullOrEmpty(projectConfig))
                    {
                        projectConfig += "-";
                    }
                    string[] slnConfigs = new string[] { slnDebug, slnRelease };
                    string[] markers = new string[] { "ActiveCfg", "Build.0" };

                    foreach (string slnConfig in slnConfigs)
                    {
                        foreach (string marker in markers)
                        {
                            slnBuilder.AppendLine($"\t\t{slnProject.ProjectGuid}.{slnConfig}.{marker} = {projectConfig}{slnConfig}");
                        }
                    }
                }
            }

            slnBuilder.AppendLine("\tEndGlobalSection");
            slnBuilder.AppendLine("\tGlobalSection(SolutionProperties) = preSolution");
            slnBuilder.AppendLine("\t\tHideSolutionNode = FALSE");
            slnBuilder.AppendLine("\tEndGlobalSection");

            // Output the project to solution folder mappings
            slnBuilder.AppendLine("\tGlobalSection(NestedProjects) = preSolution");
            foreach (var projectFolder in projectFolders)
            {
                foreach (var slnProject in projectFolder.Projects)
                {
                    slnBuilder.AppendLine($"\t\t{slnProject.ProjectGuid} = {projectFolder.ProjectGuid}");
                }
            }
            slnBuilder.AppendLine("\tEndGlobalSection");

            slnBuilder.AppendLine("EndGlobal");

            string solutionName = GetNameForSolution(solutionRootPath);
            string slnFile = Path.Combine(solutionRootPath, solutionName + ".sln");
            File.WriteAllText(slnFile, slnBuilder.ToString());
        }

        private static string GetNameForSolution(string path)
        {
            if (path.Length < 0)
                throw new ArgumentException("Invalid base bath for solution", nameof(path));

            if (path[path.Length - 1] == Path.DirectorySeparatorChar || path[path.Length - 1] == Path.AltDirectorySeparatorChar)
            {
                return GetNameForSolution(path.Substring(0, path.Length - 1));
            }
            return Path.GetFileName(path);           
        }

        internal class ProjectFolder
        {
            public string Name { get; }
            public string ProjectGuid { get; }
            public string SolutionGuid { get { return "{2150E333-8FDC-42A3-9474-1A3956D46DE8}"; } }
            public string ProjectFolderPath { get; }
            public bool InUse { get; set; }
            public List<ProjectFolder> DependsOn { get; set; } = new List<ProjectFolder>();

            public bool FolderExists { get; }

            public List<SolutionProject> Projects { get; }

            public ProjectFolder(string basePath, string relPath, string projectId, string projectExcludePattern, bool searchRecursively = false)
            {
                Name = relPath;
                ProjectGuid = projectId;
                ProjectFolderPath = Path.Combine(basePath, relPath);
                FolderExists = Directory.Exists(ProjectFolderPath);
                Projects = new List<SolutionProject>();

                if (FolderExists)
                {
                    SearchOption searchOption = searchRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                    Regex excludePattern = string.IsNullOrEmpty(projectExcludePattern) ? null : new Regex(projectExcludePattern);
                    string primaryProjectPrefix = Path.Combine(ProjectFolderPath, GetNameForSolution(basePath) + "." + relPath);
                    foreach (string proj in Directory.EnumerateFiles(ProjectFolderPath, "*proj", searchOption).OrderBy(p => p))
                    {
                        if (excludePattern == null || !excludePattern.IsMatch(proj))
                        {
                            if (proj.StartsWith(primaryProjectPrefix, StringComparison.OrdinalIgnoreCase))
                            {
                                // Always put the primary project first in the list
                                Projects.Insert(0, new SolutionProject(proj));
                            }
                            else
                            {
                                Projects.Add(new SolutionProject(proj));
                            }
                        }
                    }
                }
            }
        }

        internal class SolutionProject
        {
            public string ProjectPath { get; }
            public string ProjectGuid { get; }
            public string[] Configurations { get; set; }

            public SolutionProject(string projectPath)
            {
                ProjectPath = projectPath;
                ProjectGuid = ReadProjectGuid(projectPath);
                string configurationProps = Path.Combine(Path.GetDirectoryName(projectPath), "Configurations.props");
                if (File.Exists(configurationProps))
                {
                    Configurations = GetConfigurationStrings(configurationProps, addSuffixes:false);
                }
                else
                {
                    Configurations = new string[0];
                }
            }

            public string GetBestConfiguration(string buildConfiguration)
            {
                //TODO: We should use the FindBestConfigutation logic from the build tasks
                var match = Configurations.FirstOrDefault(c => c == buildConfiguration);
                if (match != null)
                    return match;

                match = Configurations.FirstOrDefault(c => buildConfiguration.StartsWith(c));
                if (match != null)
                    return match;

                // Try again with netstandard if we didn't find the specific build match.
                buildConfiguration = "netstandard-Windows_NT";
                match = Configurations.FirstOrDefault(c => c == buildConfiguration);
                if (match != null)
                    return match;

                match = Configurations.FirstOrDefault(c => buildConfiguration.StartsWith(c));
                if (match != null)
                    return match;

                if (Configurations.Length > 0)
                    return Configurations[0];

                return string.Empty;
            }

            private static string ReadProjectGuid(string projectFile)
            {
                var project = ProjectRootElement.Open(projectFile);
                ProjectPropertyElement projectGuid = project.Properties.FirstOrDefault(p => p.Name == "ProjectGuid");

                if (projectGuid == null)
                {
                    return Guid.NewGuid().ToString("B").ToUpper();
                }

                return projectGuid.Value;
            }

            public string SolutionGuid
            {
                get
                {
                    //ProjectTypeGuids for different projects, pulled from the Visual Studio regkeys
                    //TODO: Clean up or map these to actual projects, this is fragile
                    string slnGuid = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"; // Windows (C#)
                    if (ProjectPath.Contains("VisualBasic.vbproj"))
                    {
                        slnGuid = "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"; //Windows (VB.NET)
                    }
                    if (ProjectPath.Contains("TestNativeService")) //Windows (Visual C++)
                    {
                        slnGuid = "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}";
                    }
                    if (ProjectPath.Contains("WebServer.csproj")) //Web Application
                    {
                        slnGuid = "{349C5851-65DF-11DA-9384-00065B846F21}";
                    }

                    return slnGuid;
                }
            }
        }
    }
}
