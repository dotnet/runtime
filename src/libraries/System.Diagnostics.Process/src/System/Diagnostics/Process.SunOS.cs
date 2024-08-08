// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

// TODO: remove
#pragma warning disable CA1822

namespace System.Diagnostics
{
    public partial class Process : IDisposable
    {

        /// <summary>Gets the time the associated process was started.</summary>
        internal DateTime StartTimeCore
        {
            get
            {
                Interop.procfs.ProcessInfo iinfo = GetProcInfo();

                DateTime startTime = DateTime.UnixEpoch +
                    TimeSpan.FromSeconds(iinfo.StartTime.TvSec) +
                    TimeSpan.FromMicroseconds(iinfo.StartTime.TvNsec / 1000);

                // The return value is expected to be in the local time zone.
                return startTime.ToLocalTime();
            }
        }

        /// <summary>Gets the parent process ID</summary>
        private int ParentProcessId => GetProcInfo().ParentPid;

        /// <summary>Gets execution path</summary>
        private static string? GetPathToOpenFile()
        {
            return FindProgramInPath("xdg-open");
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
                // a.k.a. "user" + "system" time
                Interop.procfs.ProcessInfo iinfo = GetProcInfo();
                TimeSpan ts = TimeSpan.FromSeconds(iinfo.CpuTotalTime.TvSec) +
                    TimeSpan.FromMicroseconds(iinfo.CpuTotalTime.TvNsec / 1000);
                return ts;
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
                // a.k.a. "user" time
                // Could get this from /proc/$pid/status
                // Just say it's all user time for now
                return TotalProcessorTime;
            }
        }

        /// <summary>
        /// Gets the amount of time the process has spent running code inside the operating
        /// system core.
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan PrivilegedProcessorTime
        {
            get
            {
                // a.k.a. "system" time
                // Could get this from /proc/$pid/status
                // Just say it's all user time for now
                EnsureState(State.HaveNonExitedId);
                return TimeSpan.Zero;
            }
        }

        partial void EnsureHandleCountPopulated()
        {
            // TODO: remove this method if not needed
        }


        // ----------------------------------
        // ---- Unix PAL layer ends here ----
        // ----------------------------------

        /// <summary>Gets the name that was used to start the process, or null if it could not be retrieved.</summary>
        internal static string GetUntruncatedProcessName(ref Interop.procfs.ProcessInfo iinfo)
        {
            // Todo: If exec_fname matches the leading part of the args string,
            // use the args string up through the first space (XXX needs work).
            if (!string.IsNullOrEmpty(iinfo.Args))
            {
                string[] argv = iinfo.Args.Split(' ', 2);
                if (!string.IsNullOrEmpty(argv[0]))
                {
                    return Path.GetFileName(argv[0]);
                }
            }
            return "?";
        }

        /// <summary>Reads the information for this process from the procfs file system.</summary>
        private Interop.procfs.ProcessInfo GetProcInfo()
        {
            EnsureState(State.HaveNonExitedId);
            Interop.procfs.ProcessInfo iinfo;
            if (!Interop.procfs.TryGetProcessInfoById(_processId, out iinfo))
            {
                throw new Win32Exception(SR.ProcessInformationUnavailable);
            }
            return iinfo;
        }
    }
}
