// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

#pragma warning disable CA1823 // analyzer incorrectly flags fixed buffer length const (https://github.com/dotnet/roslyn/issues/37593)

internal static partial class Interop
{
    internal static partial class OS
    {
        internal const int B_OS_NAME_LENGTH = 32;
        internal const int B_FILE_NAME_LENGTH = 256;

        [LibraryImportAttribute(Interop.Libraries.libroot, SetLastError = false)]
        private static unsafe partial int _get_next_area_info(int team, ref nint cookie, out area_info areaInfo, nuint size);

        [LibraryImportAttribute(Interop.Libraries.libroot, SetLastError = false)]
        private static unsafe partial int _get_team_info(int id, out team_info info, nuint size);

        [LibraryImportAttribute(Interop.Libraries.libroot, SetLastError = false)]
        private static unsafe partial int _get_next_team_info(ref int cookie, void* info, nuint size);

        [LibraryImportAttribute(Interop.Libraries.libroot, SetLastError = false)]
        private static unsafe partial int _get_team_usage_info(int team, BTeamUsage who, out team_usage_info info, nuint size);

        [LibraryImportAttribute(Interop.Libraries.libroot, SetLastError = false)]
        private static unsafe partial int _get_thread_info(int id, out thread_info info, nuint size);

        [LibraryImportAttribute(Interop.Libraries.libroot, SetLastError = false)]
        private static unsafe partial int _get_next_thread_info(int team, ref int cookie, out thread_info info, nuint size);

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct area_info
        {
            public int area;
            public fixed byte name[B_OS_NAME_LENGTH];
            public nuint size;
            public uint @lock;
            public uint protection;
            public int team;
            public uint ram_size;
            public uint copy_count;
            public uint in_count;
            public uint out_count;
            public void* address;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct team_info
        {
            public int team;
            public int thread_count;
            public int image_count;
            public int area_count;
            public int debugger_nub_thread;
            public int debugger_nub_port;
            public int argc;
            public fixed byte args[64];
            public uint uid;
            public uint gid;
            public uint real_uid;
            public uint real_gid;
            public int group_id;
            public int session_id;
            public int parent;
            public fixed byte name[B_OS_NAME_LENGTH];
            public long start_time;
        }

        internal enum BTeamUsage : int
        {
            B_TEAM_USAGE_SELF = 0,
            B_TEAM_USAGE_CHILDREN = -1,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct team_usage_info
        {
            public long user_time;
            public long kernel_time;
        }

        internal enum thread_state : int
        {
            B_THREAD_RUNNING = 1,
            B_THREAD_READY,
            B_THREAD_RECEIVING,
            B_THREAD_ASLEEP,
            B_THREAD_SUSPENDED,
            B_THREAD_WAITING,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct thread_info
        {
            public int thread;
            public int team;
            public fixed byte name[B_OS_NAME_LENGTH];
            public thread_state state;
            public int priority;
            public int sem;
            public long user_time;
            public long kernel_time;
            public void* stack_base;
            public void* stack_end;
        }

        internal enum BPriority : int
        {
            B_IDLE_PRIORITY = 0,
            B_LOWEST_ACTIVE_PRIORITY = 1,
            B_LOW_PRIORITY = 5,
            B_NORMAL_PRIORITY = 10,
            B_DISPLAY_PRIORITY = 15,
            B_URGENT_DISPLAY_PRIORITY = 20,
            B_REAL_TIME_DISPLAY_PRIORITY = 100,
            B_URGENT_PRIORITY = 110,
            B_REAL_TIME_PRIORITY = 120,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct system_info
        {
            public long boot_time;
            public uint cpu_count;
            public ulong max_pages;
            public ulong used_pages;
            public ulong cached_pages;
            public ulong block_cache_pages;
            public ulong ignored_pages;
            public ulong needed_memory;
            public ulong free_memory;
            public ulong max_swap_pages;
            public ulong free_swap_pages;
            public uint page_faults;
            public uint max_sems;
            public uint used_sems;
            public uint max_ports;
            public uint used_ports;
            public uint max_threads;
            public uint used_threads;
            public uint max_teams;
            public uint used_teams;
            public fixed byte kernel_name[B_FILE_NAME_LENGTH];
            public fixed byte kernel_build_date[B_OS_NAME_LENGTH];
            public fixed byte kernel_build_time[B_OS_NAME_LENGTH];
            public long kernel_version;
            public uint abi;
        }

        /// <summary>
        /// Gets information about areas owned by a team.
        /// </summary>
        /// <param name="team">The team ID of the areas to iterate.</param>
        /// <param name="cookie">A cookie to track the iteration.</param>
        /// <param name="info">The <see cref="area_info"/> structure to fill in.</param>
        /// <returns>Returns 0 on success. Returns an error code on failure or when there are no more areas to iterate.</returns>
        internal static unsafe int GetNextAreaInfo(int team, ref nint cookie, out area_info info)
        {
            return _get_next_area_info(team, ref cookie, out info, (nuint)sizeof(area_info));
        }

        /// <summary>
        /// Gets information about a team.
        /// </summary>
        /// <param name="team">The team ID.</param>
        /// <param name="info">The <see cref="team_info"/> structure to fill in.</param>
        /// <returns>Returns 0 on success or an error code on failure.</returns>
        internal static unsafe int GetTeamInfo(int team, out team_info info)
        {
            return _get_team_info(team, out info, (nuint)sizeof(team_info));
        }

        /// <summary>
        /// Gets information about teams.
        /// </summary>
        /// <param name="cookie">A cookie to track the iteration.</param>
        /// <param name="info">The <see cref="team_info"/> structure to fill in.</param>
        /// <returns>Returns 0 on success. Returns an error code on failure or when there are no more teams to iterate.</returns>
        internal static unsafe int GetNextTeamInfo(ref int cookie, out team_info info)
        {
            fixed (team_info* p = &info)
            {
                return _get_next_team_info(ref cookie, p, (nuint)sizeof(team_info));
            }
        }

        /// <summary>
        /// Gets team IDs.
        /// </summary>
        /// <param name="cookie">A cookie to track the iteration.</param>
        /// <param name="team">The integer to store the retrieved team ID.</param>
        /// <returns>Returns 0 on success. Returns an error code on failure or when there are no more teams to iterate.</returns>
        internal static unsafe int GetNextTeamId(ref int cookie, out int team)
        {
            fixed (int* p = &team)
            {
                return _get_next_team_info(ref cookie, p, (nuint)sizeof(int));
            }
        }

        /// <summary>
        /// Gets information about a team's usage.
        /// </summary>
        /// <param name="team">The team ID.</param>
        /// <param name="who">Specifies whether to get usage information for the team or its children.</param>
        /// <param name="info">The <see cref="team_usage_info"/> structure to fill in.</param>
        /// <returns>Returns 0 on success or an error code on failure.</returns>
        internal static unsafe int GetTeamUsageInfo(int team, BTeamUsage who, out team_usage_info info)
        {
            return _get_team_usage_info(team, who, out info, (nuint)sizeof(team_usage_info));
        }

        /// <summary>
        /// Sets the priority of a thread.
        /// </summary>
        /// <param name="thread">The thread ID.</param>
        /// <param name="newPriority">The new priority.</param>
        /// <returns>The previous priority if successful or an error code on failure.</returns>
        [LibraryImportAttribute(Interop.Libraries.libroot, EntryPoint = "set_thread_priority", SetLastError = false)]
        internal static unsafe partial int SetThreadPriority(int thread, int newPriority);

        /// <summary>
        /// Gets information about a thread.
        /// </summary>
        /// <param name="thread">The thread ID.</param>
        /// <param name="info">The <see cref="thread_info"/> structure to fill in.</param>
        /// <returns>Returns 0 on success or an error code on failure.</returns>
        internal static unsafe int GetThreadInfo(int thread, out thread_info info)
        {
            return _get_thread_info(thread, out info, (nuint)sizeof(thread_info));
        }

        /// <summary>
        /// Gets information about threads owned by a team.
        /// </summary>
        /// <param name="team">The team ID of the threads to iterate.</param>
        /// <param name="cookie">A cookie to track the iteration.</param>
        /// <param name="info">The <see cref="thread_info"/> structure to fill in.</param>
        /// <returns>Returns 0 on success. Returns an error code on failure or when there are no more threads to iterate.</returns>
        internal static unsafe int GetNextThreadInfo(int team, ref int cookie, out thread_info info)
        {
            return _get_next_thread_info(team, ref cookie, out info, (nuint)sizeof(thread_info));
        }

        /// <summary>
        /// Gets information about the system.
        /// </summary>
        /// <param name="info">The system_info to store retrieved information.</param>
        /// <returns>0 if successful.</returns>
        [LibraryImportAttribute(Interop.Libraries.libroot, EntryPoint = "get_system_info", SetLastError = false)]
        internal static unsafe partial int GetSystemInfo(out system_info info);
    }
}
