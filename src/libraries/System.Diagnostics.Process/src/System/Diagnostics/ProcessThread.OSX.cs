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
        private static ThreadPriorityLevel PriorityLevelCore
        {
            // Does not appear to be a POSIX API to do this on macOS.
            // Considered the posix pthread_getschedparam, and pthread_setschedparam,
            // but those seems to specify the scheduling policy with the priority.
            get { throw new PlatformNotSupportedException(SR.ThreadPriorityNotSupported); }
            set { throw new PlatformNotSupportedException(SR.ThreadPriorityNotSupported); }
        }

        /// <summary>
        /// Returns the amount of time the thread has spent running code inside the operating
        /// system core.
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan PrivilegedProcessorTime => new TimeSpan((long)GetThreadInfo().pth_system_time);

        private static DateTime GetStartTime() => throw new PlatformNotSupportedException(); // macOS does not provide a way to get this data

        /// <summary>
        /// Returns the amount of time the associated thread has spent using the CPU.
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
                Interop.libproc.proc_threadinfo info = GetThreadInfo();
                return new TimeSpan((long)(info.pth_user_time + info.pth_system_time));
            }
        }

        /// <summary>
        /// Returns the amount of time the associated thread has spent running code
        /// inside the application (not the operating system core).
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan UserProcessorTime => new TimeSpan((long)GetThreadInfo().pth_user_time);

        private Interop.libproc.proc_threadinfo GetThreadInfo()
        {
            Interop.libproc.proc_threadinfo? info = Interop.libproc.GetThreadInfoById(_processId, _threadInfo._threadId);
            if (!info.HasValue)
            {
                throw new InvalidOperationException(SR.Format(SR.ThreadExited, Id));
            }
            return info.GetValueOrDefault();
        }
    }
}
