// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics {

    using System.Diagnostics;

    using System;
    using System.ComponentModel;
    
    /// <summary>
    ///     A struct defining the counter type, name and help string for a custom counter.
    /// </summary>
    public class CounterCreationData {
        private PerformanceCounterType counterType = PerformanceCounterType.NumberOfItems32;
        private string counterName = String.Empty;
        private string counterHelp = String.Empty;

        public CounterCreationData() {            
        }
    
        public CounterCreationData(string counterName, string counterHelp, PerformanceCounterType counterType) {
            CounterType = counterType;
            CounterName = counterName;
            CounterHelp = counterHelp;
        }

        public PerformanceCounterType CounterType {
            get {
                return counterType;
            }
            set {
                if (!Enum.IsDefined(typeof(PerformanceCounterType), value)) 
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(PerformanceCounterType));
            
                counterType = value;
            }
        }

        public string CounterName {
            get {
                return counterName;
            }
            set {
                PerformanceCounterCategory.CheckValidCounter(value);
                counterName = value;
            }
        }

        public string CounterHelp {
            get {
                return counterHelp;
            }
            set {
                PerformanceCounterCategory.CheckValidHelp(value);
                counterHelp = value;
            }
        }
    }
}
