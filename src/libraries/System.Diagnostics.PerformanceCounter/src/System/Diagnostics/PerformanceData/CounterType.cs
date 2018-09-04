// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics.PerformanceData
{
    /// <summary>
    /// Enum of friendly names to counter types (maps directory to the native types defined in winperf.h).
    /// </summary>    
    public enum CounterType
    {
        QueueLength = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_QUEUELEN_TYPE,
        LargeQueueLength = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_LARGE_QUEUELEN_TYPE,
        QueueLength100Ns = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_100NS_QUEUELEN_TYPE,
        QueueLengthObjectTime = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_OBJ_TIME_QUEUELEN_TYPE,
        RawData32 = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_RAWCOUNT,
        RawData64 = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_LARGE_RAWCOUNT,
        RawDataHex32 = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_RAWCOUNT_HEX,
        RawDataHex64 = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_LARGE_RAWCOUNT_HEX,
        RateOfCountPerSecond32 = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_COUNTER,
        RateOfCountPerSecond64 = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_BULK_COUNT,
        RawFraction32 = Interop.Kernel32.PerformanceCounterOptions.PERF_RAW_FRACTION,
        RawFraction64 = Interop.Kernel32.PerformanceCounterOptions.PERF_LARGE_RAW_FRACTION,
        RawBase32 = Interop.Kernel32.PerformanceCounterOptions.PERF_RAW_BASE,
        RawBase64 = Interop.Kernel32.PerformanceCounterOptions.PERF_LARGE_RAW_BASE,
        SampleFraction = Interop.Kernel32.PerformanceCounterOptions.PERF_SAMPLE_FRACTION,
        SampleCounter = Interop.Kernel32.PerformanceCounterOptions.PERF_SAMPLE_COUNTER,
        SampleBase = Interop.Kernel32.PerformanceCounterOptions.PERF_SAMPLE_BASE,
        AverageTimer32 = Interop.Kernel32.PerformanceCounterOptions.PERF_AVERAGE_TIMER,
        AverageBase = Interop.Kernel32.PerformanceCounterOptions.PERF_AVERAGE_BASE,
        AverageCount64 = Interop.Kernel32.PerformanceCounterOptions.PERF_AVERAGE_BULK,
        PercentageActive = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_TIMER,
        PercentageNotActive = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_TIMER_INV,
        PercentageActive100Ns = Interop.Kernel32.PerformanceCounterOptions.PERF_100NSEC_TIMER,
        PercentageNotActive100Ns = Interop.Kernel32.PerformanceCounterOptions.PERF_100NSEC_TIMER_INV,
        ElapsedTime = Interop.Kernel32.PerformanceCounterOptions.PERF_ELAPSED_TIME,
        MultiTimerPercentageActive = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_MULTI_TIMER,
        MultiTimerPercentageNotActive = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_MULTI_TIMER_INV,
        MultiTimerPercentageActive100Ns = Interop.Kernel32.PerformanceCounterOptions.PERF_100NSEC_MULTI_TIMER,
        MultiTimerPercentageNotActive100Ns = Interop.Kernel32.PerformanceCounterOptions.PERF_100NSEC_MULTI_TIMER_INV,
        MultiTimerBase = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_MULTI_BASE,
        Delta32 = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_DELTA,
        Delta64 = Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_LARGE_DELTA,
        ObjectSpecificTimer = Interop.Kernel32.PerformanceCounterOptions.PERF_OBJ_TIME_TIMER,
        PrecisionSystemTimer = Interop.Kernel32.PerformanceCounterOptions.PERF_PRECISION_SYSTEM_TIMER,
        PrecisionTimer100Ns = Interop.Kernel32.PerformanceCounterOptions.PERF_PRECISION_100NS_TIMER,
        PrecisionObjectSpecificTimer = Interop.Kernel32.PerformanceCounterOptions.PERF_PRECISION_OBJECT_TIMER
    }
}
