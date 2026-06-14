// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

#pragma warning disable CA1823 // analyzer incorrectly flags fixed buffer length const (https://github.com/dotnet/roslyn/issues/37593)
internal static partial class Interop
{
    internal static partial class OS
    {
        internal const int B_OS_NAME_LENGTH = 32;

        [StructLayout(LayoutKind.Sequential)]
        internal struct AreaInfo
        {
            public nuint size;
            public uint ram_size;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct TeamInfo
        {
            public int team;
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
        internal struct TeamUsageInfo
        {
            public long user_time;
            public long kernel_time;
        }

        internal enum ThreadState : int
        {
            B_THREAD_RUNNING = 1,
            B_THREAD_READY,
            B_THREAD_RECEIVING,
            B_THREAD_ASLEEP,
            B_THREAD_SUSPENDED,
            B_THREAD_WAITING,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ThreadInfo
        {
            public int thread;
            public int team;
            public ThreadState state;
            public int priority;
            public long user_time;
            public long kernel_time;
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
        internal struct SystemInfo
        {
            public long boot_time;
        }

        /// <summary>
        /// Gets information about areas owned by a team.
        /// </summary>
        /// <param name="team">The team ID of the areas to iterate.</param>
        /// <param name="cookie">A cookie to track the iteration.</param>
        /// <param name="info">The <see cref="AreaInfo"/> structure to fill in.</param>
        /// <returns>Returns 0 on success. Returns an error code on failure or when there are no more areas to iterate.</returns>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetNextAreaInfo")]
        internal static partial int GetNextAreaInfo(int team, ref nint cookie, out AreaInfo info);

        /// <summary>
        /// Gets information about a team.
        /// </summary>
        /// <param name="team">The team ID.</param>
        /// <param name="info">The <see cref="TeamInfo"/> structure to fill in.</param>
        /// <returns>Returns 0 on success or an error code on failure.</returns>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetTeamInfo")]
        internal static partial int GetTeamInfo(int team, out TeamInfo info);

        /// <summary>
        /// Gets information about teams.
        /// </summary>
        /// <param name="cookie">A cookie to track the iteration.</param>
        /// <param name="info">The <see cref="TeamInfo"/> structure to fill in.</param>
        /// <returns>Returns 0 on success. Returns an error code on failure or when there are no more teams to iterate.</returns>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetNextTeamInfo")]
        internal static partial int GetNextTeamInfo(ref int cookie, out TeamInfo info);

        /// <summary>
        /// Gets team IDs.
        /// </summary>
        /// <param name="cookie">A cookie to track the iteration.</param>
        /// <param name="team">The integer to store the retrieved team ID.</param>
        /// <returns>Returns 0 on success. Returns an error code on failure or when there are no more teams to iterate.</returns>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetNextTeamId")]
        internal static partial int GetNextTeamId(ref int cookie, out int team);

        /// <summary>
        /// Gets information about a team's usage.
        /// </summary>
        /// <param name="team">The team ID.</param>
        /// <param name="who">Specifies whether to get usage information for the team or its children.</param>
        /// <param name="info">The <see cref="TeamUsageInfo"/> structure to fill in.</param>
        /// <returns>Returns 0 on success or an error code on failure.</returns>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetTeamUsageInfo")]
        internal static partial int GetTeamUsageInfo(int team, BTeamUsage who, out TeamUsageInfo info);

        /// <summary>
        /// Sets the priority of a thread.
        /// </summary>
        /// <param name="thread">The thread ID.</param>
        /// <param name="newPriority">The new priority.</param>
        /// <returns>The previous priority if successful or an error code on failure.</returns>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetThreadPriority")]
        internal static partial int SetThreadPriority(int thread, int newPriority);

        /// <summary>
        /// Gets information about a thread.
        /// </summary>
        /// <param name="thread">The thread ID.</param>
        /// <param name="info">The <see cref="ThreadInfo"/> structure to fill in.</param>
        /// <returns>Returns 0 on success or an error code on failure.</returns>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetThreadInfo")]
        internal static partial int GetThreadInfo(int thread, out ThreadInfo info);

        /// <summary>
        /// Gets information about threads owned by a team.
        /// </summary>
        /// <param name="team">The team ID of the threads to iterate.</param>
        /// <param name="cookie">A cookie to track the iteration.</param>
        /// <param name="info">The <see cref="ThreadInfo"/> structure to fill in.</param>
        /// <returns>Returns 0 on success. Returns an error code on failure or when there are no more threads to iterate.</returns>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetNextThreadInfo")]
        internal static partial int GetNextThreadInfo(int team, ref int cookie, out ThreadInfo info);

        /// <summary>
        /// Gets information about the system.
        /// </summary>
        /// <param name="info">The system info to store retrieved information.</param>
        /// <returns>0 if successful.</returns>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetSystemInfo")]
        internal static partial int GetSystemInfo(out SystemInfo info);
    }
}
