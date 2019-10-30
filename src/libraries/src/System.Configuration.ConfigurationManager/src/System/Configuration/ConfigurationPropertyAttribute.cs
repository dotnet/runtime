// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ConfigurationPropertyAttribute : Attribute
    {
        internal const string DefaultCollectionPropertyName = "";

        public ConfigurationPropertyAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public object DefaultValue { get; set; } = ConfigurationElement.s_nullPropertyValue;

        public ConfigurationPropertyOptions Options { get; set; } = ConfigurationPropertyOptions.None;

        public bool IsDefaultCollection
        {
            get { return (Options & ConfigurationPropertyOptions.IsDefaultCollection) != 0; }
            set
            {
                if (value) Options |= ConfigurationPropertyOptions.IsDefaultCollection;
                else
                    Options &= ~ConfigurationPropertyOptions.IsDefaultCollection;
            }
        }

        public bool IsRequired
        {
            get { return (Options & ConfigurationPropertyOptions.IsRequired) != 0; }
            set
            {
                if (value) Options |= ConfigurationPropertyOptions.IsRequired;
                else Options &= ~ConfigurationPropertyOptions.IsRequired;
            }
        }

        public bool IsKey
        {
            get { return (Options & ConfigurationPropertyOptions.IsKey) != 0; }
            set
            {
                if (value) Options |= ConfigurationPropertyOptions.IsKey;
                else Options &= ~ConfigurationPropertyOptions.IsKey;
            }
        }
    }
}
