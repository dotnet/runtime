// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using System;
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
        private const string RuntimeOSProperty = "RuntimeOS";
        private const string PropsFileName = "configuration";
        private const string ConfigurationPropsPrefix = "setConfiguration";
        private const string PropsFileExtension = ".props";
        private const string ErrorMessageProperty = "ConfigurationErrorMsg";
        private const string CurrentDirectoryIdentifier = "$(MSBuildThisFileDirectory)";

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

            var projectConfigurationNotSetCondition = $"'$({ConfigurationProperty})' == ''";
            var buildConfigurationPropsFilePath = $"{ConfigurationPropsPrefix}{PropsFileExtension}";
            var buildConfigurationImport = project.AddImport($"{CurrentDirectoryIdentifier}{buildConfigurationPropsFilePath}");
            buildConfigurationImport.Condition = projectConfigurationNotSetCondition;

            CreateBuildConfigurationPropsFile(buildConfigurationPropsFilePath);

            // Parse the properties that are part of Configuration
            ParseProperties(ConfigurationProperty, project, true, p => !p.Independent);

            // Parse the independent properties that aren't part of Configuration
            ParseProperties(BuildConfigurationProperty, project, true, p => p.Independent);

            CreateRuntimeIdentifier(project);

            var projectPath = Path.Combine(PropsFolder, $"{PropsFileName}{PropsFileExtension}");
            project.TreatAsLocalProperty = string.Join<string>(";", ConfigurationFactory.GetProperties().Select(pi => pi.Name));
            project.Save(projectPath);

            return !Log.HasLoggedErrors;
        }


        /// <summary>
        /// Generates choose/when statements to parse values from configuration string
        /// </summary>
        /// <param name="propertyName">name of property to parse</param>
        /// <param name="project">project to update</param>
        private void ParseProperties(string propertyName, ProjectRootElement project, bool includeAdditionalProperites, Func<PropertyInfo, bool> configurationSelector)
        {
            var parseConfigurationPropertyGroup = project.LastChild as ProjectPropertyGroupElement;

            if (parseConfigurationPropertyGroup == null || !string.IsNullOrEmpty(project.LastChild.Condition))
            {
                parseConfigurationPropertyGroup = project.CreatePropertyGroupElement();
                project.AppendChild(parseConfigurationPropertyGroup);
            }

            // delimit property for parsing, this gaurntees that every property value is surrounded in delimiters
            var parsePropertyName = $"_parse_{propertyName}";
            var parseConfigurationValue = $"{ConfigurationFactory.PropertySeperator}$({propertyName}){ConfigurationFactory.PropertySeperator}";
            parseConfigurationPropertyGroup.AddProperty(parsePropertyName, parseConfigurationValue);

            // foreach property, pull it out of Configuration and set derived values.
            foreach (var property in ConfigurationFactory.GetProperties().Where(configurationSelector))
            {
                var choosePropertiesElement = project.CreateChooseElement();
                project.AppendChild(choosePropertiesElement);

                foreach (var value in ConfigurationFactory.GetValues(property))
                {
                    var propertiesCondition = CreateContainsCondition(parsePropertyName, ConfigurationFactory.PropertySeperator + value.Value + ConfigurationFactory.PropertySeperator);
                    var whenPropertiesElement = project.CreateWhenElement(propertiesCondition);
                    choosePropertiesElement.AppendChild(whenPropertiesElement);

                    AddProperties(whenPropertiesElement, value, includeAdditionalProperites);
                }

                var otherwisePropertiesElement = project.CreateOtherwiseElement();
                choosePropertiesElement.AppendChild(otherwisePropertiesElement);

                if (property.DefaultValue != null)
                {
                    AddProperties(otherwisePropertiesElement, property.DefaultValue, includeAdditionalProperites);
                }
                else
                {
                    var otherwiseErrorPropertyGroup = project.CreatePropertyGroupElement();
                    otherwisePropertiesElement.AppendChild(otherwiseErrorPropertyGroup);

                    otherwiseErrorPropertyGroup.AddProperty(ErrorMessageProperty, $"$({ErrorMessageProperty})Could not find a value for {property.Name} from {propertyName} '$({propertyName})'.");
                }
            }
        }

        private void CreateRuntimeIdentifier(ProjectRootElement project)
        {
            var rid = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.GetRuntimeIdentifier();

            string[] ridParts = rid.Split('-');

            if (ridParts.Length < 1)
            {
                throw new System.InvalidOperationException($"Unknown rid format {rid}.");
            }

            string osNameAndVersion = ridParts[0];

            var propertyGroup = project.CreatePropertyGroupElement();
            project.AppendChild(propertyGroup);

            var runtimeProperty = propertyGroup.AddProperty(RuntimeOSProperty, $"{osNameAndVersion}");
            runtimeProperty.Condition = $"'$({RuntimeOSProperty})' == ''";

            Log.LogMessage($"Running on OS with RID {rid}, so defaulting RuntimeOS to '{osNameAndVersion}'");
        }

        /// <summary>
        /// Creates a props file to find best $(Configuration) from $(BuildConfigurations) for any $(BuildConfiguration)
        /// </summary>
        private void CreateBuildConfigurationPropsFile(string fileName)
        {
            var buildConfigurationProps = ProjectRootElement.Create();

            // pull apart BuildConfiguration, but don't set any derived properties
            ParseProperties(BuildConfigurationProperty, buildConfigurationProps, false, p => !p.Independent);

            // Set error on missing import
            var buildConfigurationsPropertyGroup = buildConfigurationProps.CreatePropertyGroupElement();
            buildConfigurationProps.AppendChild(buildConfigurationsPropertyGroup);

            // get path to import
            var buildConfigurationImportName = $"_import_{BuildConfigurationProperty}_props";
            var significantBuildConfiguration = ConfigurationFactory.IdentityConfiguration.GetSignificantConfigurationStrings().First();
            var buildConfigurationImportPath = $"{CurrentDirectoryIdentifier}{ConfigurationPropsPrefix}.{significantBuildConfiguration}{PropsFileExtension}";
            buildConfigurationsPropertyGroup.AddProperty(buildConfigurationImportName, buildConfigurationImportPath);

            var missingImportError = buildConfigurationsPropertyGroup.AddProperty(ErrorMessageProperty, $"$({ErrorMessageProperty}){ConfigurationProperty} is not set and $({BuildConfigurationProperty}) is not a known value for {BuildConfigurationProperty}.");
            missingImportError.Condition = $"!Exists('$({buildConfigurationImportName})')";

            // import props to set ProjectConfiguration
            var buildConfigurationImport = buildConfigurationProps.CreateImportElement($"$({buildConfigurationImportName})");
            buildConfigurationImport.Condition = $"Exists('$({buildConfigurationImportName})')";
            buildConfigurationProps.AppendChild(buildConfigurationImport);

            // iterate over all possible configuration strings
            foreach (var buildConfiguration in ConfigurationFactory.GetSignficantConfigurations())
            {
                CreateBuildConfigurationPropsFile(buildConfiguration);
            }

            buildConfigurationProps.Save(Path.Combine(PropsFolder, fileName));
        }

        /// <summary>
        /// Creates a props file to find best $(Configuration) from $(BuildConfigurations) for a specific $(BuildConfiguration)
        /// </summary>
        /// <param name="buildConfiguration"></param>
        private void CreateBuildConfigurationPropsFile(Configuration buildConfiguration)
        {
            var configurationSpecificProps = ProjectRootElement.Create();
            var compatibleConfigurationStrings = new StringBuilder();

            // delimit BuildConfigurations for parsing
            var parseBuildConfigurationsPropertyGroup = configurationSpecificProps.AddPropertyGroup();
            var parseBuildConfigurationsName = $"_parse_{AvailableBuildConfigurationsProperty}";
            var parseBuildConfigurationsValue = $"{ConfigurationSeperator}$({AvailableBuildConfigurationsProperty}.Replace('%0A','').Replace('%0D','').Replace(' ','')){ConfigurationSeperator}";
            parseBuildConfigurationsPropertyGroup.AddProperty(parseBuildConfigurationsName, parseBuildConfigurationsValue);

            var chooseConfigurationElement = configurationSpecificProps.CreateChooseElement();
            configurationSpecificProps.AppendChild(chooseConfigurationElement);

            // determine compatible project configurations and select best one
            foreach (var compatibleConfiguration in ConfigurationFactory.GetCompatibleConfigurations(buildConfiguration))
            {
                var configurationStrings = compatibleConfiguration.GetSignificantConfigurationStrings();

                if (compatibleConfigurationStrings.Length > 0)
                {
                    compatibleConfigurationStrings.Append(ConfigurationSeperatorString);
                }
                compatibleConfigurationStrings.Append(string.Join(ConfigurationSeperatorString, configurationStrings));

                var guardedProjectConfigurationStrings = configurationStrings.Select(c => ConfigurationSeperator + c + ConfigurationSeperator);
                var configurationCondition = CreateContainsCondition(parseBuildConfigurationsName, guardedProjectConfigurationStrings);
                var whenConfigurationElement = configurationSpecificProps.CreateWhenElement(configurationCondition);
                chooseConfigurationElement.AppendChild(whenConfigurationElement);

                // add property to set Configuration
                var setConfigurationPropertyGroup = configurationSpecificProps.CreatePropertyGroupElement();
                whenConfigurationElement.AppendChild(setConfigurationPropertyGroup);

                // set project configuration
                setConfigurationPropertyGroup.AddProperty(ConfigurationProperty, compatibleConfiguration.GetDefaultConfigurationString());
            }

            var configurationOtherwiseElement = configurationSpecificProps.CreateOtherwiseElement();
            chooseConfigurationElement.AppendChild(configurationOtherwiseElement);

            var configurationOtherwisePropertyGroup = configurationSpecificProps.CreatePropertyGroupElement();
            configurationOtherwiseElement.AppendChild(configurationOtherwisePropertyGroup);

            configurationOtherwisePropertyGroup.AddProperty(ErrorMessageProperty,
                $"$({ErrorMessageProperty})Could not find a compatible configuration for {BuildConfigurationProperty} '$({BuildConfigurationProperty})' " +
                $"from {AvailableBuildConfigurationsProperty} '$({parseBuildConfigurationsName})'.  " +
                $"Considered {compatibleConfigurationStrings}.");

            // save a copy of for each synonymous config string
            foreach (var configurationString in buildConfiguration.GetSignificantConfigurationStrings())
            {
                var configurationProjectPath = Path.Combine(PropsFolder, $"{ConfigurationPropsPrefix}.{configurationString}{PropsFileExtension}");
                configurationSpecificProps.Save(configurationProjectPath);
            }
        }

        private void AddProperties(ProjectElementContainer parent, PropertyValue value, bool includeAddtionalProperties)
        {
            var propertyGroup = parent.ContainingProject.CreatePropertyGroupElement();
            parent.AppendChild(propertyGroup);

            propertyGroup.AddProperty(value.Property.Name, value.Value);

            if (includeAddtionalProperties)
            {
                foreach (var additionalProperty in value.AdditionalProperties)
                {
                    propertyGroup.AddProperty(additionalProperty.Key, additionalProperty.Value);
                }
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

