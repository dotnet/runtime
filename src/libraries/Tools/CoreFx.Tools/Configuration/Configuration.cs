// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.DotNet.Build.Tasks
{
    /// <summary>
    /// An ordered collection of property values
    /// </summary>
    public class Configuration
    {
        public Configuration(PropertyValue[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            Values = values;
        }

        public PropertyValue[] Values { get; }

        public static IEqualityComparer<Configuration> CompatibleComparer { get; } = new CompatibleConfigurationComparer();

        /// <summary>
        /// Constructs a configuration string from this configuration
        /// </summary>
        /// <param name="allowDefaults">true to omit default values from configuration string</param>
        /// <param name="encounteredDefault">true if a default value was omitted</param>
        /// <returns>configuration string</returns>
        private string GetConfigurationString(bool allowDefaults, bool includeInsignificant, out bool encounteredDefault)
        {
            encounteredDefault = false;
            var configurationBuilder = new StringBuilder();
            foreach (var value in Values)
            {
                if (value.Property.Independent)
                {
                    // skip independent properties
                    continue;
                }

                if (allowDefaults && value == value.Property.DefaultValue)
                {
                    // skip default value
                    encounteredDefault = true;
                    continue;
                }

                if (!includeInsignificant && value.Property.Insignificant)
                {
                    // skip insignificant properties
                    continue;
                }

                if (configurationBuilder.Length > 0)
                {
                    configurationBuilder.Append(ConfigurationFactory.PropertySeperator);
                }
                configurationBuilder.Append(value.Value);
            }

            return configurationBuilder.ToString();
        }

        /// <summary>
        /// Get properties assoicated with this configuration
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, string> GetProperties()
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();

            foreach (var value in Values)
            {
                properties.Add(value.Property.Name, value.Value);

                foreach (var additionalProperty in value.AdditionalProperties)
                {
                    properties.Add(additionalProperty.Key, additionalProperty.Value);
                }
            }

            return properties;
        }

        private static bool[] s_boolValues = new[] { true, false /*, FileNotFound */ };
        private IEnumerable<string> GetConfigurationStrings(bool includeInsignificant)
        {
            // only allow all defaults or no defaults.
            foreach (var allowDefaults in s_boolValues)
            {
                var encounteredDefault = false;

                yield return GetConfigurationString(allowDefaults, includeInsignificant, out encounteredDefault);
                if (!encounteredDefault)
                {
                    // if we didn't encounter a default value don't bother with
                    // another pass since it will produce the same string.
                    break;
                }
            }
        }

        public IEnumerable<string> GetConfigurationStrings()
        {
            return GetConfigurationStrings(includeInsignificant: true);
        }

        public IEnumerable<string> GetSignificantConfigurationStrings()
        {
            return GetConfigurationStrings(includeInsignificant: false);
        }

        public string GetDefaultConfigurationString()
        {
            var unused = false;
            return GetConfigurationString(allowDefaults: true, includeInsignificant: true, encounteredDefault: out unused);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            var other = obj as Configuration;

            if (other == null)
            {
                return false;
            }

            if (Values.Length != other.Values.Length)
            {
                return false;
            }
            return Values.SequenceEqual(other.Values);
        }

        private Nullable<int> hashCode;
        private Nullable<int> compatibleHashCode;
        public override int GetHashCode()
        {
            if (hashCode == null)
            {
                hashCode = 0;
                foreach (var value in Values)
                {
                    hashCode ^= value.GetHashCode();
                }
            }
            return hashCode.Value;
        }
        
        // Only examines significant properties for equality
        private class CompatibleConfigurationComparer : IEqualityComparer<Configuration>
        {
            public bool Equals(Configuration x, Configuration y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                var xValues = x.Values.Where(v => !v.Property.Insignificant);
                var yValues = y.Values.Where(v => !v.Property.Insignificant);

                return xValues.SequenceEqual(yValues);
            }

            public int GetHashCode(Configuration obj)
            {
                if (obj.compatibleHashCode == null)
                {
                    obj.compatibleHashCode = 0;
                    foreach (var value in obj.Values)
                    {
                        if (!value.Property.Insignificant)
                        {
                            obj.compatibleHashCode ^= value.GetHashCode();
                        }
                    }
                }
                return obj.compatibleHashCode.Value;
            }
        }

        public override string ToString()
        {
            var unused = false;
            return GetConfigurationString(true, true, out unused);
        }

    }
}
