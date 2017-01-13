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
    [DebuggerDisplay("{Property.Name} = {Value}")]
    public class PropertyValue
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

        public PropertyValue(string value, PropertyInfo property)
        {
            Value = value;
            Property = property;
            AdditionalProperties = new KeyValuePair<string, string>[0];
            CompatibleValues = Enumerable.Empty<PropertyValue>();
            ImportValues = Enumerable.Empty<PropertyValue>();
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

        public IEnumerable<PropertyValue> GetCompatibleValues(bool doNotAllowCompatibleValues)
        {
            var queue = new Queue<PropertyValue>();
            var visited = new HashSet<PropertyValue>();

            IEnumerable<PropertyValue> roots = new[] { this };

            if (!doNotAllowCompatibleValues)
            {
                roots = roots.Concat(CompatibleValues);
            }

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

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            var other = obj as PropertyValue;

            if (other == null)
            {
                return false;
            }

            return Value == other.Value && Property == other.Property;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode() ^ Property.GetHashCode();
        }
    }
}

