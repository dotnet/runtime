// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics {

    using System.Diagnostics;

    using System;

    /// <summary>
    ///     A struct holding the raw data for a performance counter.
    /// </summary>    
    public struct CounterSample {
        private long rawValue;
        private long baseValue;
        private long timeStamp;
        private long counterFrequency;
        private PerformanceCounterType counterType;
        private long timeStamp100nSec;
        private long systemFrequency;
        private long counterTimeStamp;
    
        public static CounterSample Empty = new CounterSample(0, 0, 0, 0, 0, 0, PerformanceCounterType.NumberOfItems32);

        public CounterSample(long rawValue, long baseValue, long counterFrequency, long systemFrequency, long timeStamp, long timeStamp100nSec, PerformanceCounterType counterType) {
            this.rawValue = rawValue;
            this.baseValue = baseValue;
            this.timeStamp = timeStamp;
            this.counterFrequency = counterFrequency;
            this.counterType = counterType;
            this.timeStamp100nSec = timeStamp100nSec;
            this.systemFrequency = systemFrequency;
            this.counterTimeStamp = 0;
        }

        public CounterSample(long rawValue, long baseValue, long counterFrequency, long systemFrequency, long timeStamp, long timeStamp100nSec, PerformanceCounterType counterType, long counterTimeStamp) {
            this.rawValue = rawValue;
            this.baseValue = baseValue;
            this.timeStamp = timeStamp;
            this.counterFrequency = counterFrequency;
            this.counterType = counterType;
            this.timeStamp100nSec = timeStamp100nSec;
            this.systemFrequency = systemFrequency;
            this.counterTimeStamp = counterTimeStamp;
        }         
         
        /// <summary>
        ///      Raw value of the counter.
        /// </summary>
        public long RawValue {
            get {
                return this.rawValue;
            }
        }

        internal ulong UnsignedRawValue {
             get {
                return (ulong)this.rawValue;
            }
        }
        
        /// <summary>
        ///      Optional base raw value for the counter (only used if multiple counter based).
        /// </summary>
        public long BaseValue {
            get {
                return this.baseValue;
            }
        }
        
        /// <summary>
        ///      Raw system frequency
        /// </summary>
        public long SystemFrequency {
            get {
               return this.systemFrequency;
            }
        }

        /// <summary>
        ///      Raw counter frequency
        /// </summary>
        public long CounterFrequency {
            get {
                return this.counterFrequency;
            }
        }

        /// <summary>
        ///      Raw counter frequency
        /// </summary>
        public long CounterTimeStamp {
            get {
                return this.counterTimeStamp;
            }
        }
        
        /// <summary>
        ///      Raw timestamp
        /// </summary>
        public long TimeStamp {
            get {
                return this.timeStamp;
            }
        }

        /// <summary>
        ///      Raw high fidelity timestamp
        /// </summary>
        public long TimeStamp100nSec {
            get {
                return this.timeStamp100nSec;
            }
        }
        
        /// <summary>
        ///      Counter type
        /// </summary>
        public PerformanceCounterType CounterType {
            get {
                return this.counterType;
            }
        }

        /// <summary>
        ///    Static functions to calculate the performance value off the sample
        /// </summary>
        public static float Calculate(CounterSample counterSample) {
            return CounterSampleCalculator.ComputeCounterValue(counterSample);
        }

        /// <summary>
        ///    Static functions to calculate the performance value off the samples
        /// </summary>
        public static float Calculate(CounterSample counterSample, CounterSample nextCounterSample) { 
            return CounterSampleCalculator.ComputeCounterValue(counterSample, nextCounterSample);
        }

        public override bool Equals(Object o) {
            return ( o is CounterSample) && Equals((CounterSample)o);               
        }
        
        public bool Equals(CounterSample sample) {
            return (rawValue == sample.rawValue) && 
                       (baseValue == sample.baseValue) && 
                       (timeStamp == sample.timeStamp) && 
                       (counterFrequency == sample.counterFrequency) &&
                       (counterType == sample.counterType) &&
                       (timeStamp100nSec == sample.timeStamp100nSec) && 
                       (systemFrequency == sample.systemFrequency) &&
                       (counterTimeStamp == sample.counterTimeStamp);                       
        }

        public override int GetHashCode() {
            return rawValue.GetHashCode();                
        }

        public static bool operator ==(CounterSample a, CounterSample b) {
            return a.Equals(b);
        }        

        public static bool operator !=(CounterSample a, CounterSample b) {
            return !(a.Equals(b));
        }
        
    }
}
