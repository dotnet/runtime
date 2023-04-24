// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Versioning;
using System.Text;

namespace System.Diagnostics
{
    public partial class ProcessThread
    {
        /// <summary>
        /// Returns or sets the priority level of the associated thread.  The priority level is
        /// not an absolute level, but instead contributes to the actual thread priority by
        /// considering the priority class of the process.
        /// </summary>
        private ThreadPriorityLevel PriorityLevelCore
        {
            // This mapping is relatively arbitrary.  0 is normal based on the man page,
            // and the other values above and below are simply distributed evenly.
            get
            {
                Interop.procfs.ParsedStat stat = GetStat();
                return Interop.Sys.GetThreadPriorityFromNiceValue((int)stat.nice);
            }
            set
            {
                throw new PlatformNotSupportedException(); // We can find no API to set this on Linux
            }
        }

        /// <summary>
        /// Returns the amount of time the thread has spent running code inside the operating
        /// system core.
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan PrivilegedProcessorTime
        {
            get
            {
                Interop.procfs.ParsedStat stat = GetStat();
                return Process.TicksToTimeSpan(stat.stime);
            }
        }

        private DateTime GetStartTime() => Process.BootTimeToDateTime(Process.TicksToTimeSpan(GetStat().starttime));

        /// <summary>
        /// Returns the amount of time the associated thread has spent utilizing the CPU.
        /// It is the sum of the System.Diagnostics.ProcessThread.UserProcessorTime and
        /// System.Diagnostics.ProcessThread.PrivilegedProcessorTime.
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan TotalProcessorTime
        {
            get
            {
                Interop.procfs.ParsedStat stat = GetStat();
                return Process.TicksToTimeSpan(stat.utime + stat.stime);
            }
        }

        /// <summary>
        /// Returns the amount of time the associated thread has spent running code
        /// inside the application (not the operating system core).
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan UserProcessorTime
        {
            get
            {
                Interop.procfs.ParsedStat stat = GetStat();
                return Process.TicksToTimeSpan(stat.utime);
            }
        }

        private Interop.procfs.ParsedStat GetStat()
        {
            Interop.procfs.ParsedStat stat;
            if (!Interop.procfs.TryReadStatFile(pid: _processId, tid: Id, result: out stat))
            {
                throw new InvalidOperationException(SR.Format(SR.ThreadExited, Id));
            }
            return stat;
        }
    }
}
