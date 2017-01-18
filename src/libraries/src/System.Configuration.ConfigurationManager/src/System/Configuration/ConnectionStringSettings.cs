// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    public sealed class ConnectionStringSettings : ConfigurationElement
    {
        private static readonly ConfigurationPropertyCollection s_properties;

        private static readonly ConfigurationProperty s_propName =
            new ConfigurationProperty("name", typeof(string), null, null,
                ConfigurationProperty.s_nonEmptyStringValidator,
                ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsKey);

        private static readonly ConfigurationProperty s_propConnectionString =
            new ConfigurationProperty("connectionString", typeof(string), "", ConfigurationPropertyOptions.IsRequired);

        private static readonly ConfigurationProperty s_propProviderName =
            new ConfigurationProperty("providerName", typeof(string), string.Empty, ConfigurationPropertyOptions.None);

        static ConnectionStringSettings()
        {
            // Property initialization
            s_properties = new ConfigurationPropertyCollection { s_propName, s_propConnectionString, s_propProviderName };
        }

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

        [ConfigurationProperty("name",
            Options = ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsKey, DefaultValue = "")]
        public string Name
        {
            get { return (string)base[s_propName]; }
            set { base[s_propName] = value; }
        }

        [ConfigurationProperty("connectionString", Options = ConfigurationPropertyOptions.IsRequired, DefaultValue = "")]
        public string ConnectionString
        {
            get { return (string)base[s_propConnectionString]; }
            set { base[s_propConnectionString] = value; }
        }

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