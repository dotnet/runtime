// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;

namespace System.Diagnostics
{
    internal class SystemDiagnosticsSection : ConfigurationSection
    {
        private static readonly ConfigurationProperty s_propPerfCounters = new ConfigurationProperty("performanceCounters", typeof(PerfCounterSection), new PerfCounterSection(), ConfigurationPropertyOptions.None);
        private static readonly ConfigurationPropertyCollection s_properties = new ConfigurationPropertyCollection { s_propPerfCounters };

        [ConfigurationProperty("performanceCounters")]
        public PerfCounterSection PerfCounters => (PerfCounterSection)base[s_propPerfCounters];

        protected override ConfigurationPropertyCollection Properties => s_properties;
    }
}
