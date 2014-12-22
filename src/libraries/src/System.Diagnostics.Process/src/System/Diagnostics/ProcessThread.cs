// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Diagnostics;
using System;
using System.Collections;
using System.IO;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Runtime.Versioning;

namespace System.Diagnostics
{
    /// <devdoc>
    ///    <para>
    ///       Represents a Win32 thread. This can be used to obtain
    ///       information about the thread, such as it's performance characteristics. This is
    ///       returned from the System.Diagnostics.Process.ProcessThread property of the System.Diagnostics.Process component.
    ///    </para>
    /// </devdoc>
    public class ProcessThread
    {
        //
        // FIELDS
        //

        private ThreadInfo _threadInfo;
        private bool _isRemoteMachine;
        private bool _priorityBoostEnabled;
        private bool _havePriorityBoostEnabled;
        private ThreadPriorityLevel _priorityLevel;
        private bool _havePriorityLevel;

        //
        // CONSTRUCTORS
        //

        /// <devdoc>
        ///     Internal constructor.
        /// </devdoc>
        /// <internalonly/>
        internal ProcessThread(bool isRemoteMachine, ThreadInfo threadInfo)
        {
            _isRemoteMachine = isRemoteMachine;
            _threadInfo = threadInfo;
            GC.SuppressFinalize(this);
        }

        //
        // PROPERTIES
        //

        /// <devdoc>
        ///     Returns the base priority of the thread which is computed by combining the
        ///     process priority class with the priority level of the associated thread.
        /// </devdoc>
        public int BasePriority
        {
            get
            {
                return _threadInfo.basePriority;
            }
        }

        /// <devdoc>
        ///     The current priority indicates the actual priority of the associated thread,
        ///     which may deviate from the base priority based on how the OS is currently
        ///     scheduling the thread.
        /// </devdoc>
        public int CurrentPriority
        {
            get
            {
                return _threadInfo.currentPriority;
            }
        }

