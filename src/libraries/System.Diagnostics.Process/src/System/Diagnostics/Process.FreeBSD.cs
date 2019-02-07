// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;

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
        private int ParentProcessId =>
            throw new PlatformNotSupportedException();

        // <summary>Gets execution path</summary>
        private string GetPathToOpenFile()
        {
            Interop.Sys.FileStatus stat;
            if (Interop.Sys.Stat("/usr/local/bin/open", out stat) == 0)
            {
                return "/usr/local/bin/open";
            }
            else
            {
                return null;
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------
        /// <summary>Gets the path to the current executable, or null if it could not be retrieved.</summary>
        private static string GetExePath()
        {
            return Interop.Process.GetProcPath(Interop.Sys.GetPid());
        }

        // ----------------------------------
        // ---- Unix PAL layer ends here ----
        // ----------------------------------


    }
}
