// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.DotNet.Build.Tasks
{
    public class ConfigurationFactory
    {
        internal const char PropertySeperator = '-';

        private Dictionary<string, PropertyInfo> Properties { get; }

        private PropertyInfo[] PropertiesByOrder { get; }

        private PropertyInfo[] PropertiesByPrecedence { get; }

        private Dictionary<PropertyInfo, PropertyValue[]> PropertyValues { get; }

        private Dictionary<string, PropertyValue> AllPropertyValues { get; }

        public Configuration IdentityConfiguration { get; }

        public ConfigurationFactory(ITaskItem[] properties, ITaskItem[] propertyValues)
        {
            Properties = properties.Select(p => new PropertyInfo(p))
                .ToDictionary(p => p.Name, p => p);
            PropertiesByOrder = Properties.Values.OrderBy(p => p.Order).ToArray();
            PropertiesByPrecedence = Properties.Values.OrderBy(p => p.Precedence).ToArray();

            var propertyValueGrouping = propertyValues.Select(v => new PropertyValue(v, Properties)).GroupBy(p => p.Value);

            var duplicateValueGrouping = propertyValueGrouping.Where(g => g.Count() > 1);

            if (duplicateValueGrouping.Any())
            {
                var duplicatesMessage = String.Join("; ", duplicateValueGrouping.Select(g => $"Value: {g.Key} Properties: {String.Join(", ", g.Select(p => p.Property.Name))}"));
                throw new ArgumentException($"Duplicate values are not permitted. {duplicatesMessage}");
            }

            AllPropertyValues = propertyValueGrouping
                .ToDictionary(g => g.Key, g => g.Single());
            
            PropertyValues = AllPropertyValues.Values
                .GroupBy(v => v.Property)
                .ToDictionary(g => g.Key, g => g.ToArray());

            // connect the graph
            foreach (var propertyValue in AllPropertyValues.Values)
            {
                propertyValue.ConnectValues(AllPropertyValues);
            }

            // connect property to value
            foreach (var property in Properties.Values)
            {
                property.ConnectDefault(AllPropertyValues);
            }

            IdentityConfiguration = new Configuration(PropertiesByOrder.Select(p => p.IdentityValue).ToArray());
        }

        public IEnumerable<PropertyInfo> GetProperties()
        {
            return PropertiesByOrder;
        }

        /// <summary>
        /// Get known values for a property
        /// </summary>
        /// <param name="property">name of property to retrieve values</param>
        /// <returns></returns>
        public IEnumerable<PropertyValue> GetValues(string property)
        {
            PropertyInfo propertyInfo;

            if (!Properties.TryGetValue(property, out propertyInfo))
            {
                throw new ArgumentException($"Unknown property name {property}");
            }
            
            return GetValues(propertyInfo);
        }

        /// <summary>
        /// Get known values for a property
        /// </summary>
        /// <param name="property">property to retrieve values</param>
        /// <returns></returns>
        public IEnumerable<PropertyValue> GetValues(PropertyInfo property)
        {
            PropertyValue[] values;

            if (!PropertyValues.TryGetValue(property, out values))
            {
                throw new ArgumentException($"Unknown property {property}.");
            }

            return values;
        }

        /// <summary>
        /// Calculates all value combinations for properties.
        /// </summary>
        /// <param name="properties">List of properties, ordered by precedence</param>
        /// <param name="selectValues">Value selector that returns values for each property, ordered by precedence</param>
        /// <returns>All combinations of values</returns>
        public IEnumerable<Configuration> GetConfigurations(Func<PropertyInfo, IEnumerable<PropertyValue>> selectValues)
        {
            // get all property values, ordered by precedence
            var values = PropertiesByPrecedence.Select(selectValues);

            // start with an empty enumerable
            IEnumerable<IEnumerable<PropertyValue>> emptySet = new[] { Enumerable.Empty<PropertyValue>() };

            // accumulate the cross-product
            var allValues = values.Aggregate(
                emptySet,
                (valueSets, propertyValues) =>
                  valueSets.SelectMany(valueSet =>
                    propertyValues.Select(propertyValue =>
                      valueSet.Concat(new[] { propertyValue })
                    )));

            // convert into configuration
            return allValues.Select(
                valueSet => new Configuration(
                    valueSet.OrderBy(v => v.Property.Order).ToArray()));
        }

        /// <summary>
        /// Gets all possible value combinations in order of precedence
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Configuration> GetAllConfigurations()
        {
            return GetConfigurations(p => GetValues(p));
        }

        /// <summary>
        /// Gets all significant combinations in order of precedence
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Configuration> GetSignficantConfigurations()
        {
            return GetConfigurations(p => p.Insignificant ? new[] { p.IdentityValue } : GetValues(p));
        }

        /// <summary>
        /// Gets value combinations compatible with the specify value combination in order of precedence
        /// </summary>
        /// <param name="valueSet"></param>
        /// <returns></returns>
        public IEnumerable<Configuration> GetCompatibleConfigurations(Configuration configuration, bool doNotAllowCompatibleValues = false)
        {
            var propTable = configuration.Values.ToDictionary(v => v.Property, v => v);

            return GetConfigurations(p => propTable[p].GetCompatibleValues(doNotAllowCompatibleValues));
        }

        /// <summary>
        /// Parses a configuration string to return a Configuration.
        /// </summary>
        /// <param name="configurationString"></param>
        /// <returns></returns>
        internal Configuration ParseConfiguration(string configurationString, bool permitUnknownValues = false)
        {
            var values = configurationString.Split(PropertySeperator);

            var valueSet = new PropertyValue[PropertiesByOrder.Length];

            for(int propertyIndex = 0, valueIndex = 0; propertyIndex < PropertiesByOrder.Length; propertyIndex++, valueIndex++)
            {
                var value = valueIndex < values.Length ? values[valueIndex] : null;
                var property = PropertiesByOrder[propertyIndex];

                if (String.IsNullOrEmpty(value))
                {
                    if (property.DefaultValue != null)
                    {
                        valueSet[propertyIndex] = property.DefaultValue;
                        continue;
                    }
                    else
                    {
                        throw new ArgumentException($"No value was provided for property '{property.Name}' and no default value exists");
                    }
                }

                PropertyValue propertyValue;

                if (!AllPropertyValues.TryGetValue(value, out propertyValue))
                {
                    if (permitUnknownValues)
                    {
                        valueSet[propertyIndex] = new PropertyValue(value, property);
                        continue;
                    }
                    else
                    {
                        throw new ArgumentException($"Unknown value '{value}' found in configuration '{configurationString}'.  Expected property '{property.Name}' with one of values {String.Join(", ", PropertyValues[property].Select(v => v.Value))}.");
                    }
                }

                if (propertyValue.Property != property)
                {
                    // we have a known value but it is not for the expected property.
                    // so long as we have properties with defaultValues, set them
                    while(propertyValue.Property != property)
                    {
                        if (property.DefaultValue == null)
                        {
                            // we can't use this property at this index
                            throw new ArgumentException($"Property '{propertyValue.Property.Name}' value '{propertyValue.Value}' occured at unexpected position in configuration '{configurationString}'");
                        }

                        // give this property its default value and advance to the next property
                        valueSet[propertyIndex++] = property.DefaultValue;

                        if (propertyIndex > PropertiesByOrder.Length)
                        {
                            // we ran out of possible properties.
                            throw new ArgumentException($"Property '{propertyValue.Property.Name}' value '{propertyValue.Value}' occured at unexpected position in configuration '{configurationString}'");
                        }

                        property = PropertiesByOrder[propertyIndex];
                    }
                }

                // we found the position for this value.
                Debug.Assert(propertyValue.Property == property);
                valueSet[propertyIndex] = propertyValue;
            }

            return new Configuration(valueSet);
        }
    }
}

