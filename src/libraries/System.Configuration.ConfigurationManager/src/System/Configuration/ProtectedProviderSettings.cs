// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    public class ProtectedProviderSettings : ConfigurationElement
    {
        private readonly ConfigurationProperty _propProviders =
            new ConfigurationProperty(
                name: null,
                type: typeof(ProviderSettingsCollection),
                defaultValue: null,
                options: ConfigurationPropertyOptions.IsDefaultCollection);

        private readonly ConfigurationPropertyCollection _properties;

        public ProtectedProviderSettings()
        {
            // Property initialization
            _properties = new ConfigurationPropertyCollection { _propProviders };
        }

        protected internal override ConfigurationPropertyCollection Properties => _properties;

        [ConfigurationProperty("", IsDefaultCollection = true, Options = ConfigurationPropertyOptions.IsDefaultCollection)]
        public ProviderSettingsCollection Providers => (ProviderSettingsCollection)base[_propProviders];
    }
}