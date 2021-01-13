// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Runtime.Versioning;

namespace System.Diagnostics
{
    public partial class ProcessThread
    {
        /// <summary>Sets the processor that this thread would ideally like to run on.</summary>
        public int IdealProcessor
        {
            set
            {
                using (SafeThreadHandle threadHandle = OpenThreadHandle(Interop.Kernel32.ThreadOptions.THREAD_SET_INFORMATION))
                {
                    if (Interop.Kernel32.SetThreadIdealProcessor(threadHandle, value) < 0)
                    {
                        throw new Win32Exception();
                    }
                }
            }
        }

        /// <summary>
        /// Resets the ideal processor so there is no ideal processor for this thread (e.g.
        /// any processor is ideal).
        /// </summary>
        public void ResetIdealProcessor()
        {
            // MAXIMUM_PROCESSORS == 32 on 32-bit or 64 on 64-bit, and means the thread has no preferred processor
            int MAXIMUM_PROCESSORS = IntPtr.Size == 4 ? 32 : 64;
            IdealProcessor = MAXIMUM_PROCESSORS;
        }

        /// <summary>
        /// Returns or sets whether this thread would like a priority boost if the user interacts
        /// with user interface associated with this thread.
        /// </summary>
        private bool PriorityBoostEnabledCore
        {
            get
            {
                using (SafeThreadHandle threadHandle = OpenThreadHandle(Interop.Kernel32.ThreadOptions.THREAD_QUERY_INFORMATION))
                {
                    bool disabled;
                    if (!Interop.Kernel32.GetThreadPriorityBoost(threadHandle, out disabled))
                    {
                        throw new Win32Exception();
                    }
                    return !disabled;
                }
            }
            set
            {
                using (SafeThreadHandle threadHandle = OpenThreadHandle(Interop.Kernel32.ThreadOptions.THREAD_SET_INFORMATION))
                {
                    if (!Interop.Kernel32.SetThreadPriorityBoost(threadHandle, !value))
                        throw new Win32Exception();
                }
            }
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
                using (SafeThreadHandle threadHandle = OpenThreadHandle(Interop.Kernel32.ThreadOptions.THREAD_QUERY_INFORMATION))
                {
                    int value = Interop.Kernel32.GetThreadPriority(threadHandle);
                    if (value == 0x7fffffff)
                    {
                        throw new Win32Exception();
                    }
                    return (ThreadPriorityLevel)value;
                }
            }
            set
            {
                using (SafeThreadHandle threadHandle = OpenThreadHandle(Interop.Kernel32.ThreadOptions.THREAD_SET_INFORMATION))
                {
                    if (!Interop.Kernel32.SetThreadPriority(threadHandle, (int)value))
                    {
                        throw new Win32Exception();
                    }
                }
            }
        }

        /// <summary>
        /// Sets which processors the associated thread is allowed to be scheduled to run on.
        /// Each processor is represented as a bit: bit 0 is processor one, bit 1 is processor
        /// two, etc.  For example, the value 1 means run on processor one, 2 means run on
        /// processor two, 3 means run on processor one or two.
        /// </summary>
        [SupportedOSPlatform("windows")]
        public IntPtr ProcessorAffinity
        {
            set
            {
                using (SafeThreadHandle threadHandle = OpenThreadHandle(Interop.Kernel32.ThreadOptions.THREAD_SET_INFORMATION | Interop.Kernel32.ThreadOptions.THREAD_QUERY_INFORMATION))
                {
                    if (Interop.Kernel32.SetThreadAffinityMask(threadHandle, value) == IntPtr.Zero)
                    {
                        throw new Win32Exception();
                    }
                }
            }
        }

        /// <summary>
        /// Returns the amount of time the thread has spent running code inside the operating
        /// system core.
        /// </summary>
        public TimeSpan PrivilegedProcessorTime
        {
            get { return GetThreadTimes().PrivilegedProcessorTime; }
        }

        /// <summary>Returns the time the associated thread was started.</summary>
        public DateTime StartTime
        {
            get { return GetThreadTimes().StartTime; }
        }

        /// <summary>
        /// Returns the amount of time the associated thread has spent utilizing the CPU.
        /// It is the sum of the System.Diagnostics.ProcessThread.UserProcessorTime and
        /// System.Diagnostics.ProcessThread.PrivilegedProcessorTime.
        /// </summary>
        public TimeSpan TotalProcessorTime
        {
            get { return GetThreadTimes().TotalProcessorTime; }
        }

        /// <summary>
        /// Returns the amount of time the associated thread has spent running code
        /// inside the application (not the operating system core).
        /// </summary>
        public TimeSpan UserProcessorTime
        {
            get { return GetThreadTimes().UserProcessorTime; }
        }

        /// <summary>Gets timing information for the thread.</summary>
        private ProcessThreadTimes GetThreadTimes()
        {
            using (SafeThreadHandle threadHandle = OpenThreadHandle(Interop.Kernel32.ThreadOptions.THREAD_QUERY_INFORMATION))
            {
                var threadTimes = new ProcessThreadTimes();
                if (!Interop.Kernel32.GetThreadTimes(threadHandle,
                    out threadTimes._create, out threadTimes._exit,
                    out threadTimes._kernel, out threadTimes._user))
                {
                    throw new Win32Exception();
                }
                return threadTimes;
            }
        }

        /// <summary>Open a handle to the thread.</summary>
        private SafeThreadHandle OpenThreadHandle(int access)
        {
            EnsureState(State.IsLocal);
            return ProcessManager.OpenThread((int)_threadInfo._threadId, access);
        }
    }
}
