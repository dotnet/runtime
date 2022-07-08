// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration;

namespace System.Diagnostics
{
    internal sealed class TraceSection : ConfigurationElement
    {
        private static readonly ConfigurationPropertyCollection _properties = new();
        private static readonly ConfigurationProperty _propListeners = new ConfigurationProperty("listeners", typeof(ListenerElementsCollection), null, ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propAutoFlush = new ConfigurationProperty("autoflush", typeof(bool), false, ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propIndentSize = new ConfigurationProperty("indentsize", typeof(int), 4, ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propUseGlobalLock = new ConfigurationProperty("useGlobalLock", typeof(bool), true, ConfigurationPropertyOptions.None);

        static TraceSection()
        {
            _properties.Add(_propListeners);
            _properties.Add(_propAutoFlush);
            _properties.Add(_propIndentSize);
            _properties.Add(_propUseGlobalLock);
        }

        [ConfigurationProperty("autoflush", DefaultValue = false)]
        public bool AutoFlush
        {
            get
            {
                return (bool)this[_propAutoFlush];
            }
        }

        [ConfigurationProperty("indentsize", DefaultValue = 4)]
        public int IndentSize
        {
            get
            {
                return (int)this[_propIndentSize];
            }
        }

        [ConfigurationProperty("listeners")]
        public ListenerElementsCollection Listeners
        {
            get
            {
                return (ListenerElementsCollection)this[_propListeners];
            }
        }

        [ConfigurationProperty("useGlobalLock", DefaultValue = true)]
        public bool UseGlobalLock
        {
            get
            {
                return (bool)this[_propUseGlobalLock];
            }
        }

        protected internal override ConfigurationPropertyCollection Properties
        {
            get
            {
                return _properties;
            }
        }
    }
}
