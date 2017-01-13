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
    [DebuggerDisplay("{Name}")]
    public class PropertyInfo
    {
        private string defaultValue;
        public PropertyValue DefaultValue { get; private set; }

        public PropertyValue IdentityValue { get; }

        public string Name { get; }

        public int Order { get; }

        public int Precedence { get; }

        public bool Insignificant { get; }

        public bool Independent { get; }

        public PropertyInfo(ITaskItem propertyItem)
        {
            Name = propertyItem.ItemSpec;
            defaultValue = propertyItem.GetMetadata(nameof(DefaultValue));
            Order = ParseIntMetadata(propertyItem, nameof(Order));

            Precedence = int.MaxValue;
            var precedence = propertyItem.GetMetadata(nameof(Precedence));
            if (precedence.Equals(nameof(Independent), StringComparison.OrdinalIgnoreCase))
            {
                Independent = Insignificant = true;
            }
            else if (precedence.Equals(nameof(Insignificant), StringComparison.OrdinalIgnoreCase))
            {
                Insignificant = true;
            }
            else
            {
                Precedence = ParseIntMetadata(propertyItem, nameof(Precedence));
            }

            IdentityValue = new PropertyValue($"$({Name})", this);
        }

        private static int ParseIntMetadata(ITaskItem item, string name)
        {
            int value;
            var metadata = item.GetMetadata(name);

            if (!int.TryParse(metadata, out value))
            {
                throw new InvalidDataException($"Could not parse value '{metadata}' from required metadata '{name}' on item '{item.ItemSpec}'.");
            }

            return value;
        }

        public void ConnectDefault(IDictionary<string, PropertyValue> values)
        {
            if (String.IsNullOrEmpty(defaultValue))
            {
                return;
            }

            PropertyValue value;
            if (!values.TryGetValue(defaultValue, out value))
            {
                throw new ArgumentException($"Property '{Name}' specified default '{defaultValue}' which is not a known value.");
            }

            if (value.Property != this)
            {
                throw new ArgumentException($"Property '{Name}' specified default '{defaultValue}' but that value is associated with property '{value.Property.Name}'.");
            }

            DefaultValue = value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            var other = obj as PropertyInfo;

            if (other == null)
            {
                return false;
            }

            return Name == other.Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}

