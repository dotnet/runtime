// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    public class SettingsProperty
    {
        public virtual string Name { get; set; }
        public virtual bool IsReadOnly { get; set; }
        public virtual object DefaultValue { get; set; }
        public virtual Type PropertyType { get; set; }
        public virtual SettingsSerializeAs SerializeAs { get; set; }
        public virtual SettingsProvider Provider { get; set; }
        public virtual SettingsAttributeDictionary Attributes { get; private set; }
        public bool ThrowOnErrorDeserializing { get; set; }
        public bool ThrowOnErrorSerializing { get; set; }

        public SettingsProperty(string name)
        {
            Name = name;
            Attributes = new SettingsAttributeDictionary();
        }

        public SettingsProperty(
            string name,
            Type propertyType,
            SettingsProvider provider,
            bool isReadOnly,
            object defaultValue,
            SettingsSerializeAs serializeAs,
            SettingsAttributeDictionary attributes,
            bool throwOnErrorDeserializing,
            bool throwOnErrorSerializing)
        {
            Name = name;
            PropertyType = propertyType;
            Provider = provider;
            IsReadOnly = isReadOnly;
            DefaultValue = defaultValue;
            SerializeAs = serializeAs;
            Attributes = attributes;
            ThrowOnErrorDeserializing = throwOnErrorDeserializing;
            ThrowOnErrorSerializing = throwOnErrorSerializing;
        }

        public SettingsProperty(SettingsProperty propertyToCopy)
        {
            Name = propertyToCopy.Name;
            IsReadOnly = propertyToCopy.IsReadOnly;
            DefaultValue = propertyToCopy.DefaultValue;
            SerializeAs = propertyToCopy.SerializeAs;
            Provider = propertyToCopy.Provider;
            PropertyType = propertyToCopy.PropertyType;
            ThrowOnErrorDeserializing = propertyToCopy.ThrowOnErrorDeserializing;
            ThrowOnErrorSerializing = propertyToCopy.ThrowOnErrorSerializing;
            Attributes = new SettingsAttributeDictionary(propertyToCopy.Attributes);
        }
    }

}
