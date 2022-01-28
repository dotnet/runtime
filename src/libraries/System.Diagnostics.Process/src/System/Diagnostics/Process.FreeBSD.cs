// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
    public partial class Process
    {
        /// <summary>Gets the time the associated process was started.</summary>
        internal DateTime StartTimeCore
        {
            get
            {
                EnsureState(State.HaveNonExitedId);
                Interop.Process.proc_stats stat = Interop.Process.GetThreadInfo(_processId, 0);

                return new DateTime(DateTime.UnixEpoch.Ticks + (stat.startTime * TimeSpan.TicksPerSecond)).ToLocalTime();
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
                EnsureState(State.HaveNonExitedId);
                Interop.Process.proc_stats stat = Interop.Process.GetThreadInfo(_processId, 0);
                return Process.TicksToTimeSpan(stat.userTime + stat.systemTime);
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

                Interop.Process.proc_stats stat = Interop.Process.GetThreadInfo(_processId, 0);
                return Process.TicksToTimeSpan(stat.userTime);
            }
        }

        /// <summary>Gets the amount of time the process has spent running code inside the operating system core.</summary>
        public TimeSpan PrivilegedProcessorTime
        {
            get
            {
                EnsureState(State.HaveNonExitedId);

                Interop.Process.proc_stats stat = Interop.Process.GetThreadInfo(_processId, 0);
                return Process.TicksToTimeSpan(stat.systemTime);
            }
        }

        /// <summary>Gets parent process ID</summary>
        private unsafe int ParentProcessId
        {
            get
            {
                EnsureState(State.HaveNonExitedId);

                Interop.Process.kinfo_proc* processInfo = Interop.Process.GetProcInfo(_processId, false, out int count);
                try
                {
                    if (count <= 0)
                    {
                        throw new Win32Exception(SR.ProcessInformationUnavailable);
                    }

                    return processInfo->ki_ppid;
                }
                finally
                {
                    Marshal.FreeHGlobal((IntPtr) processInfo);
                }
            }
        }

        // <summary>Gets execution path</summary>
        private static string? GetPathToOpenFile()
        {
            if (Interop.Sys.Stat("/usr/local/bin/open", out _) == 0)
            {
                return "/usr/local/bin/open";
            }
            else
            {
                return null;
            }
        }

        // ----------------------------------
        // ---- Unix PAL layer ends here ----
        // ----------------------------------


    }
}
