// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Configuration
{
    [RequiresUnreferencedCode(ConfigurationManager.TrimWarning)]
    public sealed class ConnectionStringsSection : ConfigurationSection
    {
        private static readonly ConfigurationProperty s_propConnectionStrings =
            new ConfigurationProperty(null, typeof(ConnectionStringSettingsCollection), null,
                ConfigurationPropertyOptions.IsDefaultCollection);

        private static readonly ConfigurationPropertyCollection s_properties = new ConfigurationPropertyCollection { s_propConnectionStrings };

        protected internal override ConfigurationPropertyCollection Properties => s_properties;

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCodeMessage",
            Justification = "Reflection access to the ConfigurationPropertyAttribute instance is covered by RequiresUnreferencedCode on the class: https://github.com/dotnet/runtime/issues/108454")]
        [ConfigurationProperty("", Options = ConfigurationPropertyOptions.IsDefaultCollection)]
        public ConnectionStringSettingsCollection ConnectionStrings
            => (ConnectionStringSettingsCollection)base[s_propConnectionStrings];

        protected internal override object GetRuntimeObject()
        {
            SetReadOnly();
            return this; // return the read only object
        }
    }
}
