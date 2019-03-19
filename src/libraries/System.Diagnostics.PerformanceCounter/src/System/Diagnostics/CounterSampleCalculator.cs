// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Globalization;

namespace System.Diagnostics
{
    /// <summary>
    ///     Set of utility functions for interpreting the counter data
    /// </summary>
    public static class CounterSampleCalculator
    {
        private static volatile bool s_perfCounterDllLoaded = false;

        /// <summary>
        ///    Converts 100NS elapsed time to fractional seconds
        /// </summary>
        /// <internalonly/>
        private static float GetElapsedTime(CounterSample oldSample, CounterSample newSample)
        {
            float eSeconds;
            float eDifference;

            if (newSample.RawValue == 0)
            {
                // no data [start time = 0] so return 0
                return 0.0f;
            }
            else
            {
                float eFreq;
                eFreq = (float)(ulong)oldSample.CounterFrequency;

                if (oldSample.UnsignedRawValue >= (ulong)newSample.CounterTimeStamp || eFreq <= 0.0f)
                    return 0.0f;

                // otherwise compute difference between current time and start time
                eDifference = (float)((ulong)newSample.CounterTimeStamp - oldSample.UnsignedRawValue);

                // convert to fractional seconds using object counter
                eSeconds = eDifference / eFreq;

                return eSeconds;
            }
        }

        /// <summary>
        ///    Computes the calculated value given a raw counter sample.
        /// </summary>
        public static float ComputeCounterValue(CounterSample newSample)
        {
            return ComputeCounterValue(CounterSample.Empty, newSample);
        }

        /// <summary>
        ///    Computes the calculated value given a raw counter sample.
        /// </summary>
        public static float ComputeCounterValue(CounterSample oldSample, CounterSample newSample)
        {
            int newCounterType = (int)newSample.CounterType;
            if (oldSample.SystemFrequency == 0)
            {
                if ((newCounterType != Interop.Kernel32.PerformanceCounterOptions.PERF_RAW_FRACTION) &&
                    (newCounterType != Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_RAWCOUNT) &&
                    (newCounterType != Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_RAWCOUNT_HEX) &&
                    (newCounterType != Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_LARGE_RAWCOUNT) &&
                    (newCounterType != Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_LARGE_RAWCOUNT_HEX) &&
                    (newCounterType != Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_MULTI_BASE))
                {

                    // Since oldSample has a system frequency of 0, this means the newSample is the first sample
                    // on a two sample calculation.  Since we can't do anything with it, return 0.
                    return 0.0f;
                }
            }
            else if (oldSample.CounterType != newSample.CounterType)
            {
                throw new InvalidOperationException(SR.MismatchedCounterTypes);
            }

            if (newCounterType == Interop.Kernel32.PerformanceCounterOptions.PERF_ELAPSED_TIME)
                return (float)GetElapsedTime(oldSample, newSample);

            Interop.Kernel32.PerformanceCounterOptions.PDH_RAW_COUNTER newPdhValue = new Interop.Kernel32.PerformanceCounterOptions.PDH_RAW_COUNTER();
            Interop.Kernel32.PerformanceCounterOptions.PDH_RAW_COUNTER oldPdhValue = new Interop.Kernel32.PerformanceCounterOptions.PDH_RAW_COUNTER();

            FillInValues(oldSample, newSample, ref oldPdhValue, ref newPdhValue);

            LoadPerfCounterDll();

            Interop.Kernel32.PerformanceCounterOptions.PDH_FMT_COUNTERVALUE pdhFormattedValue = new Interop.Kernel32.PerformanceCounterOptions.PDH_FMT_COUNTERVALUE();
            long timeBase = newSample.SystemFrequency;
            int result = Interop.PerfCounter.FormatFromRawValue((uint)newCounterType, Interop.Kernel32.PerformanceCounterOptions.PDH_FMT_DOUBLE | Interop.Kernel32.PerformanceCounterOptions.PDH_FMT_NOSCALE | Interop.Kernel32.PerformanceCounterOptions.PDH_FMT_NOCAP100,
                                                          ref timeBase, ref newPdhValue, ref oldPdhValue, ref pdhFormattedValue);

            if (result != Interop.Errors.ERROR_SUCCESS)
            {
                // If the numbers go negative, just return 0.  This better matches the old behavior. 
                if (result == Interop.Kernel32.PerformanceCounterOptions.PDH_CALC_NEGATIVE_VALUE || result == Interop.Kernel32.PerformanceCounterOptions.PDH_CALC_NEGATIVE_DENOMINATOR || result == Interop.Kernel32.PerformanceCounterOptions.PDH_NO_DATA)
                    return 0;
                else
                    throw new Win32Exception(result, SR.Format(SR.PerfCounterPdhError, result.ToString("x", CultureInfo.InvariantCulture)));
            }

            return (float)pdhFormattedValue.data;

        }

