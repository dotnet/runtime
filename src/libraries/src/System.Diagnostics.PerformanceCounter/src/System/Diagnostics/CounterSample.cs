// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    /// <summary>
    ///     A struct holding the raw data for a performance counter.
    /// </summary>    
    public readonly struct CounterSample
    {
        private readonly long _rawValue;
        private readonly long _baseValue;
        private readonly long _timeStamp;
        private readonly long _counterFrequency;
        private readonly PerformanceCounterType _counterType;
        private readonly long _timeStamp100nSec;
        private readonly long _systemFrequency;
        private readonly long _counterTimeStamp;

        public static CounterSample Empty = new CounterSample(0, 0, 0, 0, 0, 0, PerformanceCounterType.NumberOfItems32);

        public CounterSample(long rawValue, long baseValue, long counterFrequency, long systemFrequency, long timeStamp, long timeStamp100nSec, PerformanceCounterType counterType)
        {
            _rawValue = rawValue;
            _baseValue = baseValue;
            _timeStamp = timeStamp;
            _counterFrequency = counterFrequency;
            _counterType = counterType;
            _timeStamp100nSec = timeStamp100nSec;
            _systemFrequency = systemFrequency;
            _counterTimeStamp = 0;
        }

        public CounterSample(long rawValue, long baseValue, long counterFrequency, long systemFrequency, long timeStamp, long timeStamp100nSec, PerformanceCounterType counterType, long counterTimeStamp)
        {
            _rawValue = rawValue;
            _baseValue = baseValue;
            _timeStamp = timeStamp;
            _counterFrequency = counterFrequency;
            _counterType = counterType;
            _timeStamp100nSec = timeStamp100nSec;
            _systemFrequency = systemFrequency;
            _counterTimeStamp = counterTimeStamp;
        }

        /// <summary>
        ///      Raw value of the counter.
        /// </summary>
        public long RawValue
        {
            get
            {
                return _rawValue;
            }
        }

        internal ulong UnsignedRawValue
        {
            get
            {
                return (ulong)_rawValue;
            }
        }

        /// <summary>
        ///      Optional base raw value for the counter (only used if multiple counter based).
        /// </summary>
        public long BaseValue
        {
            get
            {
                return _baseValue;
            }
        }

        /// <summary>
        ///      Raw system frequency
        /// </summary>
        public long SystemFrequency
        {
            get
            {
                return _systemFrequency;
            }
        }

        /// <summary>
        ///      Raw counter frequency
        /// </summary>
        public long CounterFrequency
        {
            get
            {
                return _counterFrequency;
            }
        }

        /// <summary>
        ///      Raw counter frequency
        /// </summary>
        public long CounterTimeStamp
        {
            get
            {
                return _counterTimeStamp;
            }
        }

        /// <summary>
        ///      Raw timestamp
        /// </summary>
        public long TimeStamp
        {
            get
            {
                return _timeStamp;
            }
        }

        /// <summary>
        ///      Raw high fidelity timestamp
        /// </summary>
        public long TimeStamp100nSec
        {
            get
            {
                return _timeStamp100nSec;
            }
        }

        /// <summary>
        ///      Counter type
        /// </summary>
        public PerformanceCounterType CounterType
        {
            get
            {
                return _counterType;
            }
        }

        /// <summary>
        ///    Static functions to calculate the performance value off the sample
        /// </summary>
        public static float Calculate(CounterSample counterSample)
        {
            return CounterSampleCalculator.ComputeCounterValue(counterSample);
        }

        /// <summary>
        ///    Static functions to calculate the performance value off the samples
        /// </summary>
        public static float Calculate(CounterSample counterSample, CounterSample nextCounterSample)
        {
            return CounterSampleCalculator.ComputeCounterValue(counterSample, nextCounterSample);
        }

        public override bool Equals(Object o)
        {
            return (o is CounterSample) && Equals((CounterSample)o);
        }

        public bool Equals(CounterSample sample)
        {
            return (_rawValue == sample._rawValue) &&
                       (_baseValue == sample._baseValue) &&
                       (_timeStamp == sample._timeStamp) &&
                       (_counterFrequency == sample._counterFrequency) &&
                       (_counterType == sample._counterType) &&
                       (_timeStamp100nSec == sample._timeStamp100nSec) &&
                       (_systemFrequency == sample._systemFrequency) &&
                       (_counterTimeStamp == sample._counterTimeStamp);
        }

        public override int GetHashCode()
        {
            return _rawValue.GetHashCode();
        }

        public static bool operator ==(CounterSample a, CounterSample b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(CounterSample a, CounterSample b)
        {
            return !(a.Equals(b));
        }

    }
}