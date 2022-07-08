// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Configuration;

namespace System.Diagnostics
{
    internal sealed class SystemDiagnosticsSection : ConfigurationSection
    {
        private static readonly ConfigurationPropertyCollection _properties = new();
        private static readonly ConfigurationProperty _propSources = new ConfigurationProperty("sources", typeof(SourceElementsCollection), new SourceElementsCollection(), ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propSharedListeners = new ConfigurationProperty("sharedListeners", typeof(SharedListenerElementsCollection), new SharedListenerElementsCollection(), ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propSwitches = new ConfigurationProperty("switches", typeof(SwitchElementsCollection), new SwitchElementsCollection(), ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propTrace = new ConfigurationProperty("trace", typeof(TraceSection), new TraceSection(), ConfigurationPropertyOptions.None);

        static SystemDiagnosticsSection()
        {
            _properties.Add(_propSources);
            _properties.Add(_propSharedListeners);
            _properties.Add(_propSwitches);
            _properties.Add(_propTrace);
        }

        protected internal override ConfigurationPropertyCollection Properties
        {
            get
            {
                return _properties;
            }
        }

        [ConfigurationProperty("sources")]
        public SourceElementsCollection Sources
        {
            get
            {
                return (SourceElementsCollection)base[_propSources];
            }
        }

        [ConfigurationProperty("sharedListeners")]
        public ListenerElementsCollection SharedListeners
        {
            get
            {
                return (ListenerElementsCollection)base[_propSharedListeners];
            }
        }

        [ConfigurationProperty("switches")]
        public SwitchElementsCollection Switches
        {
            get
            {
                return (SwitchElementsCollection)base[_propSwitches];
            }
        }

        [ConfigurationProperty("trace")]
        public TraceSection Trace
        {
            get
            {
                return (TraceSection)base[_propTrace];
            }
        }

        protected internal override void InitializeDefault()
        {
            Trace.Listeners?.InitializeDefaultInternal();
        }
    }
}
