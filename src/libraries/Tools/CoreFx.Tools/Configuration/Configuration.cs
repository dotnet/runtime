// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        private string GetConfigurationString(bool allowDefaults, out bool encounteredDefault)
        {
            encounteredDefault = false;
            var configurationBuilder = new StringBuilder();
            foreach (var value in Values)
            {
                if (allowDefaults && value == value.Property.DefaultValue)
                {
                    // skip default value
                    encounteredDefault = true;
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
        /// <summary>
        /// Given a list of property values, returns valid configuration strings for those values.
        /// </summary>
        /// <param name="valueSet"></param>
        /// <returns></returns>
        public IEnumerable<string> GetConfigurationStrings()
        {
            // only allow all defaults or no defaults.
            foreach (var allowDefaults in s_boolValues)
            {
                var encounteredDefault = false;

                yield return GetConfigurationString(allowDefaults, out encounteredDefault);
                if (!encounteredDefault)
                {
                    // if we didn't encounter a default value don't bother with
                    // another pass since it will produce the same string.
                    break;
                }
            }
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
        public override int GetHashCode()
        {
            if (hashCode == null)
            {
                hashCode = 0;
                foreach (var value in Values)
                {
                    hashCode ^= hashCode;
                }
            }
            return hashCode.Value;
        }

        public override string ToString()
        {
            var unused = false;
            return GetConfigurationString(true, out unused);
        }

    }
}
