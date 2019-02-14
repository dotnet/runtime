// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;

namespace System.Diagnostics
{
    public partial class Process
    {
        /// <summary>Gets the amount of time the process has spent running code inside the operating system core.</summary>
        public TimeSpan PrivilegedProcessorTime
        {
            get
            {
                EnsureState(State.HaveNonExitedId);
                Interop.libproc.rusage_info_v3 info = Interop.libproc.proc_pid_rusage(_processId);
                return new TimeSpan(Convert.ToInt64(info.ri_system_time));
            }
        }

        /// <summary>Gets the time the associated process was started.</summary>
        internal DateTime StartTimeCore
        {
            get
            {
                // Get the RUsage data and convert the process start time.
                // To calculcate the process start time in OSX we call proc_pid_rusage() and use ri_proc_start_abstime.
                //
                // ri_proc_start_abstime is absolute time which is the time since some reference point. The reference point
                // usually is the system boot time but this is not necessary true in all versions of OSX. For example in Sierra 
                // Mac OS 10.12 the reference point is not really the boot time. To be always accurate in our calculations
                // we don’t assume the reference point to be the boot time and instead we calculate it by calling GetTimebaseInfo
                // which is a wrapper for native mach_absolute_time(). That method returns the current time referenced to the needed start point
                // Then we can subtract the returned time from DateTime.UtcNow and we’ll get the exact reference point.
                //
                // The absolute time is measured by the bus cycle of the processor which is measured in nanoseconds multiplied 
                // by some factor “numer / denom”. In most cases the factor is (1 / 1) but we have to get the factor and use it just 
                // in case we run on a machine has different factor. To get the factor we call GetTimebaseInfo the wrapper for the 
                // mach_timebase_info() which give us the factor. Then we multiply the factor by the absolute time and the divide
                // the result by 10^9 to convert it from nanoseconds to seconds.

                EnsureState(State.HaveNonExitedId);

                uint numer, denom;
                Interop.Sys.GetTimebaseInfo(out numer, out denom);
                Interop.libproc.rusage_info_v3 info = Interop.libproc.proc_pid_rusage(_processId);
                ulong absoluteTime;

                if (!Interop.Sys.GetAbsoluteTime(out absoluteTime))
                {
                    throw new Win32Exception(SR.RUsageFailure);
                }

                // usually seconds will be negative
                double seconds = (((long)info.ri_proc_start_abstime - (long)absoluteTime) * (double)numer / denom) / NanoSecondToSecondFactor;
                return DateTime.UtcNow.AddSeconds(seconds).ToLocalTime();
            }
        }

        /// <summary>Gets execution path</summary>
        private string GetPathToOpenFile()
        {
            return "/usr/bin/open";
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
                EnsureState(State.HaveNonExitedId);
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
                EnsureState(State.HaveNonExitedId);
                Interop.libproc.rusage_info_v3 info = Interop.libproc.proc_pid_rusage(_processId);
                return new TimeSpan(Convert.ToInt64(info.ri_user_time));
            }
        }

        /// <summary>Gets parent process ID</summary>
        private int ParentProcessId
        {
            get
            {
                EnsureState(State.HaveNonExitedId);
                Interop.libproc.proc_taskallinfo? info = Interop.libproc.GetProcessInfoById(Id);

                if (info == null)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);

                return Convert.ToInt32(info.Value.pbsd.pbi_ppid);
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

        // The ri_proc_start_abstime needs to be converted to seconds to determine
        // the actual start time of the process.
        private const int NanoSecondToSecondFactor = 1000000000;

        private Interop.libproc.rusage_info_v3 GetCurrentProcessRUsage()
        {
            return Interop.libproc.proc_pid_rusage(Interop.Sys.GetPid());
        }
    }
}
