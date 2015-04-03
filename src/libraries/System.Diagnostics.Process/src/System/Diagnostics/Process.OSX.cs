// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Diagnostics
{
    public partial class Process
    {
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
                Interop.libproc.rusage_info_v3 info = Interop.libproc.proc_pid_rusage(_processId);
                return new DateTime(Convert.ToInt64(info.ri_proc_start_abstime));
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
            // Minimum working set (or resident set, as it is called on *nix) doesn't exist so set to 0
            minWorkingSet = IntPtr.Zero;

            // Get the max working set size
            Interop.libc.rlimit info = Interop.libc.getrlimit(Interop.libc.RLIMIT_Resources.RLIMIT_RSS);
            maxWorkingSet = new IntPtr(Convert.ToInt64(info.rlim_cur));
        }

        /// <summary>Sets one or both of the minimum and maximum working set limits.</summary>
        /// <param name="newMin">The new minimum working set limit, or null not to change it.</param>
        /// <param name="newMax">The new maximum working set limit, or null not to change it.</param>
        /// <param name="resultingMin">The resulting minimum working set limit after any changes applied.</param>
        /// <param name="resultingMax">The resulting maximum working set limit after any changes applied.</param>
        private void SetWorkingSetLimitsCore(IntPtr? newMin, IntPtr? newMax, out IntPtr resultingMin, out IntPtr resultingMax)
        {
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
                Interop.libc.rlimit limits = new Interop.libc.rlimit() { rlim_cur = (ulong)newMax.Value.ToInt64() };
                int result = Interop.libc.setrlimit(Interop.libc.RLIMIT_Resources.RLIMIT_RSS, ref limits);
                if (result < 0)
                {
                    throw new System.Runtime.InteropServices.COMException(SR.RUsageFailure, System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                }

                // Grab the actual value, in case the OS decides to fudge the numbers
                limits = Interop.libc.getrlimit(Interop.libc.RLIMIT_Resources.RLIMIT_RSS);
                resultingMax = new IntPtr((long)limits.rlim_cur);
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        /// <summary>Gets the path to the current executable, or null if it could not be retrieved.</summary>
        private static string GetExePath()
        {
            return Interop.libproc.proc_pidpath(Interop.libc.getpid());
        }

        // ----------------------------------
        // ---- Unix PAL layer ends here ----
        // ----------------------------------

        private Interop.libproc.rusage_info_v3 GetCurrentProcessRUsage()
        {
            return Interop.libproc.proc_pid_rusage(Interop.libc.getpid());
        }

    }
}
