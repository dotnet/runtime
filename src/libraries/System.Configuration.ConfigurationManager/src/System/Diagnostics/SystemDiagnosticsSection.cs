// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration;

namespace System.Diagnostics
{
    public sealed partial class SystemDiagnosticsSection : ConfigurationSection
    {
        private static readonly ConfigurationPropertyCollection s_properties = new();
        private static readonly ConfigurationProperty s_propPerfCounters = new ConfigurationProperty("performanceCounters", typeof(PerfCounterSettings), new PerfCounterSettings(), ConfigurationPropertyOptions.None);

        static SystemDiagnosticsSection()
        {
            s_properties.Add(s_propPerfCounters);

#if NET7_0_OR_GREATER
            SystemDiagnosticsSectionNetCoreApp();
#endif
        }

        protected internal override ConfigurationPropertyCollection Properties => s_properties;

        [ConfigurationProperty("performanceCounters")]
        public PerfCounterSettings PerfCounterSettings => (PerfCounterSettings)base[s_propPerfCounters];
    }
}
