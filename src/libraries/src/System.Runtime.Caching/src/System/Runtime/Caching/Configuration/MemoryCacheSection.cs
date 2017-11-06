// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Runtime.Caching.Resources;

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

    public sealed class MemoryCacheSection : ConfigurationSection
    {
        private static ConfigurationPropertyCollection s_properties;
        private static readonly ConfigurationProperty s_propNamedCaches;

        static MemoryCacheSection()
        {
            s_propNamedCaches = new ConfigurationProperty("namedCaches",
                                            typeof(MemoryCacheSettingsCollection),
                                            null, // defaultValue
                                            ConfigurationPropertyOptions.None);

            s_properties = new ConfigurationPropertyCollection();
            s_properties.Add(s_propNamedCaches);
        }

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
