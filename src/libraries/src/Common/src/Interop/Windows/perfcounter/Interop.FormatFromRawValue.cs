// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class PerfCounters
    {
#pragma warning disable BCL0015 // Invalid Pinvoke call
        [DllImport(Libraries.PerfCounter, CharSet=System.Runtime.InteropServices.CharSet.Auto)]
        public static unsafe extern int FormatFromRawValue(
          uint dwCounterType,
          uint dwFormat,
          ref long pTimeBase,
          Interop.Kernel32.PerformanceCounterOptions.PDH_RAW_COUNTER pRawValue1,
          Interop.Kernel32.PerformanceCounterOptions.PDH_RAW_COUNTER pRawValue2,
          Interop.Kernel32.PerformanceCounterOptions.PDH_FMT_COUNTERVALUE pFmtValue
        );
#pragma warning restore BCL0015
    }
}
