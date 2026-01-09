// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Versioning;
using System.Threading;

namespace System.Diagnostics
{
    public partial class Process
    {
        /// <summary>
        /// Creates an array of <see cref="Process"/> components that are associated with process resources on a
        /// remote computer. These process resources share the specified process name.
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public static Process[] GetProcessesByName(string? processName, string machineName)
        {
            ProcessManager.ThrowIfRemoteMachine(machineName);

            int[] procIds = ProcessManager.GetProcessIds();
            var processes = new ArrayBuilder<Process>(string.IsNullOrEmpty(processName) ? procIds.Length : 0);

            // Iterate through all process IDs to load information about each process
            foreach (int pid in procIds)
            {
                ProcessInfo? processInfo = ProcessManager.CreateProcessInfo(pid, processName);
                if (processInfo != null)
                {
                    processes.Add(new Process(machineName, isRemoteMachine: false, pid, processInfo));
                }
            }

            return processes.ToArray();
        }

        // bigtime_t represents microseconds while DateTime.Ticks is in 100ns units
        private const int BigTimeToTicks = 10;

        private static long s_bootTimeTicks;
        /// <summary>Gets the system boot time.</summary>
        private static DateTime BootTime
        {
            get
            {
                long bootTimeTicks = Interlocked.Read(ref s_bootTimeTicks);
                if (bootTimeTicks == 0)
                {
                    Interop.OS.system_info info;
                    int status = Interop.OS.GetSystemInfo(out info);

                    if (status != 0)
                    {
                        throw new Win32Exception(status);
                    }

                    bootTimeTicks = info.boot_time * BigTimeToTicks;
                    long oldValue = Interlocked.CompareExchange(ref s_bootTimeTicks, bootTimeTicks, 0);
                    if (oldValue != 0) // a different thread has managed to update the ticks first
                    {
                        bootTimeTicks = oldValue; // consistency
                    }
                }
                return DateTime.UnixEpoch.AddTicks(bootTimeTicks);
            }
        }

        /// <summary>Gets the time the associated process was started.</summary>
        internal DateTime StartTimeCore
        {
            get
            {
                EnsureState(State.HaveNonExitedId);

                Interop.OS.team_info info;
                int status = Interop.OS.GetTeamInfo(_processId, out info);

                if (status != 0)
                {
                    throw new Win32Exception(status);
                }

                return BootTime.AddTicks(info.start_time * BigTimeToTicks);
            }
        }

        /// <summary>
        /// Gets the amount of time the associated process has spent utilizing the CPU.
        /// It is the sum of the <see cref='System.Diagnostics.Process.UserProcessorTime'/> and
        /// <see cref='System.Diagnostics.Process.PrivilegedProcessorTime'/>.
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan TotalProcessorTime
        {
            get
            {
                EnsureState(State.HaveNonExitedId);

                Interop.OS.team_usage_info info;
                int status = Interop.OS.GetTeamUsageInfo(_processId, Interop.OS.BTeamUsage.B_TEAM_USAGE_SELF, out info);

                if (status != 0)
                {
                    throw new Win32Exception(status);
                }

                return TimeSpan.FromMicroseconds(info.user_time + info.kernel_time);
            }
        }

        /// <summary>
        /// Gets the amount of time the associated process has spent running code
        /// inside the application portion of the process (not the operating system core).
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan UserProcessorTime
        {
            get
            {
                EnsureState(State.HaveNonExitedId);

                Interop.OS.team_usage_info info;
                int status = Interop.OS.GetTeamUsageInfo(_processId, Interop.OS.BTeamUsage.B_TEAM_USAGE_SELF, out info);

                if (status != 0)
                {
                    throw new Win32Exception(status);
                }

                return TimeSpan.FromMicroseconds(info.user_time);
            }
        }

        /// <summary>Gets the amount of time the process has spent running code inside the operating system core.</summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan PrivilegedProcessorTime
        {
            get
            {
                EnsureState(State.HaveNonExitedId);

                Interop.OS.team_usage_info info;
                int status = Interop.OS.GetTeamUsageInfo(_processId, Interop.OS.BTeamUsage.B_TEAM_USAGE_SELF, out info);

                if (status != 0)
                {
                    throw new Win32Exception(status);
                }

                return TimeSpan.FromMicroseconds(info.kernel_time);
            }
        }

        /// <summary>Gets parent process ID</summary>
        private unsafe int ParentProcessId
        {
            get
            {
                EnsureState(State.HaveNonExitedId);

                Interop.OS.team_info info;
                int status = Interop.OS.GetTeamInfo(_processId, out info);

                if (status != 0)
                {
                    throw new Win32Exception(status);
                }

                return info.parent;
            }
        }

        /// <summary>
        /// Gets or sets which processors the threads in this process can be scheduled to run on.
        /// </summary>
        private static IntPtr ProcessorAffinityCore
        {
            get { throw new PlatformNotSupportedException(); }
            set { throw new PlatformNotSupportedException(); }
        }

#pragma warning disable IDE0060
        /// <summary>
        /// Make sure we have obtained the min and max working set limits.
        /// </summary>
        private static void GetWorkingSetLimits(out IntPtr minWorkingSet, out IntPtr maxWorkingSet)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>Sets one or both of the minimum and maximum working set limits.</summary>
        /// <param name="newMin">The new minimum working set limit, or null not to change it.</param>
        /// <param name="newMax">The new maximum working set limit, or null not to change it.</param>
        /// <param name="resultingMin">The resulting minimum working set limit after any changes applied.</param>
        /// <param name="resultingMax">The resulting maximum working set limit after any changes applied.</param>
        private static void SetWorkingSetLimitsCore(IntPtr? newMin, IntPtr? newMax, out IntPtr resultingMin, out IntPtr resultingMax)
        {
            throw new PlatformNotSupportedException();
        }
#pragma warning restore IDE0060

        /// <summary>Gets execution path</summary>
        private static string? GetPathToOpenFile()
        {
            if (Interop.Sys.Stat("/boot/system/bin/open", out _) == 0)
            {
                return "/boot/system/bin/open";
            }
            else
            {
                return null;
            }
        }
    }
}
