// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System;
using System.Text;
using System.Runtime;
using System.Security;
using System.Threading;

namespace System.Diagnostics
{
    internal partial class SafeNativeMethods
    {
        // file src\Services\Monitoring\system\Diagnosticts\SafeNativeMethods.cs
        [DllImport(ExternDll.Kernel32, CharSet=System.Runtime.InteropServices.CharSet.Unicode, SetLastError=true)]
        public static extern IntPtr LoadLibrary(string libFilename);

        [DllImport(ExternDll.Kernel32, CharSet=System.Runtime.InteropServices.CharSet.Unicode, SetLastError=true)]
        public static extern bool FreeLibrary(HandleRef hModule);
        
        [DllImport(ExternDll.Kernel32, CharSet=CharSet.Auto, BestFitMapping=false)]
        public static extern bool GetComputerName(StringBuilder lpBuffer, int[] nSize);                                           
        
        public static unsafe int InterlockedCompareExchange(IntPtr pDestination, int exchange, int compare)
        {
            return Interlocked.CompareExchange(ref *(int *)pDestination.ToPointer(), exchange, compare);
        }

#pragma warning disable BCL0015 // Invalid Pinvoke call
        [DllImport(ExternDll.PerfCounter, CharSet=System.Runtime.InteropServices.CharSet.Auto)]
        public static unsafe extern int FormatFromRawValue(
          uint dwCounterType,
          uint dwFormat,
          ref long pTimeBase,
          Interop.Kernel32.PerformanceCounterOptions.PDH_RAW_COUNTER pRawValue1,
          Interop.Kernel32.PerformanceCounterOptions.PDH_RAW_COUNTER pRawValue2,
          Interop.Kernel32.PerformanceCounterOptions.PDH_FMT_COUNTERVALUE pFmtValue
        );
    }
}
