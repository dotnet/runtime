// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Runtime.Versioning;

namespace System.Diagnostics
{
    public partial class ProcessThread
    {
        private void SetIdealProcessor(int value)
        {
            using (SafeThreadHandle threadHandle = OpenThreadHandle(Interop.Kernel32.ThreadOptions.THREAD_SET_INFORMATION))
            {
                if (Interop.Kernel32.SetThreadIdealProcessor(threadHandle, value) < 0)
                {
                    throw new Win32Exception();
                }
            }
        }

        private void ResetIdealProcessorCore()
        {
            // MAXIMUM_PROCESSORS == 32 on 32-bit or 64 on 64-bit, and means the thread has no preferred processor
            int MAXIMUM_PROCESSORS = IntPtr.Size == 4 ? 32 : 64;
            IdealProcessor = MAXIMUM_PROCESSORS;
        }

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

        private void SetProcessorAffinity(IntPtr value)
        {
            using (SafeThreadHandle threadHandle = OpenThreadHandle(Interop.Kernel32.ThreadOptions.THREAD_SET_INFORMATION | Interop.Kernel32.ThreadOptions.THREAD_QUERY_INFORMATION))
            {
                if (Interop.Kernel32.SetThreadAffinityMask(threadHandle, value) == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }
            }
        }

        private TimeSpan GetPrivilegedProcessorTime() => GetThreadTimes().PrivilegedProcessorTime;

        private DateTime GetStartTime() => GetThreadTimes().StartTime;

        private TimeSpan GetTotalProcessorTime() => GetThreadTimes().TotalProcessorTime;

        private TimeSpan GetUserProcessorTime() => GetThreadTimes().UserProcessorTime;

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
