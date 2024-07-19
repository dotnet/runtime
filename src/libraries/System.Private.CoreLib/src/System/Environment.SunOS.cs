// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System
{
    public static partial class Environment
    {
        public static long WorkingSet =>
            (long)(Interop.procfs.TryReadProcessStatusInfo(Interop.procfs.ProcPid.Self, out Interop.procfs.ProcessStatusInfo status) ? status.ResidentSetSize : 0);

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
                Interop.procfs.ParsedStat stat = GetStat();
                return new ProcessCpuUsage { UserTime = TicksToTimeSpan(GetStat().Utime), PrivilegedTime = TicksToTimeSpan(GetStat().Stime) };
            }
        }

        private static Interop.procfs.ParsedStat GetStat()
        {
            Interop.procfs.ParsedStat stat;
            Interop.procfs.TryReadStatFile(Interop.procfs.ProcPid.Self, out stat);
            return stat;
        }
    }
}
