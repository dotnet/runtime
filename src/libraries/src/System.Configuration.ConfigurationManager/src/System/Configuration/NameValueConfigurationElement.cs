// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    public sealed class NameValueConfigurationElement : ConfigurationElement
    {
        private static readonly ConfigurationPropertyCollection s_properties;

        private static readonly ConfigurationProperty s_propName =
            new ConfigurationProperty("name", typeof(string), string.Empty, ConfigurationPropertyOptions.IsKey);

        private static readonly ConfigurationProperty s_propValue =
            new ConfigurationProperty("value", typeof(string), string.Empty, ConfigurationPropertyOptions.None);

        static NameValueConfigurationElement()
        {
            // Property initialization
            s_properties = new ConfigurationPropertyCollection { s_propName, s_propValue };
        }

        internal NameValueConfigurationElement() { }

        public NameValueConfigurationElement(string name, string value)
        {
            base[s_propName] = name;
            base[s_propValue] = value;
        }

        protected internal override ConfigurationPropertyCollection Properties => s_properties;

        [ConfigurationProperty("name", IsKey = true, DefaultValue = "")]
        public string Name => (string)base[s_propName];

        [ConfigurationProperty("value", DefaultValue = "")]
        public string Value
        {
            get { return (string)base[s_propValue]; }
            set { base[s_propValue] = value; }
        }
    }
}