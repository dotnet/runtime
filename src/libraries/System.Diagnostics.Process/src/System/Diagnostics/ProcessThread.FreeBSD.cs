// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

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
            get
            {
                Interop.Process.proc_stats stat = Interop.Process.GetThreadInfo(_processId, Id);
                return Interop.Sys.GetThreadPriorityFromNiceValue((int)stat.nice);
            }
            set
            {
                throw new PlatformNotSupportedException(); // We can find no API to set this
            }
        }

        // kinfo_proc has one  entry per thread but ki_start seems to be same for
        // all threads e.g. reflects process start. This may be re-visited later.
        private static DateTime GetStartTime() => throw new PlatformNotSupportedException();

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
                Interop.Process.proc_stats stat = Interop.Process.GetThreadInfo(_processId, Id);
                return Process.TicksToTimeSpan(stat.userTime + stat.systemTime);
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
                Interop.Process.proc_stats stat = Interop.Process.GetThreadInfo(_processId, Id);
                return Process.TicksToTimeSpan(stat.userTime);
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
                Interop.Process.proc_stats stat = Interop.Process.GetThreadInfo(_processId, Id);
                return Process.TicksToTimeSpan(stat.systemTime);
            }

        }
    }
}
