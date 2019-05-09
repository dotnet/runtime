// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics.Tracing
{
    internal sealed class RuntimeEventSourceHelper
    {
        private static long prevProcUserTime = 0;
        private static long prevProcKernelTime = 0;
        private static long prevSystemUserTime = 0;
        private static long prevSystemKernelTime = 0;

        internal static int GetCpuUsage()
        {
            // Returns the current process' CPU usage as a percentage

            int cpuUsage;

            if (!Interop.Kernel32.GetProcessTimes(Interop.Kernel32.GetCurrentProcess(), out _, out _, out long procKernelTime, out long procUserTime))
            {
                return 0;
            }

            if (!Interop.Kernel32.GetSystemTimes(out _, out long systemUserTime, out long systemKernelTime))
            {
                return 0;
            }

            if (prevSystemUserTime == 0 && prevSystemKernelTime == 0) // These may be 0 when we report CPU usage for the first time, in which case we should just return 0. 
            {
                cpuUsage = 0;
            }
            else
            {
                long totalProcTime = (procUserTime - prevProcUserTime) + (procKernelTime - prevProcKernelTime);
                long totalSystemTime = (systemUserTime - prevSystemUserTime) + (systemKernelTime - prevSystemKernelTime);
                cpuUsage = (int)(totalProcTime * 100 / totalSystemTime);
            }

            prevProcUserTime = procUserTime;
            prevProcKernelTime = procKernelTime;
            prevSystemUserTime = systemUserTime;
            prevSystemKernelTime = systemKernelTime;

            return cpuUsage;
        }
    }
}
