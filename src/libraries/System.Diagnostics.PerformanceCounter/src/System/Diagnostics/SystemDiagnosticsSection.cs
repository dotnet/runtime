// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;

namespace System.Diagnostics {
    internal class SystemDiagnosticsSection : ConfigurationSection {
        private static readonly ConfigurationPropertyCollection _properties;
        private static readonly ConfigurationProperty _propPerfCounters = new ConfigurationProperty("performanceCounters", typeof(PerfCounterSection), new PerfCounterSection(), ConfigurationPropertyOptions.None);

        static SystemDiagnosticsSection() {
            _properties = new ConfigurationPropertyCollection();
            _properties.Add(_propPerfCounters);
        }

        [ConfigurationProperty("performanceCounters")]
        public PerfCounterSection PerfCounters {
            get {
                return (PerfCounterSection) base[_propPerfCounters];
            }
        }
    }
}
    
