// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Configuration
{
    [RequiresUnreferencedCode(ConfigurationManager.TrimWarning)]
    public sealed class ConnectionStringSettings : ConfigurationElement
    {
        private static readonly ConfigurationProperty s_propName =
            new ConfigurationProperty("name", typeof(string), null, null,
                ConfigurationProperty.s_nonEmptyStringValidator,
                ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsKey);

        private static readonly ConfigurationProperty s_propConnectionString =
            new ConfigurationProperty("connectionString", typeof(string), "", ConfigurationPropertyOptions.IsRequired);

        private static readonly ConfigurationProperty s_propProviderName =
            new ConfigurationProperty("providerName", typeof(string), string.Empty, ConfigurationPropertyOptions.None);

        private static readonly ConfigurationPropertyCollection s_properties = new ConfigurationPropertyCollection { s_propName, s_propConnectionString, s_propProviderName };

        public ConnectionStringSettings() { }

        public ConnectionStringSettings(string name, string connectionString)
            : this()
        {
            Name = name;
            ConnectionString = connectionString;
        }

        public ConnectionStringSettings(string name, string connectionString, string providerName)
            : this()
        {
            Name = name;
            ConnectionString = connectionString;
            ProviderName = providerName;
        }

        internal string Key => Name;

        protected internal override ConfigurationPropertyCollection Properties => s_properties;

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCodeMessage",
            Justification = "Reflection access to the ConfigurationPropertyAttribute instance is covered by RequiresUnreferencedCode on the class: https://github.com/dotnet/runtime/issues/108454")]
        [ConfigurationProperty("name",
            Options = ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsKey, DefaultValue = "")]
        public string Name
        {
            get { return (string)base[s_propName]; }
            set { base[s_propName] = value; }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCodeMessage",
            Justification = "Reflection access to the ConfigurationPropertyAttribute instance is covered by RequiresUnreferencedCode on the class: https://github.com/dotnet/runtime/issues/108454")]
        [ConfigurationProperty("connectionString", Options = ConfigurationPropertyOptions.IsRequired, DefaultValue = "")]
        public string ConnectionString
        {
            get { return (string)base[s_propConnectionString]; }
            set { base[s_propConnectionString] = value; }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCodeMessage",
            Justification = "Reflection access to the ConfigurationPropertyAttribute instance is covered by RequiresUnreferencedCode on the class: https://github.com/dotnet/runtime/issues/108454")]
        [ConfigurationProperty("providerName", DefaultValue = "System.Data.SqlClient")]
        public string ProviderName
        {
            get { return (string)base[s_propProviderName]; }
            set { base[s_propProviderName] = value; }
        }

        public override string ToString()
        {
            return ConnectionString;
        }
    }
}
