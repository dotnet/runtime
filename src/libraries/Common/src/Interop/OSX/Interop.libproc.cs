// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;


internal static partial class Interop
{
    internal static partial class libproc
    {
        // Constants from sys\param.h
        private const int MAXCOMLEN = 16;
        private const int MAXPATHLEN = 1024;
        
        // Constants from proc_info.h
        private const int MAXTHREADNAMESIZE = 64;
        private const int PROC_PIDTASKALLINFO = 2;
        private const int PROC_PIDTHREADINFO = 5;
        private const int PROC_PIDLISTTHREADS = 6;
        private const int PROC_PIDPATHINFO_MAXSIZE = 4 * MAXPATHLEN;

        // Constants from sys\resource.h
        private const int RUSAGE_INFO_V3 = 3;

        // Defines from proc_info.h
        internal enum ThreadRunState
        {
            TH_STATE_RUNNING            = 1,
            TH_STATE_STOPPED            = 2,
            TH_STATE_WAITING            = 3,
            TH_STATE_UNINTERRUPTIBLE    = 4,
            TH_STATE_HALTED             = 5
        }

        // Defines in proc_info.h
        [Flags]
        internal enum ThreadFlags
        {
            TH_FLAGS_SWAPPED    = 0x1,
            TH_FLAGS_IDLE       = 0x2
        }

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
        internal unsafe struct proc_taskinfo
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

        // from sys\resource.h
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct rusage_info_v3
        {
            internal fixed byte     ri_uuid[16];
            internal ulong          ri_user_time;
            internal ulong          ri_system_time;
            internal ulong          ri_pkg_idle_wkups;
            internal ulong          ri_interrupt_wkups;
            internal ulong          ri_pageins;
            internal ulong          ri_wired_size;
            internal ulong          ri_resident_size;
            internal ulong          ri_phys_footprint;
            internal ulong          ri_proc_start_abstime;
            internal ulong          ri_proc_exit_abstime;
            internal ulong          ri_child_user_time;
            internal ulong          ri_child_system_time;
            internal ulong          ri_child_pkg_idle_wkups;
            internal ulong          ri_child_interrupt_wkups;
            internal ulong          ri_child_pageins;
            internal ulong          ri_child_elapsed_abstime;
            internal ulong          ri_diskio_bytesread;
            internal ulong          ri_diskio_byteswritten;
            internal ulong          ri_cpu_time_qos_default;
            internal ulong          ri_cpu_time_qos_maintenance;
            internal ulong          ri_cpu_time_qos_background;
            internal ulong          ri_cpu_time_qos_utility;
            internal ulong          ri_cpu_time_qos_legacy;
            internal ulong          ri_cpu_time_qos_user_initiated;
            internal ulong          ri_cpu_time_qos_user_interactive;
            internal ulong          ri_billed_system_time;
            internal ulong          ri_serviced_system_time;
        }

        // From proc_info.h
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal unsafe struct proc_taskallinfo
        {
            internal proc_bsdinfo    pbsd;
            internal proc_taskinfo   ptinfo;
        }

