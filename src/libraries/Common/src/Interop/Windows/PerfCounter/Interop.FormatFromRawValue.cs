// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class PerfCounter
    {
        [DllImport(Libraries.PerfCounter, CharSet = CharSet.Unicode)]
        public static extern unsafe int FormatFromRawValue(
            uint dwCounterType,
            uint dwFormat,
            ref long pTimeBase,
            ref Interop.Kernel32.PerformanceCounterOptions.PDH_RAW_COUNTER pRawValue1,
            ref Interop.Kernel32.PerformanceCounterOptions.PDH_RAW_COUNTER pRawValue2,
            ref Interop.Kernel32.PerformanceCounterOptions.PDH_FMT_COUNTERVALUE pFmtValue
        );
    }
}
