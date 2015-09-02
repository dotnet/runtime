// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace System.Diagnostics
{
    public partial class Process
    {
        // The ri_proc_start_abstime needs to be converted to milliseconds to determine
        // the actual start time of the process.
        private const ulong MillisecondFactor = 100000000000;

        /// <summary>Gets the amount of time the process has spent running code inside the operating system core.</summary>
        public TimeSpan PrivilegedProcessorTime
        {
            get
            {
                Interop.libproc.rusage_info_v3 info = Interop.libproc.proc_pid_rusage(_processId);
                return new TimeSpan(Convert.ToInt64(info.ri_system_time));
            }
        }

        /// <summary>Gets the time the associated process was started.</summary>
        public DateTime StartTime
        {
            get
            {
                // Get the RUsage data and convert the process start time (which is the number of
                // nanoseconds before Now that the process started) to a DateTime.
                DateTime now = DateTime.UtcNow;
                Interop.libproc.rusage_info_v3 info = Interop.libproc.proc_pid_rusage(_processId);
                int milliseconds = Convert.ToInt32(info.ri_proc_start_abstime / MillisecondFactor);
                TimeSpan ts = new TimeSpan(0, 0, 0, 0, milliseconds);
                return now.Subtract(ts).ToLocalTime();
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
                Interop.libproc.rusage_info_v3 info = Interop.libproc.proc_pid_rusage(_processId);
                return new TimeSpan(Convert.ToInt64(info.ri_system_time + info.ri_user_time));
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
                Interop.libproc.rusage_info_v3 info = Interop.libproc.proc_pid_rusage(_processId);
                return new TimeSpan(Convert.ToInt64(info.ri_user_time));
            }
        }

        /// <summary>
        /// Gets or sets which processors the threads in this process can be scheduled to run on.
        /// </summary>
        private unsafe IntPtr ProcessorAffinityCore
        {
            get
            {
                throw new PlatformNotSupportedException(SR.ProcessorAffinityNotSupported);
            }
            set
            {
                throw new PlatformNotSupportedException(SR.ProcessorAffinityNotSupported);
            }
        }


        /// <summary>
        /// Make sure we have obtained the min and max working set limits.
        /// </summary>
        private void GetWorkingSetLimits(out IntPtr minWorkingSet, out IntPtr maxWorkingSet)
        {
            // We can only do this for the current process on OS X
            if (_processId != Interop.Sys.GetPid())
                throw new PlatformNotSupportedException(SR.OsxExternalProcessWorkingSetNotSupported);

            // Minimum working set (or resident set, as it is called on *nix) doesn't exist so set to 0
            minWorkingSet = IntPtr.Zero;

            // Get the max working set size
            Interop.Sys.RLimit limit;
            if (Interop.Sys.GetRLimit(Interop.Sys.RlimitResources.RLIMIT_RSS, out limit) == 0)
            {
                maxWorkingSet = limit.CurrentLimit == Interop.Sys.RLIM_INFINITY ?
                    new IntPtr(Int64.MaxValue) :
                    new IntPtr(Convert.ToInt64(limit.CurrentLimit));
            }
            else
            {
                // The contract specifies that this throws Win32Exception when it failes to retrieve the info
                throw new Win32Exception();
            }
        }

        /// <summary>Sets one or both of the minimum and maximum working set limits.</summary>
        /// <param name="newMin">The new minimum working set limit, or null not to change it.</param>
        /// <param name="newMax">The new maximum working set limit, or null not to change it.</param>
        /// <param name="resultingMin">The resulting minimum working set limit after any changes applied.</param>
        /// <param name="resultingMax">The resulting maximum working set limit after any changes applied.</param>
        private void SetWorkingSetLimitsCore(IntPtr? newMin, IntPtr? newMax, out IntPtr resultingMin, out IntPtr resultingMax)
        {
            // We can only do this for the current process on OS X
            if (_processId != Interop.Sys.GetPid())
                throw new PlatformNotSupportedException(SR.OsxExternalProcessWorkingSetNotSupported);

            // There isn't a way to set the minimum working set, so throw an exception here
            if (newMin.HasValue)
            {
                throw new PlatformNotSupportedException(SR.MinimumWorkingSetNotSupported);
            }

            // The minimum resident set will always be 0, default the resulting max to 0 until we set it (to make the compiler happy)
            resultingMin = IntPtr.Zero;
            resultingMax = IntPtr.Zero;

            // The default hard upper limit is absurdly high (over 9000PB) so just change the soft limit...especially since
            // if you aren't root and move the upper limit down, you need root to move it back up
            if (newMax.HasValue)
            {
                Interop.Sys.RLimit limits = new Interop.Sys.RLimit() { CurrentLimit = (ulong)newMax.Value.ToInt64() };
                int result = Interop.Sys.SetRLimit(Interop.Sys.RlimitResources.RLIMIT_RSS, ref limits);
                if (result != 0)
                {
                    throw new System.ComponentModel.Win32Exception(SR.RUsageFailure);
                }

                // Try to grab the actual value, in case the OS decides to fudge the numbers
                result = Interop.Sys.GetRLimit(Interop.Sys.RlimitResources.RLIMIT_RSS, out limits);
                if (result == 0) resultingMax = new IntPtr((long)limits.CurrentLimit);
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        /// <summary>Gets the path to the current executable, or null if it could not be retrieved.</summary>
        private static string GetExePath()
        {
            return Interop.libproc.proc_pidpath(Interop.Sys.GetPid());
        }

        // ----------------------------------
        // ---- Unix PAL layer ends here ----
        // ----------------------------------

        private Interop.libproc.rusage_info_v3 GetCurrentProcessRUsage()
        {
            return Interop.libproc.proc_pid_rusage(Interop.Sys.GetPid());
        }

    }
}
