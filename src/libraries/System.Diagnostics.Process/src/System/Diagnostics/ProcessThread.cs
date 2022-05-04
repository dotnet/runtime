// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
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
    [Designer("System.Diagnostics.Design.ProcessThreadDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public partial class ProcessThread : Component
    {
        private readonly bool _isRemoteMachine;
        private readonly int _processId;
        private readonly ThreadInfo _threadInfo;
        private bool? _priorityBoostEnabled;
        private ThreadPriorityLevel? _priorityLevel;

        /// <devdoc>
        ///     Internal constructor.
        /// </devdoc>
        /// <internalonly/>
        internal ProcessThread(bool isRemoteMachine, int processId, ThreadInfo threadInfo)
        {
            _isRemoteMachine = isRemoteMachine;
            _processId = processId;
            _threadInfo = threadInfo;
        }

        /// <devdoc>
        ///     Returns the base priority of the thread which is computed by combining the
        ///     process priority class with the priority level of the associated thread.
        /// </devdoc>
        public int BasePriority
        {
            get { return _threadInfo._basePriority; }
        }

        /// <devdoc>
        ///     The current priority indicates the actual priority of the associated thread,
        ///     which may deviate from the base priority based on how the OS is currently
        ///     scheduling the thread.
        /// </devdoc>
        public int CurrentPriority
        {
            get { return _threadInfo._currentPriority; }
        }

        /// <devdoc>
        ///     Returns the unique identifier for the associated thread.
        /// </devdoc>
        public int Id
        {
            get { return unchecked((int)_threadInfo._threadId); }
        }

        /// <summary>
        /// Returns or sets whether this thread would like a priority boost if the user interacts
        /// with user interface associated with this thread.
        /// </summary>
        public bool PriorityBoostEnabled
        {
            get
            {
                if (!_priorityBoostEnabled.HasValue)
                {
                    _priorityBoostEnabled = PriorityBoostEnabledCore;
                }
                return _priorityBoostEnabled.Value;
            }
            set
            {
                PriorityBoostEnabledCore = value;
                _priorityBoostEnabled = value;
            }
        }

        /// <summary>
        /// Returns or sets the priority level of the associated thread.  The priority level is
        /// not an absolute level, but instead contributes to the actual thread priority by
        /// considering the priority class of the process.
        /// </summary>
        public ThreadPriorityLevel PriorityLevel
        {
            [SupportedOSPlatform("windows")]
            [SupportedOSPlatform("linux")]
            [SupportedOSPlatform("freebsd")]
            get
            {
                if (!_priorityLevel.HasValue)
                {
                    _priorityLevel = PriorityLevelCore;
                }
                return _priorityLevel.Value;
            }
            [SupportedOSPlatform("windows")]
            set
            {
                PriorityLevelCore = value;
                _priorityLevel = value;
            }
        }

        /// <devdoc>
        ///     Returns the memory address of the function that was called when the associated
        ///     thread was started.
        /// </devdoc>
        public IntPtr StartAddress
        {
            get { return _threadInfo._startAddress; }
        }

        /// <devdoc>
        ///     Returns the current state of the associated thread, e.g. is it running, waiting, etc.
        /// </devdoc>
        public ThreadState ThreadState
        {
            get { return _threadInfo._threadState; }
        }

        /// <devdoc>
        ///     Returns the reason the associated thread is waiting, if any.
        /// </devdoc>
        public ThreadWaitReason WaitReason
        {
            get
            {
                if (_threadInfo._threadState != ThreadState.Wait)
                {
                    throw new InvalidOperationException(SR.WaitReasonUnavailable);
                }
                return _threadInfo._threadWaitReason;
            }
        }

        /// <summary>Returns the time the associated thread was started.</summary>
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        public DateTime StartTime
        {
            get => GetStartTime();
        }

        /// <summary>Sets the processor that this thread would ideally like to run on.</summary>
        public int IdealProcessor { set { SetIdealProcessor(value); } }

        /// <summary>
        /// Resets the ideal processor so there is no ideal processor for this thread (e.g.
        /// any processor is ideal).
        /// </summary>
        public void ResetIdealProcessor() => ResetIdealProcessorCore();

        /// <summary>
        /// Sets which processors the associated thread is allowed to be scheduled to run on.
        /// Each processor is represented as a bit: bit 0 is processor one, bit 1 is processor
        /// two, etc.  For example, the value 1 means run on processor one, 2 means run on
        /// processor two, 3 means run on processor one or two.
        /// </summary>
        [SupportedOSPlatform("windows")]
        public IntPtr ProcessorAffinity { set { SetProcessorAffinity(value); } }

        /// <summary>
        /// Returns the amount of time the thread has spent running code inside the operating
        /// system core.
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan PrivilegedProcessorTime => GetPrivilegedProcessorTime();

        /// <summary>
        /// Returns the amount of time the associated thread has spent utilizing the CPU.
        /// It is the sum of the System.Diagnostics.ProcessThread.UserProcessorTime and
        /// System.Diagnostics.ProcessThread.PrivilegedProcessorTime.
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan TotalProcessorTime => GetTotalProcessorTime();

        /// <devdoc>
        ///     Helper to check preconditions for property access.
        /// </devdoc>
        /// <summary>
        /// Returns the amount of time the associated thread has spent running code
        /// inside the application (not the operating system core).
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan UserProcessorTime => GetUserProcessorTime();
        private void EnsureState(State state)
        {
            if (((state & State.IsLocal) != (State)0) && _isRemoteMachine)
            {
                throw new NotSupportedException(SR.NotSupportedRemoteThread);
            }
        }

        /// <summary>
        ///      Preconditions for accessing properties.
        /// </summary>
        /// <internalonly/>
        private enum State
        {
            IsLocal = 0x2
        }
    }
}
