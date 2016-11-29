// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using System.IO;
using System.Diagnostics;
using Microsoft.Build.Construction;
using System.Collections;

namespace Microsoft.DotNet.Build.Tasks
{
    public class GenerateConfigurationProps : BuildTask
    {
        private const char PropertySeperator = '|';
        private const char ConfigurationSeperator = ';';
        private const string BuildConfigurationProperty = "BuildConfiguration";
        private const string ProjectConfigurationsProperty = "ProjectConfigurations";
        private const string ProjectConfigurationProperty = "ProjectConfiguration";

        /// <summary>
        /// List of properties in order of precedence.
        /// Metadata:
        ///   DefaultValue: default value for the property.  Default values may be omitted from configuration strings.
        ///   Precedence: integer indicating selection precedence.
        ///   Order: integer indicating configuration string ordering.
        /// </summary>
        [Required]
        public ITaskItem[] Properties { get; set; }

        /// <summary>
        /// Relations between property values.
        /// 
        /// Identity: PropertyValue
        /// Metadata: 
        ///   Property: Name of property to which this value applies
        ///   Imports: List of other property values to consider, in breadth first order, after this value.
        ///   CompatibleWith: List of additional property values to consider, after all imports have been considered.
        ///   Each value will independently undergo a breadth-first traversal of imports.
        ///   Other values: Properties to be set when this configuration property is set.
        /// </summary>
        [Required]
        public ITaskItem[] PropertyValues { get; set; }

        /// <summary>
        /// Props file to generate.
        /// </summary>
        [Required]
        public string PropsFile { get; set; }

        private Dictionary<string, PropertyInfo> propertyNames;
        private Dictionary<PropertyInfo, Dictionary<string, PropertyValue>> propertyValues;
        
