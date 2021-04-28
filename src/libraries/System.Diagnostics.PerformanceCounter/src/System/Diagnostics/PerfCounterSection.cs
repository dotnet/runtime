// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration;

namespace System.Diagnostics
{
    internal sealed class PerfCounterSection : ConfigurationElement
    {
        private static readonly ConfigurationProperty s_propFileMappingSize = new ConfigurationProperty("filemappingsize", typeof(int), 524288, ConfigurationPropertyOptions.None);
        private static readonly ConfigurationPropertyCollection s_properties = new ConfigurationPropertyCollection { s_propFileMappingSize };

        [ConfigurationProperty("filemappingsize", DefaultValue = 524288)]
        public int FileMappingSize => (int)this[s_propFileMappingSize];

        protected override ConfigurationPropertyCollection Properties => s_properties;
    }
}
