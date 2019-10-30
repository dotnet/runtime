// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

namespace System.Diagnostics
{
    /// <summary>
    ///     A class defining the counter type, name and help string for a custom counter.
    /// </summary>
    public class CounterCreationData
    {
        private PerformanceCounterType _counterType = PerformanceCounterType.NumberOfItems32;
        private string _counterName = string.Empty;
        private string _counterHelp = string.Empty;

        public CounterCreationData()
        {
        }

        public CounterCreationData(string counterName, string counterHelp, PerformanceCounterType counterType)
        {
            CounterType = counterType;
            CounterName = counterName;
            CounterHelp = counterHelp;
        }

        public PerformanceCounterType CounterType
        {
            get
            {
                return _counterType;
            }
            set
            {
                if (!Enum.IsDefined(typeof(PerformanceCounterType), value))
                    throw new InvalidEnumArgumentException(nameof(PerformanceCounterType), (int)value, typeof(PerformanceCounterType));

                _counterType = value;
            }
        }

        public string CounterName
        {
            get
            {
                return _counterName;
            }
            set
            {
                PerformanceCounterCategory.CheckValidCounter(value);
                _counterName = value;
            }
        }

        public string CounterHelp
        {
            get
            {
                return _counterHelp;
            }
            set
            {
                PerformanceCounterCategory.CheckValidHelp(value);
                _counterHelp = value;
            }
        }
    }
}