        /// <devdoc>
        ///     Returns the unique identifier for the associated thread.
        /// </devdoc>
        public int Id
        {
            get
            {
                return _threadInfo.threadId;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Sets the processor that this thread would ideally like to run on.
        ///    </para>
        /// </devdoc>
        public int IdealProcessor
        {
            set
            {
                SafeThreadHandle threadHandle = null;
                try
                {
                    threadHandle = OpenThreadHandle(Interop.THREAD_SET_INFORMATION);
                    if (Interop.mincore.SetThreadIdealProcessor(threadHandle, value) < 0)
                    {
                        throw new Win32Exception();
                    }
                }
                finally
                {
                    CloseThreadHandle(threadHandle);
                }
            }
        }

        /// <devdoc>
        ///      Returns or sets whether this thread would like a priority boost if the user interacts
        ///      with user interface associated with this thread.
        /// </devdoc>
        public bool PriorityBoostEnabled
        {
            get
            {
                if (!_havePriorityBoostEnabled)
                {
                    SafeThreadHandle threadHandle = null;
                    try
                    {
                        threadHandle = OpenThreadHandle(Interop.THREAD_QUERY_INFORMATION);
                        bool disabled = false;
                        if (!Interop.mincore.GetThreadPriorityBoost(threadHandle, out disabled))
                        {
                            throw new Win32Exception();
                        }
                        _priorityBoostEnabled = !disabled;
                        _havePriorityBoostEnabled = true;
                    }
                    finally
                    {
                        CloseThreadHandle(threadHandle);
                    }
                }
                return _priorityBoostEnabled;
            }
            set
            {
                SafeThreadHandle threadHandle = null;
                try
                {
                    threadHandle = OpenThreadHandle(Interop.THREAD_SET_INFORMATION);
                    if (!Interop.mincore.SetThreadPriorityBoost(threadHandle, !value))
                        throw new Win32Exception();
                    _priorityBoostEnabled = value;
                    _havePriorityBoostEnabled = true;
                }
                finally
                {
                    CloseThreadHandle(threadHandle);
                }
            }
        }

        /// <devdoc>
        ///     Returns or sets the priority level of the associated thread.  The priority level is
        ///     not an absolute level, but instead contributes to the actual thread priority by
        ///     considering the priority class of the process.
        /// </devdoc>
        public ThreadPriorityLevel PriorityLevel
        {
            get
            {
                if (!_havePriorityLevel)
                {
                    SafeThreadHandle threadHandle = null;
                    try
                    {
                        threadHandle = OpenThreadHandle(Interop.THREAD_QUERY_INFORMATION);
                        int value = Interop.mincore.GetThreadPriority(threadHandle);
                        if (value == 0x7fffffff)
                        {
                            throw new Win32Exception();
                        }
                        _priorityLevel = (ThreadPriorityLevel)value;
                        _havePriorityLevel = true;
                    }
                    finally
                    {
                        CloseThreadHandle(threadHandle);
                    }
                }
                return _priorityLevel;
            }
            set
            {
                SafeThreadHandle threadHandle = null;
                try
                {
                    threadHandle = OpenThreadHandle(Interop.THREAD_SET_INFORMATION);
                    if (!Interop.mincore.SetThreadPriority(threadHandle, (int)value))
                    {
                        throw new Win32Exception();
                    }
                    _priorityLevel = value;
                }
                finally
                {
                    CloseThreadHandle(threadHandle);
                }
            }
        }

        /// <devdoc>
        ///     Returns the amount of time the thread has spent running code inside the operating
        ///     system core.
        /// </devdoc>
        public TimeSpan PrivilegedProcessorTime
        {
            get
            {
                EnsureState(State.IsNt);
                return GetThreadTimes().PrivilegedProcessorTime;
            }
        }

        /// <devdoc>
        ///     Returns the memory address of the function that was called when the associated
        ///     thread was started.
        /// </devdoc>
        public IntPtr StartAddress
        {
            get
            {
                EnsureState(State.IsNt);
                return _threadInfo.startAddress;
            }
        }

        /// <devdoc>
        ///     Returns the time the associated thread was started.
        /// </devdoc>
        public DateTime StartTime
        {
            get
            {
                EnsureState(State.IsNt);
                return GetThreadTimes().StartTime;
            }
        }

        /// <devdoc>
        ///     Returns the current state of the associated thread, e.g. is it running, waiting, etc.
        /// </devdoc>
        public ThreadState ThreadState
        {
            get
            {
                EnsureState(State.IsNt);
                return _threadInfo.threadState;
            }
        }

        /// <devdoc>
        ///     Returns the amount of time the associated thread has spent utilizing the CPU.
        ///     It is the sum of the System.Diagnostics.ProcessThread.UserProcessorTime and
        ///     System.Diagnostics.ProcessThread.PrivilegedProcessorTime.
        /// </devdoc>
        public TimeSpan TotalProcessorTime
        {
            get
            {
                EnsureState(State.IsNt);
                return GetThreadTimes().TotalProcessorTime;
            }
        }

        /// <devdoc>
        ///     Returns the amount of time the associated thread has spent running code
        ///     inside the application (not the operating system core).
        /// </devdoc>
        public TimeSpan UserProcessorTime
        {
            get
            {
                EnsureState(State.IsNt);
                return GetThreadTimes().UserProcessorTime;
            }
        }

        /// <devdoc>
        ///     Returns the reason the associated thread is waiting, if any.
        /// </devdoc>
        public ThreadWaitReason WaitReason
        {
            get
            {
                EnsureState(State.IsNt);
                if (_threadInfo.threadState != ThreadState.Wait)
                {
                    throw new InvalidOperationException(SR.WaitReasonUnavailable);
                }
                return _threadInfo.threadWaitReason;
            }
        }

        //
        // METHODS
        //

        /// <devdoc>
        ///     Helper to close a thread handle.
        /// </devdoc>
        /// <internalonly/>
        private static void CloseThreadHandle(SafeThreadHandle handle)
        {
            if (handle != null)
            {
                handle.Dispose();
            }
        }

        /// <devdoc>
        ///     Helper to check preconditions for property access.
        /// </devdoc>
        void EnsureState(State state)
        {
            if (((state & State.IsLocal) != (State)0) && _isRemoteMachine)
            {
                throw new NotSupportedException(SR.NotSupportedRemoteThread);
            }
        }

        /// <devdoc>
        ///     Helper to open a thread handle.
        /// </devdoc>
        /// <internalonly/>
        SafeThreadHandle OpenThreadHandle(int access)
        {
            EnsureState(State.IsLocal);
            return ProcessManager.OpenThread(_threadInfo.threadId, access);
        }

        /// <devdoc>
        ///     Resets the ideal processor so there is no ideal processor for this thread (e.g.
        ///     any processor is ideal).
        /// </devdoc>
        public void ResetIdealProcessor()
        {
            // 32 means "any processor is fine"
            IdealProcessor = 32;
        }

        /// <devdoc>
        ///     Sets which processors the associated thread is allowed to be scheduled to run on.
        ///     Each processor is represented as a bit: bit 0 is processor one, bit 1 is processor
        ///     two, etc.  For example, the value 1 means run on processor one, 2 means run on
        ///     processor two, 3 means run on processor one or two.
        /// </devdoc>
        public IntPtr ProcessorAffinity
        {
            set
            {
                SafeThreadHandle threadHandle = null;
                try
                {
                    threadHandle = OpenThreadHandle(Interop.THREAD_SET_INFORMATION | Interop.THREAD_QUERY_INFORMATION);
                    if (Interop.mincore.SetThreadAffinityMask(threadHandle, value) == IntPtr.Zero)
                    {
                        throw new Win32Exception();
                    }
                }
                finally
                {
                    CloseThreadHandle(threadHandle);
                }
            }
        }

        private ProcessThreadTimes GetThreadTimes()
        {
            ProcessThreadTimes threadTimes = new ProcessThreadTimes();

            SafeThreadHandle threadHandle = null;
            try
            {
                threadHandle = OpenThreadHandle(Interop.THREAD_QUERY_INFORMATION);

                if (!Interop.mincore.GetThreadTimes(threadHandle,
                    out threadTimes.create,
                    out threadTimes.exit,
                    out threadTimes.kernel,
                    out threadTimes.user))
                {
                    throw new Win32Exception();
                }
            }
            finally
            {
                CloseThreadHandle(threadHandle);
            }

            return threadTimes;
        }


        /// <summary>
        ///      Preconditions for accessing properties.
        /// </summary>
        /// <internalonly/>
        enum State
        {
            IsLocal = 0x2,
            IsNt = 0x4
        }
    }
}