        public override bool Execute()
        {
            propertyNames = Properties.Select(p => new PropertyInfo(p)).ToDictionary(p => p.Name, p => p);

            propertyValues = PropertyValues.Select(v => new PropertyValue(v, propertyNames))
                .GroupBy(v => v.Property.Name)
                .ToDictionary(g => propertyNames[g.Key], g => g.ToDictionary(r => r.Value, r => r));

            foreach(var specificPropertyValues in propertyValues.Values)
            {
                foreach(var specificPropertyValue in specificPropertyValues.Values)
                {
                    specificPropertyValue.ConnectValues(specificPropertyValues);
                }
            }

            var project = ProjectRootElement.Create();

            var chooseBuildConfigurationElement = project.CreateChooseElement();
            project.AppendChild(chooseBuildConfigurationElement);

            // create an empty when to short-circuit when ProjectConfiguration is already set.
            var projectConfigurationAlreadySet = project.CreateWhenElement($"'$({ProjectConfigurationProperty})' != ''");
            chooseBuildConfigurationElement.AppendChild(projectConfigurationAlreadySet);

            var choosePropertiesElement = project.CreateChooseElement();
            project.AppendChild(choosePropertiesElement);

            var properties = propertyNames.Values.OrderBy(p => p.Precedence).ToArray();
            
            // iterate over all possible configuration strings
            foreach (var valueSet in GetValueSets(properties, p => propertyValues[p].Values))
            {
                var buildConfigurationStrings = GetConfigurationStrings(valueSet);
                var buildConfigurationCondition = CreateEqualsCondition(BuildConfigurationProperty, buildConfigurationStrings);
                var whenBuildConfigurationElement = project.CreateWhenElement(buildConfigurationCondition);
                chooseBuildConfigurationElement.AppendChild(whenBuildConfigurationElement);


                var chooseProjectConfigurationElement = project.CreateChooseElement();
                whenBuildConfigurationElement.AppendChild(chooseProjectConfigurationElement);

                // determine compatible project configurations
                var propTable = valueSet.ToDictionary(v => v.Property, v => v);
                foreach (var compatibleValueSet in GetValueSets(properties, p => propTable[p].GetCompatibleValues()))
                {
                    var projectConfigurationStrings = GetConfigurationStrings(compatibleValueSet);
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
                
                // set additional properties
                foreach (var projectProperty in valueSet)
                {
                    additionalPropertiesGroup.AddProperty(projectProperty.Property.Name, projectProperty.Value);

                    foreach (var additionalProperty in projectProperty.AdditionalProperties)
                    {
                        additionalPropertiesGroup.AddProperty(additionalProperty.Key, additionalProperty.Value);
                    }
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

        /// <summary>
        /// Calculates all value combinations for properties.
        /// </summary>
        /// <param name="properties">List of properties, ordered by precedence</param>
        /// <param name="selectValues">Value selector that returns values for each property, ordered by precedence</param>
        /// <returns>All combinations of values</returns>
        private IEnumerable<IEnumerable<PropertyValue>> GetValueSets(PropertyInfo[] properties, Func<PropertyInfo, IEnumerable<PropertyValue>> selectValues)
        {
            var values = properties.Select(selectValues);

            // start with an empty enumerable
            IEnumerable<IEnumerable<PropertyValue>> emptySet = new[] { Enumerable.Empty<PropertyValue>() };

            // accumulate the cross-product
            return values.Aggregate(
                emptySet,
                (valueSets, propertyValues) =>
                  valueSets.SelectMany(valueSet => 
                    propertyValues.Select(propertyValue => 
                      valueSet.Concat(new[] { propertyValue })
                    )));
        }

        private static bool[] s_boolValues = new[] { true, false /*, FileNotFound */ };

        /// <summary>
        /// Given a list of property values, returns valid configuration strings for those values.
        /// </summary>
        /// <param name="valueSet"></param>
        /// <returns></returns>
        private IEnumerable<string> GetConfigurationStrings(IEnumerable<PropertyValue> valueSet)
        {
            var orderedValues = valueSet.OrderBy(v => v.Property.Order).ToArray();

            // only allow all defaults or no defaults.
            foreach (var allowDefaults in s_boolValues)
            {
                var encounteredDefault = false;
                var configurationBuilder = new StringBuilder();
                foreach (var value in orderedValues)
                {
                    if (allowDefaults && value.Value == value.Property.DefaultValue)
                    {
                        // skip default value
                        encounteredDefault = true;
                        continue;
                    }

                    if (configurationBuilder.Length > 0)
                    {
                        configurationBuilder.Append(PropertySeperator);
                    }
                    configurationBuilder.Append(value.Value);
                }
                yield return configurationBuilder.ToString();
                if (!encounteredDefault)
                {
                    // if we didn't encounter a default value don't bother with
                    // another pass since it will produce the same string.
                    break;
                }
            }
        }

        [DebuggerDisplay("{Name}")]
        private class PropertyInfo
        {
            public string DefaultValue { get; }

            public string Name { get; }

            public int Order { get; }

            public int Precedence { get; }

            public PropertyInfo(ITaskItem propertyItem)
            {
                Name = propertyItem.ItemSpec;
                DefaultValue = propertyItem.GetMetadata(nameof(DefaultValue));
                Precedence = int.Parse(propertyItem.GetMetadata(nameof(Precedence)));
                Order = int.Parse(propertyItem.GetMetadata(nameof(Order)));
            }
        }

        [DebuggerDisplay("{Property.Name} = {Value}")]
        private class PropertyValue
        {
            private const string PropertyName = "Property";
            private const string ImportsName = "Imports";
            private const string CompatibleWithName = "CompatibleWith";

            private static string[] s_excludedMetadata = new[] { PropertyName, ImportsName, CompatibleWithName };
            private static char[] s_SplitChar = new[] { ';' };

            private string imports;
            private string compatibleWith;

            public KeyValuePair<string, string>[] AdditionalProperties { get; }

            public IEnumerable<PropertyValue> CompatibleValues { get; private set; }

            public IEnumerable<PropertyValue> ImportValues { get; private set; }

            public PropertyInfo Property { get; }

            public string Value { get; }

            public PropertyValue(ITaskItem propertyValue, Dictionary<string, PropertyInfo> propertyNames)
            {
                Value = propertyValue.ItemSpec;

                var name = propertyValue.GetMetadata(PropertyName);
                PropertyInfo property;
                if (!propertyNames.TryGetValue(name, out property))
                {
                    throw new Exception($"PropertyValue {Value} contained unknown property name \"{name}\"");
                }
                Property = property;

                imports = propertyValue.GetMetadata(ImportsName);
                compatibleWith = propertyValue.GetMetadata(CompatibleWithName);

                var customMetadata = propertyValue.CloneCustomMetadata();
                AdditionalProperties = customMetadata.Keys.Cast<string>()
                    .Where(k => !s_excludedMetadata.Contains(k))
                    .Select(k => new KeyValuePair<string, string>(k, (string)customMetadata[k]))
                    .ToArray();
                    
            }

            public void ConnectValues(Dictionary<string, PropertyValue> values)
            {
                ImportValues = ConnectValues(imports, values);
                CompatibleValues = ConnectValues(compatibleWith, values);
            }

            private IEnumerable<PropertyValue> ConnectValues(string property, Dictionary<string, PropertyValue> values)
            {
                List<PropertyValue> connectedValues = new List<PropertyValue>();

                if (!String.IsNullOrEmpty(property))
                {
                    foreach (var value in property.Split(s_SplitChar))
                    {
                        PropertyValue otherProperty;
                        if (!values.TryGetValue(value, out otherProperty))
                        {
                            throw new Exception($"Unknown compatible value {value} for property {Property.Name} value {Value}.");
                        }
                        connectedValues.Add(otherProperty);
                    }
                }

                return connectedValues;
            }

            public IEnumerable<PropertyValue> GetCompatibleValues()
            {
                var queue = new Queue<PropertyValue>();
                var visited = new HashSet<PropertyValue>();

                var roots = new[] { this }.Concat(CompatibleValues);

                // do a breadth first traversal of imports from each root, without duplicates
                foreach (var root in roots)
                {
                    queue.Enqueue(root);
                    visited.Add(root);

                    while (queue.Count > 0)
                    {
                        var current = queue.Dequeue();

                        yield return current;

                        foreach (var import in current.ImportValues)
                        {
                            if (!visited.Contains(import))
                            {
                                queue.Enqueue(import);
                                visited.Add(import);
                            }
                        }
                    }
                }
            }
        }
    }
}

