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
            // OpenBSD has no KERN_PROC_PATHNAME. The closest available information is the
            // process argv, whose first entry is the path the process was executed with.
            ReadOnlySpan<int> sysctlName = [CTL_KERN, KERN_PROC_ARGS, pid, KERN_PROC_ARGV];

            byte* pBuffer = null;
            uint bytesLength = 0;
            try
            {
                Interop.Sys.Sysctl(sysctlName, ref pBuffer, ref bytesLength);

                if (pBuffer == null || bytesLength < (uint)sizeof(byte*))
                {
                    return string.Empty;
                }

                // The kernel relocates the argv pointer array to point within the returned buffer.
                byte* argv0 = ((byte**)pBuffer)[0];
                return argv0 is null ? string.Empty : Utf8StringMarshaller.ConvertToManaged(argv0) ?? string.Empty;
            }
            finally
            {
                NativeMemory.Free(pBuffer);
            }
        }

        /// <summary>
        /// Attempts to recover a process name that was truncated in kinfo_proc.p_comm by reading
        /// the full name from the process argv.
        /// </summary>
        /// <param name="pid">The PID of the process.</param>
        /// <param name="prefix">The (possibly truncated) p_comm value used to validate the recovered name.</param>
        /// <returns>The full process name, or null if it could not be recovered.</returns>
        private static unsafe string? GetUntruncatedProcessName(int pid, string prefix)
        {
            ReadOnlySpan<int> sysctlName = [CTL_KERN, KERN_PROC_ARGS, pid, KERN_PROC_ARGV];

            byte* pBuffer = null;
            uint bytesLength = 0;
            try
            {
                Interop.Sys.Sysctl(sysctlName, ref pBuffer, ref bytesLength);

                if (pBuffer == null || bytesLength < (uint)sizeof(byte*))
                {
                    return null;
                }

                // The kernel relocates the argv pointer array to point within the returned buffer.
                // For native executables the name is argv[0]; for scripts argv[0] is the interpreter
                // and argv[1] is the script, so check the first two NULL-terminated arguments.
                byte** argv = (byte**)pBuffer;
                for (int i = 0; i < 2 && argv[i] is not null; i++)
                {
                    string arg = Utf8StringMarshaller.ConvertToManaged(argv[i]) ?? string.Empty;

                    // Strip directory names.
                    int nameStart = arg.LastIndexOf('/') + 1;
                    string name = nameStart == 0 ? arg : arg.Substring(nameStart);

                    if (name.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        return name;
                    }
                }

                return null;
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
        public static unsafe ProcessInfo? GetProcessInfoById(int pid)
        {
            // Negative PIDs are invalid
            ArgumentOutOfRangeException.ThrowIfNegative(pid);

            ProcessInfo info;

            kinfo_proc* kinfo = GetProcInfo(pid, true, out int count);
            try
            {
                // The process may have exited between the time its PID was enumerated and now,
                // in which case no entries are returned. Report it as not found rather than failing.
                if (count < 1)
                {
                    return null;
                }

                var process = new ReadOnlySpan<kinfo_proc>(kinfo, count);

                // Get the process information for the specified pid
                info = new ProcessInfo();

                info.ProcessName = Utf8StringMarshaller.ConvertToManaged(kinfo->p_comm)!;

                // p_comm is limited to KI_MAXCOMLEN - 1 characters. When the name is at that
                // limit it may be truncated, so try to recover the full name from the process argv.
                if (info.ProcessName.Length >= KI_MAXCOMLEN - 1)
                {
                    info.ProcessName = GetUntruncatedProcessName(pid, info.ProcessName) ?? info.ProcessName;
                }

                info.BasePriority = kinfo->p_nice;

                // OpenBSD's KERN_PROC sysctl always reports p_vm_map_size as 0, so derive the
                // virtual size from the text, data, and stack segment sizes instead.
                long pageSize = Environment.SystemPageSize;
                info.VirtualBytes = ((long)kinfo->p_vm_tsize + kinfo->p_vm_dsize + kinfo->p_vm_ssize) * pageSize;
                // OpenBSD does not track a separate peak virtual size; report the current size.
                info.VirtualBytesPeak = info.VirtualBytes;
                info.WorkingSet = kinfo->p_vm_rssize;
                // p_uru_maxrss is the peak resident set size, reported in kilobytes.
                info.WorkingSetPeak = (long)kinfo->p_uru_maxrss * 1024;
                // OpenBSD does not expose a private byte count; approximate it with the
                // anonymous (data + stack) segment sizes.
                info.PrivateBytes = ((long)kinfo->p_vm_dsize + kinfo->p_vm_ssize) * pageSize;
                info.SessionId = kinfo->p_sid;

                for (int i = 0; i < process.Length; i++)
                {
                    // KERN_PROC_SHOW_THREADS returns a process-summary entry with p_tid == -1
                    // ahead of the real per-thread entries. Skip it so only actual threads are reported.
                    if (process[i].p_tid < 0)
                    {
                        continue;
                    }

                    var ti = new ThreadInfo()
                    {
                        _processId = pid,
                        _threadId = (ulong)process[i].p_tid,
                        _basePriority = process[i].p_nice,
                        _startAddress = null // OpenBSD's kinfo_proc does not expose a thread start address.
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
                        ret.systemTime = (ulong)info->p_ustime_sec * SecondsToNanoseconds + (ulong)info->p_ustime_usec * MicroSecondsToNanoSeconds;
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
                                ret.systemTime = (ulong)list[i].p_ustime_sec * SecondsToNanoseconds + (ulong)list[i].p_ustime_usec * MicroSecondsToNanoSeconds;
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
