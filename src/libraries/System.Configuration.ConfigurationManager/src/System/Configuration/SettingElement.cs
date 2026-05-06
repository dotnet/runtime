// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;

namespace System.Configuration
{
    public sealed class SettingElement : ConfigurationElement
    {
        private static readonly ConfigurationProperty s_propName = new ConfigurationProperty(
            "name",
            typeof(string),
            "",
            ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsKey);
        private static readonly ConfigurationProperty s_propSerializeAs = new ConfigurationProperty(
            "serializeAs",
            typeof(SettingsSerializeAs),
            SettingsSerializeAs.String,
            ConfigurationPropertyOptions.IsRequired);
        private static readonly ConfigurationProperty s_propValue = new ConfigurationProperty(
            "value",
            typeof(SettingValueElement),
            null,
            ConfigurationPropertyOptions.IsRequired);
        private static readonly ConfigurationPropertyCollection s_properties = new ConfigurationPropertyCollection() { s_propName, s_propSerializeAs, s_propValue };

        public SettingElement()
        {
        }

        public SettingElement(string name, SettingsSerializeAs serializeAs) : this()
        {
            Name = name;
            SerializeAs = serializeAs;
        }

        internal string Key
        {
            get
            {
                return Name;
            }
        }

        public override bool Equals(object settings)
        {
            SettingElement element = settings as SettingElement;
            return (element != null && base.Equals(settings) && Equals(element.Value, Value));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ Value.GetHashCode();
        }

        protected internal override ConfigurationPropertyCollection Properties
        {
            get
            {
                return s_properties;
            }
        }

        [ConfigurationProperty("name", IsRequired = true, IsKey = true, DefaultValue = "")]
        public string Name
        {
            get
            {
                return (string)base[s_propName];
            }
            set
            {
                base[s_propName] = value;
            }
        }

        [ConfigurationProperty("serializeAs", IsRequired = true, DefaultValue = SettingsSerializeAs.String)]
        public SettingsSerializeAs SerializeAs
        {
            get
            {
                return (SettingsSerializeAs)base[s_propSerializeAs];
            }
            set
            {
                base[s_propSerializeAs] = value;
            }
        }

        [ConfigurationProperty("value", IsRequired = true, DefaultValue = null)]
        public SettingValueElement Value
        {
            get
            {
                return (SettingValueElement)base[s_propValue];
            }
            set
            {
                base[s_propValue] = value;
            }
        }
    }
}
