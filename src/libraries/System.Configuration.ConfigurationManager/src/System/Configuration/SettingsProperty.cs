// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Configuration
{
    public class SettingsProperty
    {
        internal static bool EnableUnsafeBinaryFormatterInPropertyValueSerialization { get; } = AppContext.TryGetSwitch("System.Configuration.ConfigurationManager.EnableUnsafeBinaryFormatterInPropertyValueSerialization", out bool isEnabled) ? isEnabled : false;

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
#pragma warning disable CS0618 // Type or member is obsolete
            if (serializeAs == SettingsSerializeAs.Binary)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                if (EnableUnsafeBinaryFormatterInPropertyValueSerialization)
                {
                    SerializeAs = serializeAs;
                }
                else
                {
                    throw new NotSupportedException(Obsoletions.BinaryFormatterMessage);
                }
            }
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
