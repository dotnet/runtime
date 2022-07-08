//------------------------------------------------------------------------------
// <copyright file="TraceSection.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Configuration;

namespace System.Diagnostics {
    internal class TraceSection : ConfigurationElement {
        private static readonly ConfigurationPropertyCollection _properties;
        private static readonly ConfigurationProperty _propListeners        = new ConfigurationProperty("listeners", typeof(ListenerElementsCollection), new ListenerElementsCollection(), ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propAutoFlush        = new ConfigurationProperty("autoflush", typeof(bool), false, ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propIndentSize       = new ConfigurationProperty("indentsize", typeof(int), 4, ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propUseGlobalLock    = new ConfigurationProperty("useGlobalLock", typeof(bool), true, ConfigurationPropertyOptions.None);

        static TraceSection() {
            _properties = new ConfigurationPropertyCollection();
            _properties.Add(_propListeners);
            _properties.Add(_propAutoFlush);
            _properties.Add(_propIndentSize);
            _properties.Add(_propUseGlobalLock);
        }

        [ConfigurationProperty( "autoflush", DefaultValue=false )]
        public bool AutoFlush {
            get { 
                return (bool) this[_propAutoFlush]; 
            }
        }

        [ConfigurationProperty( "indentsize", DefaultValue=4 )]
        public int IndentSize {
            get { 
                return (int) this[_propIndentSize]; 
            }
        }

        [ConfigurationProperty( "listeners" )]
        public ListenerElementsCollection Listeners {
            get { 
                return (ListenerElementsCollection) this[_propListeners]; 
            }
        }

        [ConfigurationProperty( "useGlobalLock", DefaultValue = true)]
        public bool UseGlobalLock {
            get { 
                return (bool) this[_propUseGlobalLock]; 
            }
        }

        protected override ConfigurationPropertyCollection Properties {
            get {
                return _properties;
            }
        }
    }
}

