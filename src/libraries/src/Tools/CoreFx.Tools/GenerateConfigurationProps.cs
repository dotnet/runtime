// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.DotNet.Build.Tasks
{
    public class GenerateConfigurationProps : ConfigurationTask
    {
        private const char ConfigurationSeperator = ';';
        private const string ConfigurationSeperatorString = ";";
        private const string BuildConfigurationProperty = "BuildConfiguration";
        private const string ProjectConfigurationsProperty = "ProjectConfigurations";
        private const string ProjectConfigurationProperty = "ProjectConfiguration";
        private const string PropsFileName = "projectConfiguration";
        private const string ConfigurationPropsPrefix = "buildConfiguration";
        private const string PropsFileExtension = ".props";
        private const string ErrorMessageProperty = "ConfigurationErrorMsg";

        /// <summary>
        /// Directory in which to generate props files.
        /// </summary>
        [Required]
        public string PropsFolder { get; set; }

        public override bool Execute()
        {
            LoadConfiguration();

            var project = ProjectRootElement.Create();

            // Set error on missing import
            var buildConfigurationsPropertyGroup = project.AddPropertyGroup();
            var buildConfigurationImportPath = $"{ConfigurationPropsPrefix}.$({BuildConfigurationProperty}){PropsFileExtension}";
            var projectConfigurationNotSetCondition = $"'$({ProjectConfigurationProperty})' == ''";
            var missingImportError = buildConfigurationsPropertyGroup.AddProperty(ErrorMessageProperty, $"{ProjectConfigurationProperty} is not set and $({BuildConfigurationProperty}) is not a known value for {BuildConfigurationProperty}.");
            missingImportError.Condition = $"{projectConfigurationNotSetCondition} AND !Exists('{buildConfigurationImportPath}')";

            // import props to set ProjectConfiguration
            var buildConfigurationImport = project.AddImport(buildConfigurationImportPath);
            buildConfigurationImport.Condition = $"{projectConfigurationNotSetCondition} AND Exists('{buildConfigurationImportPath}')";

            // iterate over all possible configuration strings
            foreach (var buildConfiguration in ConfigurationFactory.GetAllConfigurations())
            {
                CreateBuildConfigurationPropsFile(buildConfiguration);
            }

            // delimit ProjectConfiguration for parsing
            var parseProjectConfigurationPropertyGroup = project.CreatePropertyGroupElement();
            project.AppendChild(parseProjectConfigurationPropertyGroup);

            var parseProjectConfigurationName = $"_parse_{ProjectConfigurationProperty}";
            var parseProjectConfigurationValue = $"{ConfigurationFactory.PropertySeperator}$({ProjectConfigurationProperty}){ConfigurationFactory.PropertySeperator}";
            parseProjectConfigurationPropertyGroup.AddProperty(parseProjectConfigurationName, parseProjectConfigurationValue);

            // foreach property, pull it out of ProjectConfiguration and set derived values.
            foreach (var property in ConfigurationFactory.GetProperties())
            {
                var choosePropertiesElement = project.CreateChooseElement();
                project.AppendChild(choosePropertiesElement);

                foreach (var value in ConfigurationFactory.GetValues(property))
                {
                    var propertiesCondition = CreateContainsCondition(parseProjectConfigurationName, ConfigurationFactory.PropertySeperator + value.Value + ConfigurationFactory.PropertySeperator);
                    var whenPropertiesElement = project.CreateWhenElement(propertiesCondition);
                    choosePropertiesElement.AppendChild(whenPropertiesElement);

                    AddProperties(whenPropertiesElement, value);
                }

                var otherwisePropertiesElement = project.CreateOtherwiseElement();
                choosePropertiesElement.AppendChild(otherwisePropertiesElement);

                if (property.DefaultValue != null)
                {
                    AddProperties(otherwisePropertiesElement, property.DefaultValue);
                }
                else
                {
                    var otherwiseErrorPropertyGroup = project.CreatePropertyGroupElement();
                    otherwisePropertiesElement.AppendChild(otherwiseErrorPropertyGroup);

                    otherwiseErrorPropertyGroup.AddProperty(ErrorMessageProperty, $"Could not find a value for {property.Name} from {ProjectConfigurationProperty} '$({ProjectConfigurationProperty})'.");
                }

            }

            var projectPath = Path.Combine(PropsFolder, $"{PropsFileName}{PropsFileExtension}");
            project.Save(projectPath);

            return !Log.HasLoggedErrors;
        }

        private void CreateBuildConfigurationPropsFile(Configuration buildConfiguration)
        {
            var configurationProject = ProjectRootElement.Create();
            var compatibleConfigurationStrings = new StringBuilder();

            // delimit ProjectConfigurations for parsing
            var configurationProjectPropertyGroup = configurationProject.AddPropertyGroup();
            var parseProjectConfigurationsName = $"_parse_{ProjectConfigurationsProperty}";
            var parseProjectConfigurationsValue = $"{ConfigurationSeperator}$({ProjectConfigurationsProperty}){ConfigurationSeperator}";
            configurationProjectPropertyGroup.AddProperty(parseProjectConfigurationsName, parseProjectConfigurationsValue);

            var chooseProjectConfigurationElement = configurationProject.CreateChooseElement();
            configurationProject.AppendChild(chooseProjectConfigurationElement);

            // determine compatible project configurations and select best one
            foreach (var compatibleProjectConfiguration in ConfigurationFactory.GetCompatibleConfigurations(buildConfiguration))
            {
                var projectConfigurationStrings = compatibleProjectConfiguration.GetConfigurationStrings();

                if (compatibleConfigurationStrings.Length > 0)
                {
                    compatibleConfigurationStrings.Append(ConfigurationSeperatorString);
                }
                compatibleConfigurationStrings.Append(string.Join(ConfigurationSeperatorString, projectConfigurationStrings));

                var gaurdedProjectConfigurationStrings = projectConfigurationStrings.Select(c => ConfigurationSeperator + c + ConfigurationSeperator);
                var projectConfigurationCondition = CreateContainsCondition(parseProjectConfigurationsName, gaurdedProjectConfigurationStrings);
                var whenProjectConfigurationElement = configurationProject.CreateWhenElement(projectConfigurationCondition);
                chooseProjectConfigurationElement.AppendChild(whenProjectConfigurationElement);

                // add property to set ProjectConfiguration
                var projectConfigurationPropertyGroup = configurationProject.CreatePropertyGroupElement();
                whenProjectConfigurationElement.AppendChild(projectConfigurationPropertyGroup);

                // set project configuration
                projectConfigurationPropertyGroup.AddProperty(ProjectConfigurationProperty, projectConfigurationStrings.First());
            }

            var configurationOtherwiseElement = configurationProject.CreateOtherwiseElement();
            chooseProjectConfigurationElement.AppendChild(configurationOtherwiseElement);

            var configurationOtherwisePropertyGroup = configurationProject.CreatePropertyGroupElement();
            configurationOtherwiseElement.AppendChild(configurationOtherwisePropertyGroup);

            configurationOtherwisePropertyGroup.AddProperty(ErrorMessageProperty,
                $"Could not find a compatible configuration for {BuildConfigurationProperty} '$({BuildConfigurationProperty})' " +
                $"from {ProjectConfigurationsProperty} '$({ProjectConfigurationsProperty})'.  " +
                $"Considered {compatibleConfigurationStrings}.");

            // save a copy of for each synonymous config string
            foreach (var configurationString in buildConfiguration.GetConfigurationStrings())
            {
                var configurationProjectPath = Path.Combine(PropsFolder, $"{ConfigurationPropsPrefix}.{configurationString}{PropsFileExtension}");
                configurationProject.Save(configurationProjectPath);
            }
        }

        private void AddProperties(ProjectElementContainer parent, PropertyValue value)
        {
            var propertyGroup = parent.ContainingProject.CreatePropertyGroupElement();
            parent.AppendChild(propertyGroup);

            propertyGroup.AddProperty(value.Property.Name, value.Value);

            foreach (var additionalProperty in value.AdditionalProperties)
            {
                propertyGroup.AddProperty(additionalProperty.Key, additionalProperty.Value);
            }
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

        private string CreateContainsCondition(string propertyName, string propertyValue)
        {
            return $"$({propertyName}.Contains('{propertyValue}'))";
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

