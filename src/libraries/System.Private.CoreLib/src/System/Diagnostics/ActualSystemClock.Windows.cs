// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Diagnostics
{
    public partial class ActualSystemClock
    {
        private static unsafe ulong GetTicks()
        {
            ulong fileTime;
            s_pfnGetSystemTimeAsFileTime(&fileTime);

            if (!DateTime.s_systemSupportsLeapSeconds)
            {
                return fileTime + DateTime.FileTimeOffset;
            }

            Interop.Kernel32.SYSTEMTIME time;
            ulong hundredNanoSecond;

            if (Interop.Kernel32.FileTimeToSystemTime(&fileTime, &time) != Interop.BOOL.FALSE)
            {
                // to keep the time precision
                ulong tmp = fileTime; // temp. variable avoids double read from memory
                hundredNanoSecond = tmp % TimeSpan.TicksPerMillisecond;
            }
            else
            {
                Interop.Kernel32.GetSystemTime(&time);
                hundredNanoSecond = 0;
            }

            return DateTime.GetTicksFromSystemTime(in time, hundredNanoSecond);
        }

        private static readonly unsafe delegate* unmanaged[SuppressGCTransition]<ulong*, void> s_pfnGetSystemTimeAsFileTime = GetGetSystemTimeAsFileTimeFnPtr();

        private static unsafe delegate* unmanaged[SuppressGCTransition]<ulong*, void> GetGetSystemTimeAsFileTimeFnPtr()
        {
            IntPtr kernel32Lib = Interop.Kernel32.LoadLibraryEx(Interop.Libraries.Kernel32, IntPtr.Zero, Interop.Kernel32.LOAD_LIBRARY_SEARCH_SYSTEM32);
            Debug.Assert(kernel32Lib != IntPtr.Zero);

            IntPtr pfnGetSystemTime = NativeLibrary.GetExport(kernel32Lib, "GetSystemTimeAsFileTime");

            if (NativeLibrary.TryGetExport(kernel32Lib, "GetSystemTimePreciseAsFileTime", out IntPtr pfnGetSystemTimePrecise))
            {
                // GetSystemTimePreciseAsFileTime exists and we'd like to use it.  However, on
                // misconfigured systems, it's possible for the "precise" time to be inaccurate:
                //     https://github.com/dotnet/runtime/issues/9014
                // If it's inaccurate, though, we expect it to be wildly inaccurate, so as a
                // workaround/heuristic, we get both the "normal" and "precise" times, and as
                // long as they're close, we use the precise one. This workaround can be removed
                // when we better understand what's causing the drift and the issue is no longer
                // a problem or can be better worked around on all targeted OSes.

                // Retry this check several times to reduce chance of false negatives due to thread being rescheduled
                // at wrong time.
                for (int i = 0; i < 10; i++)
                {
                    long systemTimeResult, preciseSystemTimeResult;
                    ((delegate* unmanaged[SuppressGCTransition]<long*, void>)pfnGetSystemTime)(&systemTimeResult);
                    ((delegate* unmanaged[SuppressGCTransition]<long*, void>)pfnGetSystemTimePrecise)(&preciseSystemTimeResult);

                    if (Math.Abs(preciseSystemTimeResult - systemTimeResult) <= 100 * TimeSpan.TicksPerMillisecond)
                    {
                        pfnGetSystemTime = pfnGetSystemTimePrecise; // use the precise version
                        break;
                    }
                }
            }

            return (delegate* unmanaged[SuppressGCTransition]<ulong*, void>)pfnGetSystemTime;
        }
    }
}
