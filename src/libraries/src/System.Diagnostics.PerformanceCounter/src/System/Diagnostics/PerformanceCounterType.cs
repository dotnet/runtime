// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    /// <summary>
    ///     Enum of friendly names to counter types (maps directory to the native types)
    /// </summary>
    public enum PerformanceCounterType
    {
        NumberOfItems32 = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_RAWCOUNT,
        NumberOfItems64 = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_LARGE_RAWCOUNT,
        NumberOfItemsHEX32 = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_RAWCOUNT_HEX,
        NumberOfItemsHEX64 = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_LARGE_RAWCOUNT_HEX,
        RateOfCountsPerSecond32 = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_COUNTER,
        RateOfCountsPerSecond64 = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_BULK_COUNT,
        CountPerTimeInterval32 = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_QUEUELEN_TYPE,
        CountPerTimeInterval64 = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_LARGE_QUEUELEN_TYPE,
        RawFraction = Interop.Kernel32.PerformanceCounterOptions.PERF_RAW_FRACTION,
        RawBase = Interop.Kernel32.PerformanceCounterOptions.PERF_RAW_BASE,

        AverageTimer32 = Interop.Kernel32.PerformanceCounterOptions.PERF_AVERAGE_TIMER,
        AverageBase = Interop.Kernel32.PerformanceCounterOptions.PERF_AVERAGE_BASE,
        AverageCount64 = Interop.Kernel32.PerformanceCounterOptions.PERF_AVERAGE_BULK,

        SampleFraction = Interop.Kernel32.PerformanceCounterOptions.PERF_SAMPLE_FRACTION,
        SampleCounter = Interop.Kernel32.PerformanceCounterOptions.PERF_SAMPLE_COUNTER,
        SampleBase = Interop.Kernel32.PerformanceCounterOptions.PERF_SAMPLE_BASE,

        CounterTimer = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_TIMER,
        CounterTimerInverse = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_TIMER_INV,
        Timer100Ns = Interop.Kernel32.PerformanceCounterOptions.PERF_100NSEC_TIMER,
        Timer100NsInverse = Interop.Kernel32.PerformanceCounterOptions.PERF_100NSEC_TIMER_INV,
        ElapsedTime = Interop.Kernel32.PerformanceCounterOptions.PERF_ELAPSED_TIME,
        CounterMultiTimer = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_MULTI_TIMER,
        CounterMultiTimerInverse = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_MULTI_TIMER_INV,
        CounterMultiTimer100Ns = Interop.Kernel32.PerformanceCounterOptions.PERF_100NSEC_MULTI_TIMER,
        CounterMultiTimer100NsInverse = Interop.Kernel32.PerformanceCounterOptions.PERF_100NSEC_MULTI_TIMER_INV,
        CounterMultiBase = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_MULTI_BASE,

        CounterDelta32 = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_DELTA,
        CounterDelta64 = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_LARGE_DELTA
    }
}