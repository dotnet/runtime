// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    public sealed class ConnectionStringsSection : ConfigurationSection
    {
        private static readonly ConfigurationPropertyCollection s_properties;

        private static readonly ConfigurationProperty s_propConnectionStrings =
            new ConfigurationProperty(null, typeof(ConnectionStringSettingsCollection), null,
                ConfigurationPropertyOptions.IsDefaultCollection);

        static ConnectionStringsSection()
        {
            // Property initialization
            s_properties = new ConfigurationPropertyCollection { s_propConnectionStrings };
        }

        protected internal override ConfigurationPropertyCollection Properties => s_properties;

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