        // This method figures out which values are supposed to go into which structures so that PDH can do the 
        // calculation for us.  This was ported from Window's cutils.c
        private static void FillInValues(CounterSample oldSample, CounterSample newSample, ref Interop.Kernel32.PerformanceCounterOptions.PDH_RAW_COUNTER oldPdhValue, ref Interop.Kernel32.PerformanceCounterOptions.PDH_RAW_COUNTER newPdhValue)
        {
            int newCounterType = (int)newSample.CounterType;

            switch (newCounterType)
            {
                case Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_COUNTER:
                case Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_QUEUELEN_TYPE:
                case Interop.Kernel32.PerformanceCounterOptions.PERF_SAMPLE_COUNTER:
                case Interop.Kernel32.PerformanceCounterOptions.PERF_OBJ_TIME_TIMER:
                case Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_OBJ_TIME_QUEUELEN_TYPE:
                    newPdhValue.FirstValue = newSample.RawValue;
                    newPdhValue.SecondValue = newSample.TimeStamp;

                    oldPdhValue.FirstValue = oldSample.RawValue;
                    oldPdhValue.SecondValue = oldSample.TimeStamp;
                    break;

                case Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_100NS_QUEUELEN_TYPE:
                    newPdhValue.FirstValue = newSample.RawValue;
                    newPdhValue.SecondValue = newSample.TimeStamp100nSec;

                    oldPdhValue.FirstValue = oldSample.RawValue;
                    oldPdhValue.SecondValue = oldSample.TimeStamp100nSec;
                    break;

                case Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_TIMER:
                case Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_TIMER_INV:
                case Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_BULK_COUNT:
                case Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_LARGE_QUEUELEN_TYPE:
                case Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_MULTI_TIMER:
                case Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_MULTI_TIMER_INV:
                    newPdhValue.FirstValue = newSample.RawValue;
                    newPdhValue.SecondValue = newSample.TimeStamp;

                    oldPdhValue.FirstValue = oldSample.RawValue;
                    oldPdhValue.SecondValue = oldSample.TimeStamp;
                    if (newCounterType == Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_MULTI_TIMER || newCounterType == Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_MULTI_TIMER_INV)
                    {
                        //  this is to make PDH work like PERFMON for
                        //  this counter type
                        newPdhValue.FirstValue *= (uint)newSample.CounterFrequency;
                        if (oldSample.CounterFrequency != 0)
                        {
                            oldPdhValue.FirstValue *= (uint)oldSample.CounterFrequency;
                        }
                    }

                    if ((newCounterType & Interop.Kernel32.PerformanceCounterOptions.PERF_MULTI_COUNTER) == Interop.Kernel32.PerformanceCounterOptions.PERF_MULTI_COUNTER)
                    {
                        newPdhValue.MultiCount = (int)newSample.BaseValue;
                        oldPdhValue.MultiCount = (int)oldSample.BaseValue;
                    }

                    break;
                //
                //  These counters do not use any time reference
                //
                case Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_RAWCOUNT:
                case Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_RAWCOUNT_HEX:
                case Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_DELTA:
                case Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_LARGE_RAWCOUNT:
                case Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_LARGE_RAWCOUNT_HEX:
                case Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_LARGE_DELTA:
                    newPdhValue.FirstValue = newSample.RawValue;
                    newPdhValue.SecondValue = 0;

                    oldPdhValue.FirstValue = oldSample.RawValue;
                    oldPdhValue.SecondValue = 0;
                    break;
                //
                //  These counters use the 100 Ns time base in thier calculation
                //
                case Interop.Kernel32.PerformanceCounterOptions.PERF_100NSEC_TIMER:
                case Interop.Kernel32.PerformanceCounterOptions.PERF_100NSEC_TIMER_INV:
                case Interop.Kernel32.PerformanceCounterOptions.PERF_100NSEC_MULTI_TIMER:
                case Interop.Kernel32.PerformanceCounterOptions.PERF_100NSEC_MULTI_TIMER_INV:
                    newPdhValue.FirstValue = newSample.RawValue;
                    newPdhValue.SecondValue = newSample.TimeStamp100nSec;

                    oldPdhValue.FirstValue = oldSample.RawValue;
                    oldPdhValue.SecondValue = oldSample.TimeStamp100nSec;
                    if ((newCounterType & Interop.Kernel32.PerformanceCounterOptions.PERF_MULTI_COUNTER) == Interop.Kernel32.PerformanceCounterOptions.PERF_MULTI_COUNTER)
                    {
                        newPdhValue.MultiCount = (int)newSample.BaseValue;
                        oldPdhValue.MultiCount = (int)oldSample.BaseValue;
                    }
                    break;
                //
                //  These counters use two data points
                //
                case Interop.Kernel32.PerformanceCounterOptions.PERF_SAMPLE_FRACTION:
                case Interop.Kernel32.PerformanceCounterOptions.PERF_RAW_FRACTION:
                case Interop.Kernel32.PerformanceCounterOptions.PERF_LARGE_RAW_FRACTION:
                case Interop.Kernel32.PerformanceCounterOptions.PERF_PRECISION_SYSTEM_TIMER:
                case Interop.Kernel32.PerformanceCounterOptions.PERF_PRECISION_100NS_TIMER:
                case Interop.Kernel32.PerformanceCounterOptions.PERF_PRECISION_OBJECT_TIMER:
                case Interop.Kernel32.PerformanceCounterOptions.PERF_AVERAGE_TIMER:
                case Interop.Kernel32.PerformanceCounterOptions.PERF_AVERAGE_BULK:
                    newPdhValue.FirstValue = newSample.RawValue;
                    newPdhValue.SecondValue = newSample.BaseValue;

                    oldPdhValue.FirstValue = oldSample.RawValue;
                    oldPdhValue.SecondValue = oldSample.BaseValue;
                    break;

                default:
                    // an unidentified counter was returned so
                    newPdhValue.FirstValue = 0;
                    newPdhValue.SecondValue = 0;

                    oldPdhValue.FirstValue = 0;
                    oldPdhValue.SecondValue = 0;
                    break;
            }
        }

        private static void LoadPerfCounterDll()
        {
            if (s_perfCounterDllLoaded)
                return;

            string installPath = NetFrameworkUtils.GetLatestBuildDllDirectory(".");

            string perfcounterPath = Path.Combine(installPath, "perfcounter.dll");
            if (Interop.Kernel32.LoadLibrary(perfcounterPath) == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            s_perfCounterDllLoaded = true;
        }
    }
}