        // From proc_info.h
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct proc_threadinfo
        {
            internal ulong      pth_user_time;
            internal ulong      pth_system_time;
            internal int        pth_cpu_usage;
            internal int        pth_policy;
            internal int        pth_run_state;
            internal int        pth_flags;
            internal int        pth_sleep_time;
            internal int        pth_curpri;
            internal int        pth_priority;
            internal int        pth_maxpriority;
            internal fixed byte pth_name[MAXTHREADNAMESIZE];
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct proc_fdinfo
        {
            internal int proc_fd;
            internal uint proc_fdtype;
        }

        /// <summary>
        /// Queries the OS for the PIDs for all running processes
        /// </summary>
        /// <param name="buffer">A pointer to the memory block where the PID array will start</param>
        /// <param name="buffersize">The length of the block of memory allocated for the PID array</param>
        /// <returns>Returns the number of elements (PIDs) in the buffer</returns>
        [DllImport(Interop.Libraries.libproc, SetLastError = true)]
        private static extern unsafe int proc_listallpids(
            int*    pBuffer, 
            int     buffersize);

        /// <summary>
        /// Queries the OS for the list of all running processes and returns the PID for each
        /// </summary>
        /// <returns>Returns a list of PIDs corresponding to all running processes</returns>
        internal static unsafe int[] proc_listallpids()
        {
            // Get the number of processes currently running to know how much data to allocate
            int numProcesses = proc_listallpids(null, 0);
            if (numProcesses <= 0)
            {
                throw new Win32Exception(SR.CantGetAllPids);
            }

            int[] processes;

            do
            {
                // Create a new array for the processes (plus a 10% buffer in case new processes have spawned)
                // Since we don't know how many threads there could be, if result == size, that could mean two things
                // 1) We guessed exactly how many processes there are
                // 2) There are more processes that we didn't get since our buffer is too small
                // To make sure it isn't #2, when the result == size, increase the buffer and try again
                processes = new int[(int)(numProcesses * 1.10)];

                fixed (int* pBuffer = &processes[0])
                {
                    numProcesses = proc_listallpids(pBuffer, processes.Length * sizeof(int));
                    if (numProcesses <= 0)
                    {
                        throw new Win32Exception(SR.CantGetAllPids);
                    }
                }
            }
            while (numProcesses == processes.Length);

            // Remove extra elements
            Array.Resize<int>(ref processes, numProcesses);

            return processes;
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
        /// Gets information about a process given it's PID
        /// </summary>
        /// <param name="pid">The PID of the process</param>
        /// <param name="flavor">Should be PROC_PIDTHREADINFO</param>
        /// <param name="arg">Flavor dependent value</param>
        /// <param name="buffer">A pointer to a block of memory (of size proc_threadinfo) allocated that will contain the data</param>
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
            proc_threadinfo* buffer,
            int bufferSize);

        /// <summary>
        /// Gets information about a process given it's PID
        /// </summary>
        /// <param name="pid">The PID of the process</param>
        /// <param name="flavor">Should be PROC_PIDLISTFDS</param>
        /// <param name="arg">Flavor dependent value</param>
        /// <param name="buffer">A pointer to a block of memory (of size proc_fdinfo) allocated that will contain the data</param>
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
            proc_fdinfo* buffer,
            int bufferSize);

        /// <summary>
        /// Gets information about a process given it's PID
        /// </summary>
        /// <param name="pid">The PID of the process</param>
        /// <param name="flavor">Should be PROC_PIDTASKALLINFO</param>
        /// <param name="arg">Flavor dependent value</param>
        /// <param name="buffer">A pointer to a block of memory (of size ulong[]) allocated that will contain the data</param>
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
            ulong* buffer,
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

        /// <summary>
        /// Gets the thread information for the given thread
        /// </summary>
        /// <param name="thread">The ID of the thread to query for information</param>
        /// <returns>
        /// Returns a valid proc_threadinfo struct for valid threads that the caller
        /// has permissions to access; otherwise, returns null
        /// </returns>
        internal static unsafe proc_threadinfo? GetThreadInfoById(int pid, ulong thread)
        {
            // Negative PIDs are invalid
            if (pid < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pid));
            }

