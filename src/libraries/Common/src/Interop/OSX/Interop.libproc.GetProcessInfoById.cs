// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

#pragma warning disable CA1823 // analyzer incorrectly flags fixed buffer length const (https://github.com/dotnet/roslyn/issues/37593)

internal static partial class Interop
{
    internal static partial class libproc
    {
        // Constants from sys\param.h
        private const int MAXCOMLEN = 16;

        // Constants from proc_info.h
        private const int PROC_PIDTASKALLINFO = 2;

        // From proc_info.h
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct proc_bsdinfo
        {
            internal uint       pbi_flags;
            internal uint       pbi_status;
            internal uint       pbi_xstatus;
            internal uint       pbi_pid;
            internal uint       pbi_ppid;
            internal uint       pbi_uid;
            internal uint       pbi_gid;
            internal uint       pbi_ruid;
            internal uint       pbi_rgid;
            internal uint       pbi_svuid;
            internal uint       pbi_svgid;
            internal uint       reserved;
            internal fixed byte pbi_comm[MAXCOMLEN];
            internal fixed byte pbi_name[MAXCOMLEN * 2];
            internal uint       pbi_nfiles;
            internal uint       pbi_pgid;
            internal uint       pbi_pjobc;
            internal uint       e_tdev;
            internal uint       e_tpgid;
            internal int        pbi_nice;
            internal ulong      pbi_start_tvsec;
            internal ulong      pbi_start_tvusec;
        }

        // From proc_info.h
        [StructLayout(LayoutKind.Sequential)]
        internal struct proc_taskinfo
        {
            internal ulong   pti_virtual_size;
            internal ulong   pti_resident_size;
            internal ulong   pti_total_user;
            internal ulong   pti_total_system;
            internal ulong   pti_threads_user;
            internal ulong   pti_threads_system;
            internal int     pti_policy;
            internal int     pti_faults;
            internal int     pti_pageins;
            internal int     pti_cow_faults;
            internal int     pti_messages_sent;
            internal int     pti_messages_received;
            internal int     pti_syscalls_mach;
            internal int     pti_syscalls_unix;
            internal int     pti_csw;
            internal int     pti_threadnum;
            internal int     pti_numrunning;
            internal int     pti_priority;
        };

        // From proc_info.h
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct proc_taskallinfo
        {
            internal proc_bsdinfo    pbsd;
            internal proc_taskinfo   ptinfo;
        }

        /// <summary>
        /// Gets information about a process given it's PID
        /// </summary>
        /// <param name="pid">The PID of the process</param>
        /// <param name="flavor">Should be PROC_PIDTASKALLINFO</param>
        /// <param name="arg">Flavor dependent value</param>
        /// <param name="buffer">A pointer to a block of memory (of size proc_taskallinfo) allocated that will contain the data</param>
        /// <param name="bufferSize">The size of the allocated block above</param>
        /// <returns>
        /// The amount of data actually returned. If this size matches the bufferSize parameter then
        /// the data is valid. If the sizes do not match then the data is invalid, most likely due
        /// to not having enough permissions to query for the data of that specific process
        /// </returns>
        [DllImport(Interop.Libraries.libproc, SetLastError = true)]
        private static extern unsafe int proc_pidinfo(
            int pid,
            int flavor,
            ulong arg,
            proc_taskallinfo* buffer,
            int bufferSize);

        /// <summary>
        /// Gets the process information for a given process
        /// </summary>
        /// <param name="pid">The PID (process ID) of the process</param>
        /// <returns>
        /// Returns a valid proc_taskallinfo struct for valid processes that the caller
        /// has permission to access; otherwise, returns null
        /// </returns>
        internal static unsafe proc_taskallinfo? GetProcessInfoById(int pid)
        {
            // Negative PIDs are invalid
            if (pid < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pid));
            }

            // Get the process information for the specified pid
            int size = sizeof(proc_taskallinfo);
            proc_taskallinfo info = default(proc_taskallinfo);
            int result = proc_pidinfo(pid, PROC_PIDTASKALLINFO, 0, &info, size);
            return (result == size ? new proc_taskallinfo?(info) : null);
        }
    }
}
