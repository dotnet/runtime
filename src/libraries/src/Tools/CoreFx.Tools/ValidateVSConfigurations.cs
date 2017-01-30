using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Evaluation;
using System.Linq;
using Microsoft.Build.Construction;

namespace Microsoft.DotNet.Build.Tasks
{
    public class ValidateVSConfigurations : BuildTask
    {
        [Required]
        public ITaskItem[] ProjectsToValidate { get; set; }

        public bool UpdateProjects { get; set; }

        private const string ConfigurationPropsFilename = "Configurations.props";
        private static Regex s_configurationConditionRegex = new Regex(@"'\$\(Configuration\)\|\$\(Platform\)' ?== ?'(?<config>.*)'");
        private static string[] s_configurationSuffixes = new [] { "-Debug|AnyCPU", "-Release|AnyCPU" };

        public override bool Execute()
        {
            foreach (var item in ProjectsToValidate)
            {
                string projectFile = item.ItemSpec;
                string projectConfigurationPropsFile = Path.Combine(Path.GetDirectoryName(projectFile), ConfigurationPropsFilename);

                if (File.Exists(projectConfigurationPropsFile))
                {
                    var expectedConfigurations = GetConfigurationStrings(projectConfigurationPropsFile);

                    var project = ProjectRootElement.Open(projectFile);

                    ICollection<ProjectPropertyGroupElement> propertyGroups;
                    var actualConfigurations = GetConfigurationFromPropertyGroups(project, out propertyGroups);

                    if (!actualConfigurations.SequenceEqual(expectedConfigurations))
                    {
                        if (!UpdateProjects)
                        {
                            Log.LogError($"{item} configurations does not match it's Configurations.props.");
                        }
                        else
                        {
                            ReplaceConfigurationPropertyGroups(project, propertyGroups, expectedConfigurations);
                            project.Save();
                        }
                    }
                }
            }

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets a sorted list of configuration strings from a Configurations.props file
        /// </summary>
        /// <param name="configurationProjectFile">Path to Configuration.props file</param>
        /// <returns>Sorted list of configuration strings</returns>
        private static string[] GetConfigurationStrings(string configurationProjectFile)
        {
            var configurationProject = new Project(configurationProjectFile);

            var buildConfigurations = configurationProject.GetPropertyValue("BuildConfigurations");

            ProjectCollection.GlobalProjectCollection.UnloadProject(configurationProject);

            return buildConfigurations.Trim()
                                      .Split(';')
                                      .Select(c => c.Trim())
                                      .Where(c => !String.IsNullOrEmpty(c))
                                      .SelectMany(c => s_configurationSuffixes.Select(s => c + s))
                                      .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                                      .ToArray();
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
    }
}
