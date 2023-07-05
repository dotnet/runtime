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
                return new TimeSpan(Convert.ToInt64(info.ri_system_time / NanosecondsTo100NanosecondsFactor));
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
                return new TimeSpan(Convert.ToInt64((info.ri_system_time + info.ri_user_time) / NanosecondsTo100NanosecondsFactor));
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
                return new TimeSpan(Convert.ToInt64(info.ri_user_time / NanosecondsTo100NanosecondsFactor));
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
    }
}
