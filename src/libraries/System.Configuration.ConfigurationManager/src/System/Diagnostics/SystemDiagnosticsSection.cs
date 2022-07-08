//------------------------------------------------------------------------------
// <copyright file="SystemDiagnosticsSection.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System.Configuration;

namespace System.Diagnostics {
    internal class SystemDiagnosticsSection : ConfigurationSection {
        private static readonly ConfigurationPropertyCollection _properties;
        private static readonly ConfigurationProperty _propAssert = new ConfigurationProperty("assert", typeof(AssertSection), new AssertSection(), ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propPerfCounters = new ConfigurationProperty("performanceCounters", typeof(PerfCounterSection), new PerfCounterSection(), ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propSources = new ConfigurationProperty("sources", typeof(SourceElementsCollection), new SourceElementsCollection(), ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propSharedListeners = new ConfigurationProperty("sharedListeners", typeof(SharedListenerElementsCollection), new SharedListenerElementsCollection(), ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propSwitches = new ConfigurationProperty("switches", typeof(SwitchElementsCollection), new SwitchElementsCollection(), ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propTrace = new ConfigurationProperty("trace", typeof(TraceSection), new TraceSection(), ConfigurationPropertyOptions.None);

        static SystemDiagnosticsSection() {
            _properties = new ConfigurationPropertyCollection();
            _properties.Add(_propAssert);
            _properties.Add(_propPerfCounters);
            _properties.Add(_propSources);
            _properties.Add(_propSharedListeners);
            _properties.Add(_propSwitches);
            _properties.Add(_propTrace);
        }

        [ConfigurationProperty("assert")]
        public AssertSection Assert {
            get {
                return (AssertSection) base[_propAssert];
            }
        }

        [ConfigurationProperty("performanceCounters")]
        public PerfCounterSection PerfCounters {
            get {
                return (PerfCounterSection) base[_propPerfCounters];
            }
        }

        protected override ConfigurationPropertyCollection Properties {
            get {
                return _properties;
            }
        }

        [ConfigurationProperty("sources")]
        public SourceElementsCollection  Sources {
            get {
                return (SourceElementsCollection ) base[_propSources];
            }
        }

        [ConfigurationProperty("sharedListeners")]
        public ListenerElementsCollection SharedListeners {
            get {
                return (ListenerElementsCollection) base[_propSharedListeners];
            }
        }

        [ConfigurationProperty("switches")]
        public SwitchElementsCollection Switches {
            get {
                return (SwitchElementsCollection) base[_propSwitches];
            }
        }

        [ConfigurationProperty("trace")]
        public TraceSection Trace {
            get {
                return (TraceSection) base[_propTrace];
            }
        }

        protected override void InitializeDefault() {
            Trace.Listeners.InitializeDefaultInternal();
        }
    }
}
    
