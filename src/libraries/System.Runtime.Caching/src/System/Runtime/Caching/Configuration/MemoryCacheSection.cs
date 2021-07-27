// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Runtime.Caching.Resources;
using System.Runtime.Versioning;

namespace System.Runtime.Caching.Configuration
{
    /*
       <system.runtime.caching>
         <memoryCaches>
           <namedCaches>
             <add name="Default" physicalMemoryPercentage="0" pollingInterval="00:02:00"/>
             <add name="Foo" physicalMemoryPercentage="0" pollingInterval="00:02:00"/>
             <add name="Bar" physicalMemoryPercentage="0" pollingInterval="00:02:00"/>
           </namedCaches>
         </memoryCaches>
       </system.caching>
    */

#if NET5_0_OR_GREATER
    [UnsupportedOSPlatform("browser")]
#endif
    internal sealed class MemoryCacheSection : ConfigurationSection
    {
        private static readonly ConfigurationProperty s_propNamedCaches = new ConfigurationProperty("namedCaches",
            typeof(MemoryCacheSettingsCollection),
            null, // defaultValue
            ConfigurationPropertyOptions.None);
        private static readonly ConfigurationPropertyCollection s_properties = new ConfigurationPropertyCollection() { s_propNamedCaches };

        public MemoryCacheSection()
        {
        }

        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                return s_properties;
            }
        }

        [ConfigurationProperty("namedCaches")]
        public MemoryCacheSettingsCollection NamedCaches
        {
            get
            {
                return (MemoryCacheSettingsCollection)base[s_propNamedCaches];
            }
        }
    }
}
