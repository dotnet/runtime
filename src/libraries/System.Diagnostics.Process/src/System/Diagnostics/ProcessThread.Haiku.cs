// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
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
                Interop.OS.thread_info info;
                int status = Interop.OS.GetThreadInfo(Id, out info);

                if (status != 0)
                {
                    throw new Win32Exception(status);
                }

                // thread_info.priority are BeOS-style priority values, not POSIX nice values (those returned by getpriority()).
                return
                    (info.priority >= (int)Interop.OS.BPriority.B_REAL_TIME_DISPLAY_PRIORITY) ? ThreadPriorityLevel.TimeCritical :
                    (info.priority >= (int)Interop.OS.BPriority.B_URGENT_DISPLAY_PRIORITY) ? ThreadPriorityLevel.Highest :
                    (info.priority >= (int)Interop.OS.BPriority.B_DISPLAY_PRIORITY) ? ThreadPriorityLevel.AboveNormal :
                    (info.priority >= (int)Interop.OS.BPriority.B_NORMAL_PRIORITY) ? ThreadPriorityLevel.Normal :
                    (info.priority >= (int)Interop.OS.BPriority.B_LOW_PRIORITY) ? ThreadPriorityLevel.BelowNormal :
                    (info.priority >= (int)Interop.OS.BPriority.B_LOWEST_ACTIVE_PRIORITY) ? ThreadPriorityLevel.Lowest :
                    ThreadPriorityLevel.Idle;
            }
            set
            {
                int newPriority = (value == ThreadPriorityLevel.TimeCritical) ? (int)Interop.OS.BPriority.B_REAL_TIME_DISPLAY_PRIORITY :
                    (value == ThreadPriorityLevel.Highest) ? (int)Interop.OS.BPriority.B_URGENT_DISPLAY_PRIORITY :
                    (value == ThreadPriorityLevel.AboveNormal) ? (int)Interop.OS.BPriority.B_DISPLAY_PRIORITY :
                    (value == ThreadPriorityLevel.Normal) ? (int)Interop.OS.BPriority.B_NORMAL_PRIORITY :
                    (value == ThreadPriorityLevel.BelowNormal) ? (int)Interop.OS.BPriority.B_LOW_PRIORITY :
                    (value == ThreadPriorityLevel.Lowest) ? (int)Interop.OS.BPriority.B_LOWEST_ACTIVE_PRIORITY :
                    (int)Interop.OS.BPriority.B_IDLE_PRIORITY;

                int status = Interop.OS.SetThreadPriority(Id, newPriority);

                if (status != 0)
                {
                    throw new Win32Exception(status);
                }
            }
        }

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
                Interop.OS.thread_info info;
                int status = Interop.OS.GetThreadInfo(Id, out info);

                if (status != 0)
                {
                    throw new Win32Exception(status);
                }

                return TimeSpan.FromMicroseconds(info.user_time + info.kernel_time);
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
                Interop.OS.thread_info info;
                int status = Interop.OS.GetThreadInfo(Id, out info);

                if (status != 0)
                {
                    throw new Win32Exception(status);
                }

                return TimeSpan.FromMicroseconds(info.user_time);
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
                Interop.OS.thread_info info;
                int status = Interop.OS.GetThreadInfo(Id, out info);

                if (status != 0)
                {
                    throw new Win32Exception(status);
                }

                return TimeSpan.FromMicroseconds(info.kernel_time);
            }
        }
    }
}
