// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    public class KeyValueConfigurationElement : ConfigurationElement
    {
        private static readonly ConfigurationPropertyCollection s_properties;

        private static readonly ConfigurationProperty s_propKey =
            new ConfigurationProperty("key", typeof(string), string.Empty,
                ConfigurationPropertyOptions.IsKey | ConfigurationPropertyOptions.IsRequired);

        private static readonly ConfigurationProperty s_propValue =
            new ConfigurationProperty("value", typeof(string), string.Empty, ConfigurationPropertyOptions.None);

        private readonly string _initKey;
        private readonly string _initValue;

        private bool _needsInit;

        static KeyValueConfigurationElement()
        {
            s_properties = new ConfigurationPropertyCollection { s_propKey, s_propValue };
        }

        internal KeyValueConfigurationElement() { }

        public KeyValueConfigurationElement(string key, string value)
        {
            _needsInit = true;
            _initKey = key;
            _initValue = value;
        }

        protected internal override ConfigurationPropertyCollection Properties => s_properties;

        [ConfigurationProperty("key", Options = ConfigurationPropertyOptions.IsKey, DefaultValue = "")]
        public string Key => (string)base[s_propKey];

        [ConfigurationProperty("value", DefaultValue = "")]
        public string Value
        {
            get { return (string)base[s_propValue]; }
            set { base[s_propValue] = value; }
        }

        protected internal override void Init()
        {
            base.Init();

            // We cannot initialize configuration properties in the constructor,
            // because Properties is an overridable virtual property that 
            // hence may not be available in the constructor.
            if (_needsInit)
            {
                _needsInit = false;
                base[s_propKey] = _initKey;
                Value = _initValue;
            }
        }
    }
}