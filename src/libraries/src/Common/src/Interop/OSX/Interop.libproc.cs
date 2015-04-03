// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
        private const int PROC_PIDLISTFDS = 1;
        private const int PROC_PIDTASKALLINFO = 2;
        private const int PROC_PIDTHREADINFO = 5;
        private const int PROC_PIDLISTTHREADS = 6;
        private const int PROC_PIDPATHINFO_MAXSIZE = 4 * MAXPATHLEN;
        private static int PROC_PIDLISTFD_SIZE = Marshal.SizeOf<proc_fdinfo>();
        private static int PROC_PIDLISTTHREADS_SIZE = (Marshal.SizeOf<uint>() * 2);
        
        // Constants from sys\resource.h
        private const int RUSAGE_SELF = 0;

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

        /// <summary>
        /// Converts an OS-level thread state to a .NET thread state.
        /// </summary>
        /// <param name="state">The OS state to convert</param>
        /// <returns>The .NET mapping of the OS thread state</returns>
        internal static System.Diagnostics.ThreadState ConvertOsxThreadRunStateToThreadState(ThreadRunState state)
        {
            switch (state)
            {
                case ThreadRunState.TH_STATE_RUNNING:
                    return System.Diagnostics.ThreadState.Running;
                case ThreadRunState.TH_STATE_STOPPED:
                    return System.Diagnostics.ThreadState.Terminated;
                case ThreadRunState.TH_STATE_HALTED:
                    return System.Diagnostics.ThreadState.Wait;
                case ThreadRunState.TH_STATE_UNINTERRUPTIBLE:
                    return System.Diagnostics.ThreadState.Running;
                case ThreadRunState.TH_STATE_WAITING:
                    return System.Diagnostics.ThreadState.Standby;
                default:
                    throw new ArgumentOutOfRangeException("state");
            }
        }

        /// <summary>
        /// Convert an OS-level thread flag to a .NET ThreadWaitReason
        /// </summary>
        /// <param name="flags">The OS flags to convert</param>
        /// <returns>
        /// Returns a single thread wait reasons based on flag priority.
        /// </returns>
        internal static System.Diagnostics.ThreadWaitReason ConvertOsxThreadFlagsToWaitReason(ThreadFlags flags)
        {
            // Since ThreadWaitReason isn't a flag, we have to do a mapping and will lose some information.
            // The priorities below are arbitrary
            if ((flags & ThreadFlags.TH_FLAGS_SWAPPED) == ThreadFlags.TH_FLAGS_SWAPPED)
                return System.Diagnostics.ThreadWaitReason.PageOut;
            else
                return System.Diagnostics.ThreadWaitReason.Unknown; // There isn't a good mapping for anything else
        }

        // From proc_info.h
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct proc_bsdinfo
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
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAXCOMLEN)]
            internal string     pbi_comm;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAXCOMLEN * 2)]
            internal string     pbi_name;
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

        // from sys\resource.h
        [StructLayout(LayoutKind.Sequential)]
        internal struct rusage_info_v3
        {
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]
            internal ushort[]    ri_uuid;
            internal ulong       ri_user_time;
            internal ulong       ri_system_time;
            internal ulong       ri_pkg_idle_wkups;
            internal ulong       ri_interrupt_wkups;
            internal ulong       ri_pageins;
            internal ulong       ri_wired_size;
            internal ulong       ri_resident_size;
            internal ulong       ri_phys_footprint;
            internal ulong       ri_proc_start_abstime;
            internal ulong       ri_proc_exit_abstime;
            internal ulong       ri_child_user_time;
            internal ulong       ri_child_system_time;
            internal ulong       ri_child_pkg_idle_wkups;
            internal ulong       ri_child_interrupt_wkups;
            internal ulong       ri_child_pageins;
            internal ulong       ri_child_elapsed_abstime;
            internal ulong       ri_diskio_bytesread;
            internal ulong       ri_diskio_byteswritten;
            internal ulong       ri_cpu_time_qos_default;
            internal ulong       ri_cpu_time_qos_maintenance;
            internal ulong       ri_cpu_time_qos_background;
            internal ulong       ri_cpu_time_qos_utility;
            internal ulong       ri_cpu_time_qos_legacy;
            internal ulong       ri_cpu_time_qos_user_initiated;
            internal ulong       ri_cpu_time_qos_user_interactive;
            internal ulong       ri_billed_system_time;
            internal ulong       ri_serviced_system_time;
        }

        // From proc_info.h
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct proc_taskallinfo
        {
            internal proc_bsdinfo    pbsd;
            internal proc_taskinfo   ptinfo;
        }

        // From proc_info.h
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct proc_threadinfo
        {
            internal ulong pth_user_time;
            internal ulong pth_system_time;
            internal int pth_cpu_usage;
            internal int pth_policy;
            internal int pth_run_state;
            internal int pth_flags;
            internal int pth_sleep_time;
            internal int pth_curpri;
            internal int pth_priority;
            internal int pth_maxpriority;
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
        private static extern int proc_listallpids(
            IntPtr buffer, 
            int buffersize);

        /// <summary>
        /// Queries the OS for the list of all running processes and returns the PID for each
        /// </summary>
        /// <returns>Returns a list of PIDs corresponding to all running processes</returns>
        internal static int[] proc_listallpids()
        {
            // Get the number of processes currently running to know how much data to allocate
            int numProcesses = proc_listallpids(IntPtr.Zero, 0);
            if (numProcesses <= 0)
            {
                throw new System.Runtime.InteropServices.COMException(SR.CantGetAllPids, Marshal.GetLastWin32Error());
            }

            // Allocate the correct amount of memory, plus 1/4 more just in case more processes have spawned between
            // when we asked and now (very possible), and then get the PID list
            int bufferSize = Convert.ToInt32(numProcesses * Marshal.SizeOf<uint>() * (numProcesses * .25));
            IntPtr unmanagedArray = Marshal.AllocCoTaskMem(bufferSize);
            numProcesses = proc_listallpids(unmanagedArray, bufferSize);
            if (numProcesses <= 0)
            {
                throw new System.Runtime.InteropServices.COMException(SR.CantGetAllPids, Marshal.GetLastWin32Error());
            }

            // Copy the pids from unmanaged to managed memory and then free unmanaged memory
            int[] pids = new int[numProcesses];
            Marshal.Copy(unmanagedArray, pids, 0, numProcesses);
            Marshal.FreeCoTaskMem(unmanagedArray);

            return pids;
        }

        /// <summary>
        /// Gets information about a process given it's PID
        /// </summary>
        /// <param name="pid">The PID of the process</param>
        /// <param name="flavor">Should be one of the PROC_PID* constants</param>
        /// <param name="arg">Should be 0</param>
        /// <param name="buffer">A pointer to a block of memory (of size proc_taskallinfo) allocated that will contain the data</param>
        /// <param name="bufferSize">The size of the allocated block above</param>
        /// <returns>
        /// The amount of data actually returned. If this size matches the bufferSize parameter then
        /// the data is valid. If the sizes do not match then the data is invalid, most likely due 
        /// to not having enough permissions to query for the data of that specific process
        /// </returns>
        [DllImport(Interop.Libraries.libproc, SetLastError = true)]
        private static extern int proc_pidinfo(
            int pid,
            int flavor,
            ulong arg,
            IntPtr buffer,
            int bufferSize);

        /// <summary>
        /// Gets the process information for a given process
        /// </summary>
        /// <param name="pid">The PID (process ID) of the process</param>
        /// <returns>
        /// Returns a valid proc_taskallinfo struct for valid processes that the caller 
        /// has permission to access; otherwise, returns null
        /// </returns>
        internal static proc_taskallinfo? GetProcessInfoById(int pid)
        {
            proc_taskallinfo info;
            bool success = internal_proc_pidinfo_helper<proc_taskallinfo>(pid, PROC_PIDTASKALLINFO, 0, out info);
            return (success ? new proc_taskallinfo?(info) : null);
        }

        /// <summary>
        /// Gets the thread information for the given thread
        /// </summary>
        /// <param name="thread">The ID of the thread to query for information</param>
        /// <returns>
        /// Returns a valid proc_threadinfo struct for valid threads that the caller
        /// has permissions to access; otherwise, returns null
        /// </returns>
        internal static proc_threadinfo? GetThreadInfoById(ulong thread)
        {
            proc_threadinfo info;
            bool success = internal_proc_pidinfo_helper<proc_threadinfo>(Interop.libc.getpid(), PROC_PIDTHREADINFO, (ulong)thread, out info);
            return (success ? new proc_threadinfo?(info) : null);
        }

        internal static List<KeyValuePair<ulong, proc_threadinfo?>> GetAllThreadsInProcess(int pid)
        {
            // Allocate unmanaged memory for the thread IDs
            int size = PROC_PIDLISTTHREADS_SIZE * 20; // start assuming 20 threads is enough
            IntPtr buffer = Marshal.AllocCoTaskMem(size);

            // Try to get the thread IDs
            int result = proc_pidinfo(pid, PROC_PIDLISTTHREADS, 0,  buffer, size);

            // Since we don't know how many threads there could be, if result == size, that could mean two things
            // 1) We guessed exactly how many threads there are
            // 2) There are more threads that we didn't get since our buffer is too small
            // To make sure it isn't #2, when we result == size, double the buffer and try again
            while (result == size)
            {
                Marshal.FreeCoTaskMem(buffer);
                size *= 2;
                Marshal.AllocCoTaskMem(size);
                result = proc_pidinfo(pid, PROC_PIDLISTTHREADS, 0, buffer, size);
            }

            // Any data means result > 0
            if (result <= 0)
            {
                throw new System.ComponentModel.Win32Exception();
            }

            List<KeyValuePair<ulong, proc_threadinfo?>> threads = new List<KeyValuePair<ulong, proc_threadinfo?>>();

            // Loop over each thread and get the info
            for (int i = 0; i < (result / PROC_PIDLISTTHREADS_SIZE); i++)
            {
                ulong threadId = (ulong)Marshal.ReadInt64(buffer, i * Marshal.SizeOf<ulong>());
                threads.Add(new KeyValuePair<ulong, proc_threadinfo?>(threadId, GetThreadInfoById(threadId)));
            }

            // Free our unmanaged memory
            Marshal.FreeCoTaskMem(buffer);

            return threads;
        }

        /// <summary>
        /// Retrieves the number of open file descriptors for the specified pid
        /// </summary>
        /// <returns>Returns a list of open File Descriptors for this process</returns>
        /// <remarks>
        /// This function doesn't use the helper since it seems to allow passing NULL
        /// values in to the buffer and length parameters to get back an estimation 
        /// of how much data we will need to allocate; the other flavors don't seem
        /// to support doing that.
        /// </remarks>
        internal static List<proc_fdinfo> GetFileDescriptorsForPid(int pid)
        {
            // Negative PIDs are invalid
            if (pid < 0)
            {
                throw new ArgumentOutOfRangeException("pid");
            }

            int size = 0;
            IntPtr buffer = IntPtr.Zero;
            
            // Query for an estimation about the size of the buffer we will need. This seems
            // to add some padding from the real number, so we don't need to do that
            int result = proc_pidinfo(pid, PROC_PIDLISTFDS, 0, buffer, size);
            if (result <= 0)
            {
                throw new System.ComponentModel.Win32Exception();
            }

            // Allocate our unmanaged memory and call again to get the descriptors
            size = result;
            buffer = Marshal.AllocCoTaskMem(size);
            result = proc_pidinfo(pid, PROC_PIDLISTFDS, 0, buffer, size);
            if (result <= 0)
            {
                throw new System.ComponentModel.Win32Exception();
            }

            List<proc_fdinfo> fds = new List<proc_fdinfo>();

            // Copy the file descriptors over
            for (int i = 0; i < (result / PROC_PIDLISTFD_SIZE); i++)
            {
                fds.Add(Marshal.PtrToStructure<proc_fdinfo>(buffer + (i * PROC_PIDLISTFD_SIZE)));
            }

            // Free our unmanaged memory
            Marshal.FreeCoTaskMem(buffer);

            return fds;
        }

        private static bool internal_proc_pidinfo_helper<T>(int pid, int flavor, ulong args, out T info)
        {
            // Negative PIDs are invalid
            if (pid < 0)
            {
                throw new ArgumentOutOfRangeException("pid", SR.NegativePidNotSupported);
            }

            bool success = false;

            // Allocate our unmanaged buffer for the task info
            info = default(T);
            int size = Marshal.SizeOf<T>();
            IntPtr buffer = Marshal.AllocCoTaskMem(size);

            // Get the process information for the given PID
            int result = proc_pidinfo(pid, flavor, args, buffer, size);
            if (result == size)
            {
                // Copy the struct from unmanaged memory to managed memory
                info = Marshal.PtrToStructure<T>(buffer);
                success = true;
            }

            // Free our unmanaged buffer
            Marshal.FreeCoTaskMem(buffer);

            return success;
        }

        /// <summary>
        /// Gets the full path to the executable file identified by the specified PID
        /// </summary>
        /// <param name="pid">The PID of the running process</param>
        /// <param name="buffer">A pointer to an allocated block of memory that will be filled with the process path</param>
        /// <param name="bufferSize">The size of the buffer, should be PROC_PIDPATHINFO_MAXSIZE</param>
        /// <returns>Returns the length of the path returned on success</returns>
        [DllImport(Interop.Libraries.libproc, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern int proc_pidpath(
            int pid, 
            IntPtr buffer, 
            uint bufferSize);

        /// <summary>
        /// Gets the full path to the executable file identified by the specified PID
        /// </summary>
        /// <param name="pid">The PID of the running process</param>
        /// <returns>Returns the full path to the process executable</returns>
        internal static string proc_pidpath(int pid)
        {
            // Negative PIDs are invalid
            if (pid < 0)
            {
                throw new ArgumentOutOfRangeException("pid", SR.NegativePidNotSupported);
            }

            // Allocate our unmanaged buffer for the path
            string path = string.Empty;
            uint size = (uint)Marshal.SizeOf<char>() * PROC_PIDPATHINFO_MAXSIZE + 1;
            IntPtr buffer = Marshal.AllocCoTaskMem((int)size);

            // Get the PID's executable's path
            int result = proc_pidpath(pid, buffer, size);
            if (result < 0)
            {
                // Check for less than 0 since the kernel process won't have a path
                throw new COMException(string.Format(SR.CantFindProcessExecutablePath, pid), Marshal.GetLastWin32Error());
            }

            // Copy unmanaged data into a managed string and free the unmanaged buffer
            path = Marshal.PtrToStringUni(buffer, result);
            Marshal.FreeCoTaskMem(buffer);

            return path;
        }

        /// <summary>
        /// Gets the rusage information for the process identified by the PID
        /// </summary>
        /// <param name="pid">The process to retrieve the rusage for</param>
        /// <param name="flavor">Should be RUSAGE_SELF to specify getting the info for the specified process</param>
        /// <param name="rusage_info_t">A buffer to be filled with rusage_info data</param>
        /// <returns>Returns 0 on success; on fail, -1 and errno is set with the error code</returns>
        /// <remarks>
        /// We need to use IntPtr here for the buffer since the function signature uses 
        /// void* and not a strong type even though it returns a rusage_info struct
        /// </remarks>
        [DllImport(Interop.Libraries.libproc, SetLastError = true)]
        private static extern int proc_pid_rusage(
            int pid,
            int flavor,
            IntPtr rusage_info_t);

        /// <summary>
        /// Gets the rusage information for the process identified by the PID
        /// </summary>
        /// <param name="pid">The process to retrieve the rusage for</param>
        /// <returns>On success, returns a struct containing info about the process; on
        /// failure or when the caller doesn't have permissions to the process, throws a COMException
        /// </returns>
        internal static rusage_info_v3 proc_pid_rusage(int pid)
        {
            // Negative PIDs are invalid
            if (pid < 0)
            {
                throw new ArgumentOutOfRangeException("pid", SR.NegativePidNotSupported);
            }

            // Allocate our managed buffer for the data
            rusage_info_v3 info;
            int size = Marshal.SizeOf<rusage_info_v3>();
            IntPtr buffer = Marshal.AllocCoTaskMem(size);

            // Get the PIDs rusage info
            int result = proc_pid_rusage(pid, RUSAGE_SELF, buffer);
            if (result < 0)
            {
                throw new COMException(SR.RUsageFailure, Marshal.GetLastWin32Error());
            }

            // Copy the unmanaged data into the managed struct and delete the unmanaged buffer
            info = Marshal.PtrToStructure<rusage_info_v3>(buffer);
            Marshal.FreeCoTaskMem(buffer);

            return info;
        }
    }
}
