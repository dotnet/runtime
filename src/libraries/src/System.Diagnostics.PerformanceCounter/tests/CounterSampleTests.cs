// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Specialized;
using Xunit;

namespace System.Diagnostics.Tests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap)] // In appcontainer, cannot write to perf counters
    public static class CounterSampleTests
    {
        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void CounterSample_Constructor_EmptyCounterSample()
        {
            CounterSample counterSample = new CounterSample();

            Assert.Equal(0, counterSample.BaseValue);
            Assert.Equal(0, counterSample.CounterFrequency);
            Assert.Equal(PerformanceCounterType.NumberOfItemsHEX32, counterSample.CounterType);
            Assert.Equal(0, counterSample.RawValue);
            Assert.Equal(0, counterSample.SystemFrequency);
            Assert.Equal(0, counterSample.TimeStamp);
            Assert.Equal(0, counterSample.TimeStamp100nSec);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void CounterSample_Constructor_CounterSample()
        {
            long timeStamp = DateTime.Now.ToFileTime();
            CounterSample counterSample = new CounterSample(1, 2, 3, 4, timeStamp, timeStamp, PerformanceCounterType.SampleFraction);

            Assert.Equal(2, counterSample.BaseValue);
            Assert.Equal(3, counterSample.CounterFrequency);
            Assert.Equal(PerformanceCounterType.SampleFraction, counterSample.CounterType);
            Assert.Equal(1, counterSample.RawValue);
            Assert.Equal(4, counterSample.SystemFrequency);
            Assert.Equal(timeStamp, counterSample.TimeStamp);
            Assert.Equal(timeStamp, counterSample.TimeStamp100nSec);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void CounterSample_Calculate_CalculateCounterSample()
        {
            CounterSample counterSample = new CounterSample(5, 0, 0, 0, 0, 0, PerformanceCounterType.NumberOfItems32);

            Assert.Equal(5, CounterSample.Calculate(counterSample));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void CounterSample_Calculate_CalculateCounterSampleCounterSample()
        {
            CounterSample counterSample1 = new CounterSample(5, 0, 0, 1, 0, 0, PerformanceCounterType.CounterDelta32);
            CounterSample counterSample2 = new CounterSample(15, 0, 0, 1, 0, 0, PerformanceCounterType.CounterDelta32);

            Assert.Equal(10, CounterSample.Calculate(counterSample1, counterSample2));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void CounterSample_Equal()
        {
            CounterSample counterSample1 = new CounterSample(5, 0, 0, 1, 0, 0, PerformanceCounterType.CounterDelta32);
            CounterSample counterSample2 = new CounterSample(5, 0, 0, 1, 0, 0, PerformanceCounterType.CounterDelta32);

            Assert.Equal(counterSample1, counterSample2);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void CounterSample_opInequality()
        {
            CounterSample counterSample1 = new CounterSample(5, 0, 0, 1, 0, 0, PerformanceCounterType.CounterDelta32);
            CounterSample counterSample2 = new CounterSample(15, 0, 0, 1, 0, 0, PerformanceCounterType.CounterDelta32);

            Assert.True(counterSample1 != counterSample2);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void CounterSample_opEquality()
        {
            CounterSample counterSample1 = new CounterSample(5, 0, 0, 1, 0, 0, PerformanceCounterType.CounterDelta32);
            CounterSample counterSample2 = new CounterSample(5, 0, 0, 1, 0, 0, PerformanceCounterType.CounterDelta32);

            Assert.True(counterSample1 == counterSample2);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void CounterSample_GetHashCode()
        {
            CounterSample counterSample1 = new CounterSample(5, 0, 0, 1, 0, 0, PerformanceCounterType.CounterDelta32);
            CounterSample counterSample2 = new CounterSample(5, 0, 0, 1, 0, 0, PerformanceCounterType.CounterDelta32);

            Assert.Equal(counterSample1.GetHashCode(), counterSample2.GetHashCode());
        }
    }
}
