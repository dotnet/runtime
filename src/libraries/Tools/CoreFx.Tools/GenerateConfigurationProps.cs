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
        private const string AvailableBuildConfigurationsProperty = "BuildConfigurations";
        private const string ConfigurationProperty = "Configuration";
        private const string PropsFileName = "configuration";
        private const string ConfigurationPropsPrefix = "buildConfiguration";
        private const string PropsFileExtension = ".props";
        private const string ErrorMessageProperty = "ConfigurationErrorMsg";

        /// <summary>
        /// Directory in which to generate props files.
        /// </summary>
        [Required]
        public string PropsFolder { get; set; }

        /// <summary>
        /// Generates a set of props files which can statically determine the best $(Configuration) for 
        /// a given $(BuildConfiguration) from a set of configurations $(BuildConfigurations).
        /// Props files also set properties based on $(Configuration) like TargetGroup, OSGroup, 
        /// NuGetTargetMoniker, etc.
        /// </summary>
        /// <returns></returns>
        public override bool Execute()
        {
            LoadConfiguration();

            var project = ProjectRootElement.Create();

            // Set error on missing import
            var buildConfigurationsPropertyGroup = project.AddPropertyGroup();
            var buildConfigurationImportPath = $"{ConfigurationPropsPrefix}.$({BuildConfigurationProperty}){PropsFileExtension}";
            var projectConfigurationNotSetCondition = $"'$({ConfigurationProperty})' == ''";
            var missingImportError = buildConfigurationsPropertyGroup.AddProperty(ErrorMessageProperty, $"{ConfigurationProperty} is not set and $({BuildConfigurationProperty}) is not a known value for {BuildConfigurationProperty}.");
            missingImportError.Condition = $"{projectConfigurationNotSetCondition} AND !Exists('{buildConfigurationImportPath}')";

            // import props to set ProjectConfiguration
            var buildConfigurationImport = project.AddImport(buildConfigurationImportPath);
            buildConfigurationImport.Condition = $"{projectConfigurationNotSetCondition} AND Exists('{buildConfigurationImportPath}')";

            // iterate over all possible configuration strings
            foreach (var buildConfiguration in ConfigurationFactory.GetAllConfigurations())
            {
                CreateBuildConfigurationPropsFile(buildConfiguration);
            }

            // delimit Configuration for parsing, this gaurntees that every property value is surrounded in delimiters
            var parseConfigurationPropertyGroup = project.CreatePropertyGroupElement();
            project.AppendChild(parseConfigurationPropertyGroup);

            var parseConfigurationName = $"_parse_{ConfigurationProperty}";
            var parseConfigurationValue = $"{ConfigurationFactory.PropertySeperator}$({ConfigurationProperty}){ConfigurationFactory.PropertySeperator}";
            parseConfigurationPropertyGroup.AddProperty(parseConfigurationName, parseConfigurationValue);

            // foreach property, pull it out of Configuration and set derived values.
            foreach (var property in ConfigurationFactory.GetProperties())
            {
                var choosePropertiesElement = project.CreateChooseElement();
                project.AppendChild(choosePropertiesElement);

                foreach (var value in ConfigurationFactory.GetValues(property))
                {
                    var propertiesCondition = CreateContainsCondition(parseConfigurationName, ConfigurationFactory.PropertySeperator + value.Value + ConfigurationFactory.PropertySeperator);
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

                    otherwiseErrorPropertyGroup.AddProperty(ErrorMessageProperty, $"Could not find a value for {property.Name} from {ConfigurationProperty} '$({ConfigurationProperty})'.");
                }

            }

            var projectPath = Path.Combine(PropsFolder, $"{PropsFileName}{PropsFileExtension}");
            project.Save(projectPath);

            return !Log.HasLoggedErrors;
        }

        private void CreateBuildConfigurationPropsFile(Configuration buildConfiguration)
        {
            var configurationSpecificProps = ProjectRootElement.Create();
            var compatibleConfigurationStrings = new StringBuilder();

            // delimit BuildConfigurations for parsing
            var parseBuildConfigurationsPropertyGroup = configurationSpecificProps.AddPropertyGroup();
            var parseBuildConfigurationsName = $"_parse_{AvailableBuildConfigurationsProperty}";
            var parseBuildConfigurationsValue = $"{ConfigurationSeperator}$({AvailableBuildConfigurationsProperty}){ConfigurationSeperator}";
            parseBuildConfigurationsPropertyGroup.AddProperty(parseBuildConfigurationsName, parseBuildConfigurationsValue);

            var chooseConfigurationElement = configurationSpecificProps.CreateChooseElement();
            configurationSpecificProps.AppendChild(chooseConfigurationElement);

            // determine compatible project configurations and select best one
            foreach (var compatibleConfiguration in ConfigurationFactory.GetCompatibleConfigurations(buildConfiguration))
            {
                var configurationStrings = compatibleConfiguration.GetConfigurationStrings();

                if (compatibleConfigurationStrings.Length > 0)
                {
                    compatibleConfigurationStrings.Append(ConfigurationSeperatorString);
                }
                compatibleConfigurationStrings.Append(string.Join(ConfigurationSeperatorString, configurationStrings));

                var gaurdedProjectConfigurationStrings = configurationStrings.Select(c => ConfigurationSeperator + c + ConfigurationSeperator);
                var configurationCondition = CreateContainsCondition(parseBuildConfigurationsName, gaurdedProjectConfigurationStrings);
                var whenConfigurationElement = configurationSpecificProps.CreateWhenElement(configurationCondition);
                chooseConfigurationElement.AppendChild(whenConfigurationElement);

                // add property to set Configuration
                var setConfigurationPropertyGroup = configurationSpecificProps.CreatePropertyGroupElement();
                whenConfigurationElement.AppendChild(setConfigurationPropertyGroup);

                // set project configuration
                setConfigurationPropertyGroup.AddProperty(ConfigurationProperty, configurationStrings.First());
            }

            var configurationOtherwiseElement = configurationSpecificProps.CreateOtherwiseElement();
            chooseConfigurationElement.AppendChild(configurationOtherwiseElement);

            var configurationOtherwisePropertyGroup = configurationSpecificProps.CreatePropertyGroupElement();
            configurationOtherwiseElement.AppendChild(configurationOtherwisePropertyGroup);

            configurationOtherwisePropertyGroup.AddProperty(ErrorMessageProperty,
                $"Could not find a compatible configuration for {BuildConfigurationProperty} '$({BuildConfigurationProperty})' " +
                $"from {AvailableBuildConfigurationsProperty} '$({AvailableBuildConfigurationsProperty})'.  " +
                $"Considered {compatibleConfigurationStrings}.");

            // save a copy of for each synonymous config string
            foreach (var configurationString in buildConfiguration.GetConfigurationStrings())
            {
                var configurationProjectPath = Path.Combine(PropsFolder, $"{ConfigurationPropsPrefix}.{configurationString}{PropsFileExtension}");
                configurationSpecificProps.Save(configurationProjectPath);
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

