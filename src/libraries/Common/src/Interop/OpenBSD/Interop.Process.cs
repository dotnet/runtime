// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

#pragma warning disable CA1823 // analyzer incorrectly flags fixed buffer length const (https://github.com/dotnet/roslyn/issues/37593)

internal static partial class Interop
{
    internal static partial class Process
    {
        private const ulong SecondsToNanoseconds = 1000000000;
        private const ulong MicroSecondsToNanoSeconds = 1000;

        internal struct proc_stats
        {
            internal long startTime;
            internal int nice;
            internal ulong userTime;        /* in ticks */
            internal ulong systemTime;      /* in ticks */
        }

        /// <summary>
        /// Queries the OS for the list of all running processes and returns the PID for each
        /// </summary>
        /// <returns>Returns a list of PIDs corresponding to all running processes</returns>
        internal static unsafe int[] ListAllPids()
        {
            kinfo_proc* entries = GetProcInfo(0, false, out int numProcesses);
            try
            {
                if (numProcesses <= 0)
                {
                    throw new Win32Exception(SR.CantGetAllPids);
                }

                var list = new ReadOnlySpan<kinfo_proc>(entries, numProcesses);
                var pids = new int[numProcesses];

                // walk through process list and skip kernel threads
                int idx = 0;
                for (int i = 0; i < list.Length; i++)
                {
                    if (list[i].p_ppid == 0)
                    {
                        // skip kernel threads
                        numProcesses -= 1;
                    }
                    else
                    {
                        pids[idx] = list[i].p_pid;
                        idx += 1;
                    }
                }

                // Remove extra elements
                Array.Resize<int>(ref pids, numProcesses);
                return pids;
            }
            finally
            {
                NativeMemory.Free(entries);
            }
        }

        /// <summary>
        /// Gets executable name for process given it's PID
        /// </summary>
        /// <param name="pid">The PID of the process</param>
        public static unsafe string GetProcPath(int pid)
        {
            // TODO
        }

        /// <summary>
        /// Gets the process information for a given process
        /// </summary>
        /// <param name="pid">The PID (process ID) of the process</param>
        /// <returns>
        /// Returns a valid ProcessInfo struct for valid processes that the caller
        /// has permission to access; otherwise, returns null
        /// </returns>
        public static unsafe ProcessInfo GetProcessInfoById(int pid)
        {
            // Negative PIDs are invalid
            ArgumentOutOfRangeException.ThrowIfNegative(pid);

            ProcessInfo info;

            kinfo_proc* kinfo = GetProcInfo(pid, true, out int count);
            try
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(count, 1, nameof(pid));

                var process = new ReadOnlySpan<kinfo_proc>(kinfo, count);

                // Get the process information for the specified pid
                info = new ProcessInfo();

                info.ProcessName = Utf8StringMarshaller.ConvertToManaged(kinfo->p_comm)!;
                info.BasePriority = kinfo->p_nice;
                info.VirtualBytes = (long)kinfo->p_vm_map_size;
                info.WorkingSet = kinfo->p_vm_rssize;
                info.SessionId = kinfo->p_sid;

                for (int i = 0; i < process.Length; i++)
                {
                    var ti = new ThreadInfo()
                    {
                        _processId = pid,
                        _threadId = (ulong)process[i].p_tid,
                        _basePriority = process[i].p_nice,
                        _startAddress = // TODO: this doesn't exist on OpenBSD
                    };
                    info._threadInfoList.Add(ti);
                }
            }
            finally
            {
                NativeMemory.Free(kinfo);
            }

            return info;
        }

        /// <summary>
        /// Gets the process information for a given process
        /// </summary>
        /// <param name="pid">The PID (process ID) of the process</param>
        /// <param name="tid">The TID (thread ID) of the process</param>
        /// <returns>
        /// Returns basic info about thread. If tid is 0, it will return
        /// info for process e.g. main thread.
        /// </returns>
        public static unsafe proc_stats GetThreadInfo(int pid, int tid)
        {
            proc_stats ret = default;
            int count;

            kinfo_proc* info = GetProcInfo(pid, (tid != 0), out count);
            try
            {
                if (info != null && count >= 1)
                {
                    if (tid == 0)
                    {
                        ret.startTime = (int)info->p_ustart_sec;
                        ret.nice = info->p_nice;
                        ret.userTime = (ulong)info->p_uutime_sec * SecondsToNanoseconds + (ulong)info->p_uutime_usec * MicroSecondsToNanoSeconds;
                        ret.systemTime = (ulong)info->p_uutime_sec * SecondsToNanoseconds + (ulong)info->p_uutime_usec * MicroSecondsToNanoSeconds;
                    }
                    else
                    {
                        var list = new ReadOnlySpan<kinfo_proc>(info, count);
                        for (int i = 0; i < list.Length; i++)
                        {
                            if (list[i].p_tid == tid)
                            {
                                ret.startTime = (int)list[i].p_ustart_sec;
                                ret.nice = list[i].p_nice;
                                ret.userTime = (ulong)list[i].p_uutime_sec * SecondsToNanoseconds + (ulong)list[i].p_uutime_usec * MicroSecondsToNanoSeconds;
                                ret.systemTime = (ulong)list[i].p_uutime_sec * SecondsToNanoseconds + (ulong)list[i].p_uutime_usec * MicroSecondsToNanoSeconds;
                                break;
                            }
                        }
                    }
                }
            }
            finally
            {
                NativeMemory.Free(info);
            }

            return ret;
        }
    }
}
