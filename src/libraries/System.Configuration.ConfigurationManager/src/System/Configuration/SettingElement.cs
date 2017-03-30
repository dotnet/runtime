// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;

namespace System.Configuration
{
    public sealed class SettingElement : ConfigurationElement
    {
        private static ConfigurationPropertyCollection _properties;
        private static readonly ConfigurationProperty _propName = new ConfigurationProperty(
            "name",
            typeof(string),
            "",
            ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsKey);
        private static readonly ConfigurationProperty _propSerializeAs = new ConfigurationProperty(
            "serializeAs",
            typeof(SettingsSerializeAs),
            SettingsSerializeAs.String,
            ConfigurationPropertyOptions.IsRequired);
        private static readonly ConfigurationProperty _propValue = new ConfigurationProperty(
            "value",
            typeof(SettingValueElement),
            null,
            ConfigurationPropertyOptions.IsRequired);
        private static XmlDocument _document = new XmlDocument();

        static SettingElement()
        {
            // Property initialization
            _properties = new ConfigurationPropertyCollection();

            _properties.Add(_propName);
            _properties.Add(_propSerializeAs);
            _properties.Add(_propValue);

        }

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
                return _properties;
            }
        }

        [ConfigurationProperty("name", IsRequired = true, IsKey = true, DefaultValue = "")]
        public string Name
        {
            get
            {
                return (string)base[_propName];
            }
            set
            {
                base[_propName] = value;
            }
        }

        [ConfigurationProperty("serializeAs", IsRequired = true, DefaultValue = SettingsSerializeAs.String)]
        public SettingsSerializeAs SerializeAs
        {
            get
            {
                return (SettingsSerializeAs)base[_propSerializeAs];
            }
            set
            {
                base[_propSerializeAs] = value;
            }
        }

        [ConfigurationProperty("value", IsRequired = true, DefaultValue = null)]
        public SettingValueElement Value
        {
            get
            {
                return (SettingValueElement)base[_propValue];
            }
            set
            {
                base[_propValue] = value;
            }
        }
    }
}
