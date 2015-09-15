// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Diagnostics
{
    public partial class Process : IDisposable
    {
        /// <summary>Gets the amount of time the process has spent running code inside the operating system core.</summary>
        public TimeSpan PrivilegedProcessorTime
        {
            get
            {
                return TicksToTimeSpan(GetStat().stime);
            }
        }

        /// <summary>Gets the time the associated process was started.</summary>
        public DateTime StartTime
        {
            get
            {
                return BootTimeToDateTime(GetStat().starttime);
            }
        }

        /// <summary>
        /// Gets the amount of time the associated process has spent utilizing the CPU.
        /// It is the sum of the <see cref='System.Diagnostics.Process.UserProcessorTime'/> and
        /// <see cref='System.Diagnostics.Process.PrivilegedProcessorTime'/>.
        /// </summary>
        public TimeSpan TotalProcessorTime
        {
            get
            {
                Interop.procfs.ParsedStat stat = GetStat();
                return TicksToTimeSpan(stat.utime + stat.stime);
            }
        }

        /// <summary>
        /// Gets the amount of time the associated process has spent running code
        /// inside the application portion of the process (not the operating system core).
        /// </summary>
        public TimeSpan UserProcessorTime
        {
            get
            {
                return TicksToTimeSpan(GetStat().utime);
            }
        }

        /// <summary>
        /// Gets or sets which processors the threads in this process can be scheduled to run on.
        /// </summary>
        private unsafe IntPtr ProcessorAffinityCore
        {
            get
            {
                EnsureState(State.HaveId);

                Interop.libc.cpu_set_t set = default(Interop.libc.cpu_set_t);
                if (Interop.libc.sched_getaffinity(_processId, (IntPtr)sizeof(Interop.libc.cpu_set_t), &set) != 0)
                {
                    throw new Win32Exception(); // match Windows exception
                }

                ulong bits = 0;
                int maxCpu = IntPtr.Size == 4 ? 32 : 64;
                for (int cpu = 0; cpu < maxCpu; cpu++)
                {
                    if (Interop.libc.CPU_ISSET(cpu, &set))
                        bits |= (1u << cpu);
                }
                return (IntPtr)bits;
            }
            set
            {
                EnsureState(State.HaveId);

                Interop.libc.cpu_set_t set = default(Interop.libc.cpu_set_t);

                long bits = (long)value;
                int maxCpu = IntPtr.Size == 4 ? 32 : 64;
                for (int cpu = 0; cpu < maxCpu; cpu++)
                {
                    if ((bits & (1u << cpu)) != 0)
                        Interop.libc.CPU_SET(cpu, &set);
                }

                if (Interop.libc.sched_setaffinity(_processId, (IntPtr)sizeof(Interop.libc.cpu_set_t), &set) != 0)
                {
                    throw new Win32Exception(); // match Windows exception
                }
            }
        }


        /// <summary>
        /// Make sure we have obtained the min and max working set limits.
        /// </summary>
        private void GetWorkingSetLimits(out IntPtr minWorkingSet, out IntPtr maxWorkingSet)
        {
            minWorkingSet = IntPtr.Zero; // no defined limit available
            ulong rsslim = GetStat().rsslim;

            // rsslim is a ulong, but maxWorkingSet is an IntPtr, so we need to cap rsslim
            // at the max size of IntPtr.  This often happens when there is no configured 
            // rsslim other than ulong.MaxValue, which without these checks would show up
            // as a maxWorkingSet == -1.
            switch (IntPtr.Size)
            {
                case 4:
                    if (rsslim > int.MaxValue)
                        rsslim = int.MaxValue;
                    break;
                case 8:
                    if (rsslim > long.MaxValue)
                        rsslim = long.MaxValue;
                    break;
            }

            maxWorkingSet = (IntPtr)rsslim;
        }

        /// <summary>Sets one or both of the minimum and maximum working set limits.</summary>
        /// <param name="newMin">The new minimum working set limit, or null not to change it.</param>
        /// <param name="newMax">The new maximum working set limit, or null not to change it.</param>
        /// <param name="resultingMin">The resulting minimum working set limit after any changes applied.</param>
        /// <param name="resultingMax">The resulting maximum working set limit after any changes applied.</param>
        private void SetWorkingSetLimitsCore(IntPtr? newMin, IntPtr? newMax, out IntPtr resultingMin, out IntPtr resultingMax)
        {
            // RLIMIT_RSS with setrlimit not supported on Linux > 2.4.30.
            throw new PlatformNotSupportedException();
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        /// <summary>Gets the path to the current executable, or null if it could not be retrieved.</summary>
        private static string GetExePath()
        {
            // Determine the maximum size of a path
            int maxPath = Interop.Sys.MaxPath;

            // Start small with a buffer allocation, and grow only up to the max path
            for (int pathLen = 256; pathLen < maxPath; pathLen *= 2)
            {
                // Read from procfs the symbolic link to this process' executable
                byte[] buffer = new byte[pathLen + 1]; // +1 for null termination
                int resultLength = Interop.Sys.ReadLink(Interop.procfs.SelfExeFilePath, buffer, pathLen);

                // If we got one, null terminate it (readlink doesn't do this) and return the string
                if (resultLength > 0)
                {
                    buffer[resultLength] = (byte)'\0';
                    return Encoding.UTF8.GetString(buffer, 0, resultLength);
                }

                // If the buffer was too small, loop around again and try with a larger buffer.
                // Otherwise, bail.
                if (resultLength == 0 || Interop.Sys.GetLastError() != Interop.Error.ENAMETOOLONG)
                {
                    break;
                }
            }

            // Could not get a path
            return null;
        }

        // ----------------------------------
        // ---- Unix PAL layer ends here ----
        // ----------------------------------

        /// <summary>Computes a time based on a number of ticks since boot.</summary>
        /// <param name="ticksAfterBoot">The number of ticks since boot.</param>
        /// <returns>The converted time.</returns>
        internal static DateTime BootTimeToDateTime(ulong ticksAfterBoot)
        {
            // Read procfs to determine the system's uptime, aka how long ago it booted
            string uptimeStr = File.ReadAllText(Interop.procfs.ProcUptimeFilePath, Encoding.UTF8);
            int spacePos = uptimeStr.IndexOf(' ');
            double uptime;
            if (spacePos < 1 || !double.TryParse(uptimeStr.Substring(0, spacePos), out uptime))
            {
                throw new Win32Exception();
            }

            // Use the uptime and the current time to determine the absolute boot time
            DateTime bootTime = DateTime.UtcNow - TimeSpan.FromSeconds(uptime);

            // And use that to determine the absolute time for ticksStartedAfterBoot
            DateTime dt = bootTime + TicksToTimeSpan(ticksAfterBoot);

            // The return value is expected to be in the local time zone.
            // It is converted here (rather than starting with DateTime.Now) to avoid DST issues.
            return dt.ToLocalTime();
        }

        /// <summary>Reads the stats information for this process from the procfs file system.</summary>
        private Interop.procfs.ParsedStat GetStat()
        {
            EnsureState(State.HaveId);
            return Interop.procfs.ReadStatFile(_processId);
        }

    }
}
