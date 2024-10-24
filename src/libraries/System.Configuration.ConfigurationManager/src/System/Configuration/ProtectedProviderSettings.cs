// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Configuration
{
    [RequiresUnreferencedCode(ConfigurationManager.TrimWarning)]
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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCodeMessage",
            Justification = "Reflection access to the ConfigurationPropertyAttribute instance is covered by RequiresUnreferencedCode on the class: https://github.com/dotnet/runtime/issues/108454")]
        [ConfigurationProperty("", IsDefaultCollection = true, Options = ConfigurationPropertyOptions.IsDefaultCollection)]
        public ProviderSettingsCollection Providers => (ProviderSettingsCollection)base[_propProviders];
    }
}
