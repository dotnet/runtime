// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Pdh
    {
        [DllImport(Libraries.Pdh, CharSet = CharSet.Unicode)]
        public static extern int PdhFormatFromRawValue(
            uint dwCounterType,
            uint dwFormat,
            ref long pTimeBase,
            ref Interop.Kernel32.PerformanceCounterOptions.PDH_RAW_COUNTER pRawValue1,
            ref Interop.Kernel32.PerformanceCounterOptions.PDH_RAW_COUNTER pRawValue2,
            ref Interop.Kernel32.PerformanceCounterOptions.PDH_FMT_COUNTERVALUE pFmtValue
        );
    }
}
