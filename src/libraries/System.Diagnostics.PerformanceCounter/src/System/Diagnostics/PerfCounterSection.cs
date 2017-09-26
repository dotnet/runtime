// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;

namespace System.Diagnostics {
    internal class PerfCounterSection : ConfigurationElement {
        private static readonly ConfigurationPropertyCollection _properties;
        private static readonly ConfigurationProperty _propFileMappingSize = new ConfigurationProperty("filemappingsize", typeof(int), 524288, ConfigurationPropertyOptions.None);

        static PerfCounterSection(){
            _properties = new ConfigurationPropertyCollection();
            _properties.Add(_propFileMappingSize);
        }

        [ConfigurationProperty("filemappingsize", DefaultValue = 524288)]
        public int FileMappingSize {
            get { 
                return (int) this[_propFileMappingSize]; 
            }
        }

        protected override ConfigurationPropertyCollection Properties {
            get {
                return _properties;
            }
        }
    }
}

