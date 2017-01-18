// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    /// <summary>
    /// ConfigurationSection class for sections that store client settings.
    /// </summary>
    public sealed class ClientSettingsSection : ConfigurationSection
    {
        private static ConfigurationPropertyCollection _properties;
        private static readonly ConfigurationProperty _propSettings = new ConfigurationProperty(null, typeof(SettingElementCollection), null, ConfigurationPropertyOptions.IsDefaultCollection);

        static ClientSettingsSection()
        {
            _properties = new ConfigurationPropertyCollection();
            _properties.Add(_propSettings);
        }

        public ClientSettingsSection()
        {
        }

        protected internal override ConfigurationPropertyCollection Properties
        {
            get
            {
                return _properties;
            }
        }

        [ConfigurationProperty("", IsDefaultCollection = true)]
        public SettingElementCollection Settings
        {
            get
            {
                return (SettingElementCollection)base[_propSettings];
            }
        }
    }
}
