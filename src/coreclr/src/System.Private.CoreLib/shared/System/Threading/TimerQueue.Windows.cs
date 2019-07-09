// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Threading
{
    internal partial class TimerQueue
    {
        private static long TickCount64
        {
            get
            {
                // We need to keep our notion of time synchronized with the calls to SleepEx that drive
                // the underlying native timer.  In Win8, SleepEx does not count the time the machine spends
                // sleeping/hibernating.  Environment.TickCount (GetTickCount) *does* count that time,
                // so we will get out of sync with SleepEx if we use that method.
                //
                // So, on Win8, we use QueryUnbiasedInterruptTime instead; this does not count time spent
                // in sleep/hibernate mode.
                if (Environment.IsWindows8OrAbove)
                {
                    if (!Interop.Kernel32.QueryUnbiasedInterruptTime(out ulong time100ns))
                        Marshal.ThrowExceptionForHR(Marshal.GetLastWin32Error());

                    return (long)(time100ns / 10_000); // convert from 100ns to milliseconds
                }
                else
                {
                    return Environment.TickCount64;
                }
            }
        }
    }
}
