// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

internal static partial class Interop
{
    internal static partial class Process
    {
        internal struct proc_stats
        {
            internal long startTime;        /* time_t */
            internal int nice;
            internal ulong userTime;        /* in ticks */
            internal ulong systemTime;      /* in ticks */
        }

        internal static int[] ListAllPids() =>
            OperatingSystem.IsFreeBSD() ? FreeBSDProcess.ListAllPids() : OpenBSDProcess.ListAllPids();

        internal static string GetProcPath(int pid) =>
            OperatingSystem.IsFreeBSD() ? FreeBSDProcess.GetProcPath(pid) : OpenBSDProcess.GetProcPath(pid);

        internal static int GetParentPid(int pid) =>
            OperatingSystem.IsFreeBSD() ? FreeBSDProcess.GetParentPid(pid) : OpenBSDProcess.GetParentPid(pid);

        internal static ProcessInfo? GetProcessInfoById(int pid) =>
            OperatingSystem.IsFreeBSD() ? FreeBSDProcess.GetProcessInfoById(pid) : OpenBSDProcess.GetProcessInfoById(pid);

        internal static proc_stats GetThreadInfo(int pid, int tid) =>
            OperatingSystem.IsFreeBSD() ? FreeBSDProcess.GetThreadInfo(pid, tid) : OpenBSDProcess.GetThreadInfo(pid, tid);
    }
}
