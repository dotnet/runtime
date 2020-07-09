// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.Tracing
{
    internal static class RuntimeEventSourceHelper
    {
        private static long s_prevProcUserTime;
        private static long s_prevProcKernelTime;
        private static long s_prevSystemUserTime;
        private static long s_prevSystemKernelTime;

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

            if (s_prevSystemUserTime == 0 && s_prevSystemKernelTime == 0) // These may be 0 when we report CPU usage for the first time, in which case we should just return 0.
            {
                cpuUsage = 0;
            }
            else
            {
                long totalProcTime = (procUserTime - s_prevProcUserTime) + (procKernelTime - s_prevProcKernelTime);
                long totalSystemTime = (systemUserTime - s_prevSystemUserTime) + (systemKernelTime - s_prevSystemKernelTime);
                cpuUsage = (int)(totalProcTime * 100 / totalSystemTime);
            }

            s_prevProcUserTime = procUserTime;
            s_prevProcKernelTime = procKernelTime;
            s_prevSystemUserTime = systemUserTime;
            s_prevSystemKernelTime = systemKernelTime;

            return cpuUsage;
        }
    }
}
