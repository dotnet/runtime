// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Diagnostics
{
    public partial class ProcessThread
    {
        private static ThreadPriorityLevel PriorityLevelCore
        {
            // Does not appear to be a POSIX API to do this on macOS.
            // Considered the posix pthread_getschedparam, and pthread_setschedparam,
            // but those seems to specify the scheduling policy with the priority.
            get { throw new PlatformNotSupportedException(SR.ThreadPriorityNotSupported); }
            set { throw new PlatformNotSupportedException(SR.ThreadPriorityNotSupported); }
        }

        private TimeSpan GetPrivilegedProcessorTime() => new TimeSpan((long)GetThreadInfo().pth_system_time);

        private static DateTime GetStartTime() => throw new PlatformNotSupportedException(); // macOS does not provide a way to get this data

        private TimeSpan GetTotalProcessorTime()
        {
            Interop.libproc.proc_threadinfo info = GetThreadInfo();
            return new TimeSpan((long)(info.pth_user_time + info.pth_system_time));
        }

        private TimeSpan GetUserProcessorTime() => new TimeSpan((long)GetThreadInfo().pth_user_time);

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
