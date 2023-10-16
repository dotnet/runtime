// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Versioning;

namespace System.Diagnostics
{
    public partial class Process
    {
        private const int NanosecondsTo100NanosecondsFactor = 100;
        private static volatile uint s_timeBase_numer, s_timeBase_denom;

        private const int MicrosecondsToSecondsFactor = 1_000_000;

        /// <summary>Gets the amount of time the process has spent running code inside the operating system core.</summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan PrivilegedProcessorTime
        {
            get
            {
                EnsureState(State.HaveNonExitedId);
                Interop.libproc.rusage_info_v3 info = Interop.libproc.proc_pid_rusage(_processId);
                return MapTime(info.ri_system_time);
            }
        }

        /// <summary>Gets the time the associated process was started.</summary>
        internal DateTime StartTimeCore
        {
            get
            {
                EnsureState(State.HaveNonExitedId);
                Interop.libproc.proc_taskallinfo? info = Interop.libproc.GetProcessInfoById(Id);

                if (info == null)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);

                DateTime startTime = DateTime.UnixEpoch + TimeSpan.FromSeconds(info.Value.pbsd.pbi_start_tvsec + info.Value.pbsd.pbi_start_tvusec / (double)MicrosecondsToSecondsFactor);

                // The return value is expected to be in the local time zone.
                return startTime.ToLocalTime();
            }
        }

        /// <summary>Gets execution path</summary>
        private static string GetPathToOpenFile()
        {
            return "/usr/bin/open";
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
                Interop.libproc.rusage_info_v3 info = Interop.libproc.proc_pid_rusage(_processId);
                return MapTime(info.ri_system_time + info.ri_user_time);
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
                Interop.libproc.rusage_info_v3 info = Interop.libproc.proc_pid_rusage(_processId);
                return MapTime(info.ri_user_time);
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

        // ----------------------------------
        // ---- Unix PAL layer ends here ----
        // ----------------------------------

        private static Interop.libproc.rusage_info_v3 GetCurrentProcessRUsage()
        {
            return Interop.libproc.proc_pid_rusage(Environment.ProcessId);
        }

        private static TimeSpan MapTime(ulong sysTime)
        {
            uint denom = s_timeBase_denom;
            if (denom == default)
            {
                Interop.libSystem.mach_timebase_info_data_t timeBase = GetTimeBase();
                s_timeBase_denom = denom = timeBase.denom;
                s_timeBase_numer = timeBase.numer;
            }
            uint numer = s_timeBase_numer;

            // By dividing by NanosecondsTo100NanosecondsFactor first, we lose some precision, but increase the range
            // where no overflow will happen.
            return new TimeSpan(Convert.ToInt64(sysTime / NanosecondsTo100NanosecondsFactor * numer / denom));
        }

        private static unsafe Interop.libSystem.mach_timebase_info_data_t GetTimeBase()
        {
            Interop.libSystem.mach_timebase_info_data_t timeBase = default;
            var returnCode = Interop.libSystem.mach_timebase_info(&timeBase);
            Debug.Assert(returnCode == 0, $"Non-zero exit code from mach_timebase_info: {returnCode}");
            if (returnCode != 0)
            {
                // Fallback: let's assume that the time values are in nanoseconds,
                // i.e. the time base is 1/1.
                timeBase.numer = timeBase.denom = 1;
            }
            return timeBase;
        }
    }
}
