// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

// TODO: remove
#pragma warning disable CA1822

namespace System.Diagnostics
{
    public partial class ProcessThread
    {

        /// <summary>Gets the time this thread was started.</summary>
        internal DateTime GetStartTime()
        {
            Interop.procfs.ThreadInfo iinfo = GetThreadInfo();

            DateTime startTime = DateTime.UnixEpoch +
                TimeSpan.FromSeconds(iinfo.StartTime.TvSec) +
                TimeSpan.FromMicroseconds(iinfo.StartTime.TvNsec / 1000);

            // The return value is expected to be in the local time zone.
            return startTime.ToLocalTime();
        }

        /// <summary>
        /// Returns or sets the priority level of the associated thread.  The priority level is
        /// not an absolute level, but instead contributes to the actual thread priority by
        /// considering the priority class of the process.
        /// </summary>
        private ThreadPriorityLevel PriorityLevelCore
        {
            get
            {
                Interop.procfs.ThreadInfo iinfo = GetThreadInfo();
                return GetThreadPriorityFromSysPri(iinfo.Priority);
            }
            set
            {
                // Raising priority is a privileged operation.
                // Might be able to adjust our "nice" value.   Maybe later...
                throw new PlatformNotSupportedException();
            }
        }

        /// <summary>
        /// Gets the amount of time the associated thread has spent utilizing the CPU.
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
                // a.k.a. "user" + "system" time
                Interop.procfs.ThreadInfo iinfo = GetThreadInfo();
                TimeSpan ts = TimeSpan.FromSeconds(iinfo.CpuTotalTime.TvSec) +
                    TimeSpan.FromMicroseconds(iinfo.CpuTotalTime.TvNsec / 1000);
                return ts;
            }
        }

        /// <summary>
        /// Gets the amount of time the associated thread has spent running code
        /// inside the application (not the operating system core).
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan UserProcessorTime
        {
            get
            {
                // a.k.a. "user" time
                // Could get this from /proc/$pid/lwp/$lwpid/lwpstatus
                // Just say it's all user time for now
                return TotalProcessorTime;
            }
        }

        /// <summary>
        /// Gets the amount of time the thread has spent running code inside the operating
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
                // Could get this from /proc/$pid/lwp/$lwpid/lwpstatus
                // Just say it's all user time for now
                return TimeSpan.Zero;
            }

        }

        // ----------------------------------
        // ---- exported stuff ends here ----
        // ----------------------------------

        // XXX Is there a better place for this?  Linux uses Interop.Sys.GetThreadPriorityFromNiceValue()
        // System priorities go from 1 to 100, where 60 and above are for "system" things
        // These mappingsare relatively arbitrary.  Normal user processes run at priority 59.
        // and the other values above and below are simply distributed somewhat evenly.
        private static System.Diagnostics.ThreadPriorityLevel GetThreadPriorityFromSysPri(int pri)
        {
            Debug.Assert((pri >= 0) && (pri <= 100));
            return
                (pri >= 90) ? ThreadPriorityLevel.TimeCritical :
                (pri >= 80) ? ThreadPriorityLevel.Highest :
                (pri >= 60)  ? ThreadPriorityLevel.AboveNormal :
                (pri == 59)  ? ThreadPriorityLevel.Normal :
                (pri >= 40)  ? ThreadPriorityLevel.BelowNormal :
                (pri >= 20) ? ThreadPriorityLevel.Lowest :
                ThreadPriorityLevel.Idle;
        }

        /// <summary>Reads the information for this thread from the procfs file system.</summary>
        private Interop.procfs.ThreadInfo GetThreadInfo()
        {
            Interop.procfs.ThreadInfo iinfo;
            if (!Interop.procfs.TryGetThreadInfoById(_processId, tid: Id, out iinfo))
            {
                throw new InvalidOperationException(SR.Format(SR.ThreadExited, Id));
            }
            return iinfo;
        }

    }
}