            // Negative TIDs are invalid
            if (thread < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(thread));
            }

            // Get the thread information for the specified thread in the specified process
            int size = sizeof(proc_threadinfo);
            proc_threadinfo info = default(proc_threadinfo);
            int result = proc_pidinfo(pid, PROC_PIDTHREADINFO, (ulong)thread, &info, size);
            return (result == size ? new proc_threadinfo?(info) : null);
        }

        internal static unsafe List<KeyValuePair<ulong, proc_threadinfo?>> GetAllThreadsInProcess(int pid)
        {
            // Negative PIDs are invalid
            if (pid < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pid));
            }

            int result = 0;
            int size = 20; // start assuming 20 threads is enough
            ulong[] threadIds = null;
            var threads = new List<KeyValuePair<ulong, proc_threadinfo?>>();

            // We have no way of knowing how many threads the process has (and therefore how big our buffer should be)
            // so while the return value of the function is the same as our buffer size (meaning it completely filled
            // our buffer), double our buffer size and try again. This ensures that we don't miss any threads
            do
            {
                threadIds = new ulong[size];
                fixed (ulong* pBuffer = &threadIds[0])
                {
                    result = proc_pidinfo(pid, PROC_PIDLISTTHREADS, 0, pBuffer, sizeof(ulong) * threadIds.Length);
                }

                if (result <= 0)
                {
                    // If we were unable to access the information, just return the empty list.  
                    // This is likely to happen for privileged processes, if the process went away
                    // by the time we tried to query it, etc.
                    return threads;
                }
                else
                {
                    checked
                    {
                        size *= 2;
                    }
                }
            }
            while (result == sizeof(ulong) * threadIds.Length);

            Debug.Assert((result % sizeof(ulong)) == 0);

            // Loop over each thread and get the thread info
            int count = (int)(result / sizeof(ulong));
            threads.Capacity = count;
            for (int i = 0; i < count; i++)
            {        
                threads.Add(new KeyValuePair<ulong, proc_threadinfo?>(threadIds[i], GetThreadInfoById(pid, threadIds[i])));
            }

            return threads;
        }

        /// <summary>
        /// Gets the full path to the executable file identified by the specified PID
        /// </summary>
        /// <param name="pid">The PID of the running process</param>
        /// <param name="buffer">A pointer to an allocated block of memory that will be filled with the process path</param>
        /// <param name="bufferSize">The size of the buffer, should be PROC_PIDPATHINFO_MAXSIZE</param>
        /// <returns>Returns the length of the path returned on success</returns>
        [DllImport(Interop.Libraries.libproc, SetLastError = true)]
        private static extern unsafe int proc_pidpath(
            int pid, 
            byte* buffer, 
            uint bufferSize);

        /// <summary>
        /// Gets the full path to the executable file identified by the specified PID
        /// </summary>
        /// <param name="pid">The PID of the running process</param>
        /// <returns>Returns the full path to the process executable</returns>
        internal static unsafe string proc_pidpath(int pid)
        {
            // Negative PIDs are invalid
            if (pid < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pid), SR.NegativePidNotSupported);
            }

            // The path is a fixed buffer size, so use that and trim it after
            int result = 0;
            byte* pBuffer = stackalloc byte[PROC_PIDPATHINFO_MAXSIZE];
            result = proc_pidpath(pid, pBuffer, (uint)(PROC_PIDPATHINFO_MAXSIZE * sizeof(byte)));
            if (result <= 0)
            {
                throw new Win32Exception();
            }

            // OS X uses UTF-8. The conversion may not strip off all trailing \0s so remove them here
            return System.Text.Encoding.UTF8.GetString(pBuffer, result);
        }

        /// <summary>
        /// Gets the rusage information for the process identified by the PID
        /// </summary>
        /// <param name="pid">The process to retrieve the rusage for</param>
        /// <param name="flavor">Specifies the type of struct that is passed in to <paramref>buffer</paramref>. Should be RUSAGE_INFO_V3 to specify a rusage_info_v3 struct.</param>
        /// <param name="buffer">A buffer to be filled with rusage_info data</param>
        /// <returns>Returns 0 on success; on fail, -1 and errno is set with the error code</returns>
        [DllImport(Interop.Libraries.libproc, SetLastError = true)]
        private static extern unsafe int proc_pid_rusage(
            int pid,
            int flavor,
            rusage_info_v3* buffer);

        /// <summary>
        /// Gets the rusage information for the process identified by the PID
        /// </summary>
        /// <param name="pid">The process to retrieve the rusage for</param>
        /// <returns>On success, returns a struct containing info about the process; on
        /// failure or when the caller doesn't have permissions to the process, throws a Win32Exception
        /// </returns>
        internal static unsafe rusage_info_v3 proc_pid_rusage(int pid)
        {
            // Negative PIDs are invalid
            if (pid < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pid), SR.NegativePidNotSupported);
            }

            rusage_info_v3 info = new rusage_info_v3();

            // Get the PIDs rusage info
            int result = proc_pid_rusage(pid, RUSAGE_INFO_V3, &info);
            if (result < 0)
            {
                throw new InvalidOperationException(SR.RUsageFailure);
            }

            return info;
        }
    }
}
