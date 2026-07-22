// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class @libproc
    {
        // Constants from sys/resource.h
        private const int RUSAGE_INFO_V4 = 4;

        // From sys/resource.h - verify field order/types against the local SDK header
        // ($(xcrun --show-sdk-path)/usr/include/sys/resource.h) before relying on this;
        // LibraryImport does not validate this layout against the native struct.
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct rusage_info_v4
        {
            internal fixed byte ri_uuid[16];
            internal ulong ri_user_time;
            internal ulong ri_system_time;
            internal ulong ri_pkg_idle_wkups;
            internal ulong ri_interrupt_wkups;
            internal ulong ri_pageins;
            internal ulong ri_wired_size;
            internal ulong ri_resident_size;
            internal ulong ri_phys_footprint;
            internal ulong ri_proc_start_abstime;
            internal ulong ri_proc_exit_abstime;
            internal ulong ri_child_user_time;
            internal ulong ri_child_system_time;
            internal ulong ri_child_pkg_idle_wkups;
            internal ulong ri_child_interrupt_wkups;
            internal ulong ri_child_pageins;
            internal ulong ri_child_elapsed_abstime;
            internal ulong ri_diskio_bytesread;
            internal ulong ri_diskio_byteswritten;
            internal ulong ri_cpu_time_qos_default;
            internal ulong ri_cpu_time_qos_maintenance;
            internal ulong ri_cpu_time_qos_background;
            internal ulong ri_cpu_time_qos_utility;
            internal ulong ri_cpu_time_qos_legacy;
            internal ulong ri_cpu_time_qos_user_initiated;
            internal ulong ri_cpu_time_qos_user_interactive;
            internal ulong ri_billed_system_time;
            internal ulong ri_serviced_system_time;
            internal ulong ri_logical_writes;
            internal ulong ri_lifetime_max_phys_footprint;
            internal ulong ri_instructions;
            internal ulong ri_cycles;
            internal ulong ri_billed_energy;
            internal ulong ri_serviced_energy;
            internal ulong ri_interval_max_phys_footprint;
            internal ulong ri_runnable_time;
        }

        /// <summary>
        /// Gets extended resource usage information about a process given its PID
        /// </summary>
        /// <param name="pid">The PID of the process</param>
        /// <param name="flavor">Should be RUSAGE_INFO_V4</param>
        /// <param name="buffer">A pointer to a block of memory (of size rusage_info_v4) that will contain the data</param>
        /// <returns>
        /// 0 on success. A negative value indicates an error (e.g. lack of permission to
        /// query the specified process); check errno/SetLastError in that case.
        /// </returns>
        [LibraryImport(Interop.Libraries.libproc, SetLastError = true)]
        private static unsafe partial int proc_pid_rusage(
            int pid,
            int flavor,
            rusage_info_v4* buffer);

        /// <summary>
        /// Gets the physical memory footprint (private/unique memory usage) for a given process
        /// </summary>
        /// <param name="pid">The PID (process ID) of the process</param>
        /// <returns>
        /// The process's physical footprint in bytes for valid processes the caller has permission
        /// to access; otherwise, null (e.g. root-owned processes without elevated privileges)
        /// </returns>
        internal static unsafe ulong? GetProcessPhysicalFootprint(int pid)
        {
            // Negative PIDs are invalid
            ArgumentOutOfRangeException.ThrowIfNegative(pid);

            rusage_info_v4 info = default;
            int result = proc_pid_rusage(pid, RUSAGE_INFO_V4, &info);
            return (result == 0 ? new ulong?(info.ri_phys_footprint) : null);
        }
    }
}
