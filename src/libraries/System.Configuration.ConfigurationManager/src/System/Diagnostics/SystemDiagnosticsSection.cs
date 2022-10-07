// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration;

namespace System.Diagnostics
{
    internal sealed class SystemDiagnosticsSection : ConfigurationSection
    {
        private static readonly ConfigurationPropertyCollection s_properties = new();
        private static readonly ConfigurationProperty s_propSources = new("sources", typeof(SourceElementsCollection), new SourceElementsCollection(), ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty s_propSharedListeners = new("sharedListeners", typeof(SharedListenerElementsCollection), new SharedListenerElementsCollection(), ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty s_propSwitches = new("switches", typeof(SwitchElementsCollection), new SwitchElementsCollection(), ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty s_propTrace = new("trace", typeof(TraceSection), new TraceSection(), ConfigurationPropertyOptions.None);

        static SystemDiagnosticsSection()
        {
            s_properties.Add(s_propSources);
            s_properties.Add(s_propSharedListeners);
            s_properties.Add(s_propSwitches);
            s_properties.Add(s_propTrace);
        }

        protected internal override ConfigurationPropertyCollection Properties => s_properties;

        [ConfigurationProperty("sources")]
        public SourceElementsCollection Sources => (SourceElementsCollection)base[s_propSources];

        [ConfigurationProperty("sharedListeners")]
        public ListenerElementsCollection SharedListeners => (ListenerElementsCollection)base[s_propSharedListeners];

        [ConfigurationProperty("switches")]
        public SwitchElementsCollection Switches => (SwitchElementsCollection)base[s_propSwitches];

        [ConfigurationProperty("trace")]
        public TraceSection Trace => (TraceSection)base[s_propTrace];

        protected internal override void InitializeDefault()
        {
            Trace.Listeners?.InitializeDefaultInternal();
        }
    }
}
