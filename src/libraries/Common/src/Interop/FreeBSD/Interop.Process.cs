// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

#pragma warning disable CA1823 // analyzer incorrectly flags fixed buffer length const (https://github.com/dotnet/roslyn/issues/37593)

internal static partial class Interop
{
    internal static partial class Process
    {
        private const ulong SecondsToNanoseconds = 1000000000;
        private const ulong MicroSecondsToNanoSeconds = 1000;

        // Constants from sys/sysctl.h
        private const int KERN_PROC_PATHNAME = 12;

        internal struct proc_stats
        {
            internal long startTime;        /* time_t */
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
                    if (list[i].ki_ppid == 0)
                    {
                        // skip kernel threads
                        numProcesses -= 1;
                    }
                    else
                    {
                        pids[idx] = list[i].ki_pid;
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
            Span<int> sysctlName = stackalloc int[] { CTL_KERN, KERN_PROC, KERN_PROC_PATHNAME, pid };
            byte* pBuffer = null;
            int bytesLength = 0;

            try
            {
                Interop.Sys.Sysctl(sysctlName, ref pBuffer, ref bytesLength);
                return System.Text.Encoding.UTF8.GetString(pBuffer, bytesLength - 1);
            }
            finally
            {
                NativeMemory.Free(pBuffer);
            }
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
            if (pid < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pid));
            }

            ProcessInfo info;

            kinfo_proc* kinfo = GetProcInfo(pid, true, out int count);
            try
            {
                if (count < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(pid));
                }

                var process = new ReadOnlySpan<kinfo_proc>(kinfo, count);

                // Get the process information for the specified pid
                info = new ProcessInfo();

                info.ProcessName = Marshal.PtrToStringAnsi((IntPtr)kinfo->ki_comm)!;
                info.BasePriority = kinfo->ki_nice;
                info.VirtualBytes = (long)kinfo->ki_size;
                info.WorkingSet = kinfo->ki_rssize;
                info.SessionId = kinfo->ki_sid;

                for (int i = 0; i < process.Length; i++)
                {
                    var ti = new ThreadInfo()
                    {
                        _processId = pid,
                        _threadId = (ulong)process[i].ki_tid,
                        _basePriority = process[i].ki_nice,
                        _startAddress = (IntPtr)process[i].ki_tdaddr
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
        /// Returns basic info about thread. If tis is 0, it will return
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
                        ret.startTime = (int)info->ki_start.tv_sec;
                        ret.nice = info->ki_nice;
                        ret.userTime = (ulong)info->ki_rusage.ru_utime.tv_sec * SecondsToNanoseconds + (ulong)info->ki_rusage.ru_utime.tv_usec * MicroSecondsToNanoSeconds;
                        ret.systemTime = (ulong)info->ki_rusage.ru_stime.tv_sec * SecondsToNanoseconds + (ulong)info->ki_rusage.ru_stime.tv_usec * MicroSecondsToNanoSeconds;
                    }
                    else
                    {
                        var list = new ReadOnlySpan<kinfo_proc>(info, count);
                        for (int i = 0; i < list.Length; i++)
                        {
                            if (list[i].ki_tid == tid)
                            {
                                ret.startTime = (int)list[i].ki_start.tv_sec;
                                ret.nice = list[i].ki_nice;
                                ret.userTime = (ulong)list[i].ki_rusage.ru_utime.tv_sec * SecondsToNanoseconds + (ulong)list[i].ki_rusage.ru_utime.tv_usec * MicroSecondsToNanoSeconds;
                                ret.systemTime = (ulong)list[i].ki_rusage.ru_stime.tv_sec * SecondsToNanoseconds + (ulong)list[i].ki_rusage.ru_stime.tv_usec * MicroSecondsToNanoSeconds;
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
