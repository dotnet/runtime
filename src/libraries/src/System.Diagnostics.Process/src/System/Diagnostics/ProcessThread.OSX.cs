// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

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
                throw new PlatformNotSupportedException();
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        /// <summary>
        /// Returns the amount of time the thread has spent running code inside the operating
        /// system core.
        /// </summary>
        public TimeSpan PrivilegedProcessorTime
        {
            get
            {
                Interop.libproc.proc_threadinfo? info = Interop.libproc.GetThreadInfoById(_processId, (ulong)_threadInfo._threadId);
                if (info.HasValue)
                    return new TimeSpan((long)info.Value.pth_system_time);
                else
                    throw new System.ComponentModel.Win32Exception();
            }
        }

        /// <summary>Returns the time the associated thread was started.</summary>
        public DateTime StartTime
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        /// <summary>
        /// Returns the amount of time the associated thread has spent utilizing the CPU.
        /// It is the sum of the System.Diagnostics.ProcessThread.UserProcessorTime and
        /// System.Diagnostics.ProcessThread.PrivilegedProcessorTime.
        /// </summary>
        public TimeSpan TotalProcessorTime
        {
            get
            {
                Interop.libproc.proc_threadinfo? info = Interop.libproc.GetThreadInfoById(_processId, (ulong)_threadInfo._threadId);
                if (info.HasValue)
                    return new TimeSpan((long)(info.Value.pth_user_time + info.Value.pth_system_time));
                else
                    throw new System.ComponentModel.Win32Exception();
            }
        }

        /// <summary>
        /// Returns the amount of time the associated thread has spent running code
        /// inside the application (not the operating system core).
        /// </summary>
        public TimeSpan UserProcessorTime
        {
            get
            {
                Interop.libproc.proc_threadinfo? info = Interop.libproc.GetThreadInfoById(_processId, (ulong)_threadInfo._threadId);
                if (info.HasValue)
                    return new TimeSpan((long)info.Value.pth_user_time);
                else
                    throw new System.ComponentModel.Win32Exception();
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

    }
}
