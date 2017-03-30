// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    public sealed class IriParsingElement : ConfigurationElement
    {
        internal const bool EnabledDefaultValue = false;

        private ConfigurationPropertyCollection _properties = new ConfigurationPropertyCollection();

        private readonly ConfigurationProperty _enabled =
            new ConfigurationProperty(CommonConfigurationStrings.Enabled, typeof(bool), EnabledDefaultValue,
                ConfigurationPropertyOptions.None);

        public IriParsingElement()
        {
            _properties.Add(_enabled);
        }

        protected internal override ConfigurationPropertyCollection Properties
        {
            get
            {
                return _properties;
            }
        }

        [ConfigurationProperty(CommonConfigurationStrings.Enabled, DefaultValue = EnabledDefaultValue)]
        public bool Enabled
        {
            get { return (bool)this[_enabled]; }
            set { this[_enabled] = value; }
        }
    }
}
