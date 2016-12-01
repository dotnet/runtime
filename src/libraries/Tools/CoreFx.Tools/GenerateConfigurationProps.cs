// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.DotNet.Build.Tasks
{
    public class GenerateConfigurationProps : ConfigurationTask
    {
        private const char ConfigurationSeperator = ';';
        private const string BuildConfigurationProperty = "BuildConfiguration";
        private const string ProjectConfigurationsProperty = "ProjectConfigurations";
        private const string ProjectConfigurationProperty = "ProjectConfiguration";

        /// <summary>
        /// Props file to generate.
        /// </summary>
        [Required]
        public string PropsFile { get; set; }

        
        public override bool Execute()
        {
            LoadConfiguration();

            var project = ProjectRootElement.Create();

            var chooseBuildConfigurationElement = project.CreateChooseElement();
            project.AppendChild(chooseBuildConfigurationElement);

            // create an empty when to short-circuit when ProjectConfiguration is already set.
            var projectConfigurationAlreadySet = project.CreateWhenElement($"'$({ProjectConfigurationProperty})' != ''");
            chooseBuildConfigurationElement.AppendChild(projectConfigurationAlreadySet);

            var choosePropertiesElement = project.CreateChooseElement();
            project.AppendChild(choosePropertiesElement);

            // iterate over all possible configuration strings
            foreach (var configuration in ConfigurationFactory.GetAllConfigurations())
            {
                var buildConfigurationStrings = configuration.GetConfigurationStrings();
                var buildConfigurationCondition = CreateEqualsCondition(BuildConfigurationProperty, buildConfigurationStrings);
                var whenBuildConfigurationElement = project.CreateWhenElement(buildConfigurationCondition);
                chooseBuildConfigurationElement.AppendChild(whenBuildConfigurationElement);


                var chooseProjectConfigurationElement = project.CreateChooseElement();
                whenBuildConfigurationElement.AppendChild(chooseProjectConfigurationElement);

                // determine compatible project configurations
                foreach (var compatibleConfiguration in ConfigurationFactory.GetCompatibleConfigurations(configuration))
                {
                    var projectConfigurationStrings = compatibleConfiguration.GetConfigurationStrings();
                    var gaurdedProjectConfigurationStrings = projectConfigurationStrings.Select(c => ConfigurationSeperator + c + ConfigurationSeperator);
                    var projectConfigurationCondition = CreateContainsCondition(ProjectConfigurationsProperty, gaurdedProjectConfigurationStrings);
                    var whenProjectConfigurationElement = project.CreateWhenElement(projectConfigurationCondition);
                    chooseProjectConfigurationElement.AppendChild(whenProjectConfigurationElement);

                    // add property to set ProjectConfiguration
                    var projectConfigurationPropertyGroup = project.CreatePropertyGroupElement();
                    whenProjectConfigurationElement.AppendChild(projectConfigurationPropertyGroup);

                    // set project configuration
                    projectConfigurationPropertyGroup.AddProperty(ProjectConfigurationProperty, projectConfigurationStrings.First());
                }

                var propertiesCondition = CreateEqualsCondition(ProjectConfigurationProperty, buildConfigurationStrings);
                var whenPropertiesElement = project.CreateWhenElement(propertiesCondition);
                choosePropertiesElement.AppendChild(whenPropertiesElement);

                var additionalPropertiesGroup = project.CreatePropertyGroupElement();
                whenPropertiesElement.AppendChild(additionalPropertiesGroup);
                

                // TODO: parse out the project configuration to parts and set properties on parts rather than the full expansion.

                // set additional properties
                foreach (var projectProperty in configuration.GetProperties())
                {
                    additionalPropertiesGroup.AddProperty(projectProperty.Key, projectProperty.Value);
                }
            }

            project.Save(PropsFile);

            return !Log.HasLoggedErrors;
        }

        private string CreateEqualsCondition(string propertyName, IEnumerable<string> propertyValues)
        {
            var condition = new StringBuilder();

            foreach (var propertyValue in propertyValues)
            {
                if (condition.Length != 0)
                {
                    condition.Append(" OR ");
                }

                condition.Append($"'$({propertyName})' == '{propertyValue}'");
            }

            return condition.ToString();
        }

        private string CreateContainsCondition(string propertyName, IEnumerable<string> propertyValues)
        {
            var condition = new StringBuilder();

            foreach (var propertyValue in propertyValues)
            {
                if (condition.Length != 0)
                {
                    condition.Append(" OR ");
                }

                condition.Append($"$({propertyName}.Contains('{propertyValue}'))");
            }

            return condition.ToString();
        }
    }
}

