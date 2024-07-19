// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System
{
    public static partial class Environment
    {
        public static unsafe long WorkingSet
        {
            get
            {
                Interop.Process.kinfo_proc* processInfo = Interop.Process.GetProcInfo(ProcessId, true, out _);
                try
                {
                    return processInfo->ki_rssize;
                }
                finally
                {
                    NativeMemory.Free(processInfo);
                }
            }
        }

        /// <summary>
        /// Get the CPU usage, including the process time spent running the application code, the process time spent running the operating system code,
        /// and the total time spent running both the application and operating system code.
        /// </summary>
        [SupportedOSPlatform("maccatalyst")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public static ProcessCpuUsage CpuUsage
        {
            get
            {
                Interop.Process.proc_stats stat = Interop.Process.GetThreadInfo(ProcessId, 0);
                return new ProcessCpuUsage { UserTime = TicksToTimeSpan(stat.userTime), PrivilegedTime = TicksToTimeSpan(stat.systemTime) };
            }
        }
    }
}
