// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Configuration
{
    /// <summary>
    /// ConfigurationSection class for sections that store client settings.
    /// </summary>
    public sealed class ClientSettingsSection : ConfigurationSection
    {
        private static readonly ConfigurationProperty s_propSettings = new ConfigurationProperty(null, typeof(SettingElementCollection), null, ConfigurationPropertyOptions.IsDefaultCollection);
        private static readonly ConfigurationPropertyCollection s_properties = new ConfigurationPropertyCollection() { s_propSettings };

        public ClientSettingsSection()
        {
        }

        protected internal override ConfigurationPropertyCollection Properties
        {
            get
            {
                return s_properties;
            }
        }

        [ConfigurationProperty("", IsDefaultCollection = true)]
        public SettingElementCollection Settings
        {
            get
            {
                return (SettingElementCollection)base[s_propSettings];
            }
        }
    }
}
