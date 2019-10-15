// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

namespace System.Diagnostics
{
    /// <devdoc>
    ///    <para>
    ///       Represents a Win32 thread. This can be used to obtain
    ///       information about the thread, such as it's performance characteristics. This is
    ///       returned from the System.Diagnostics.Process.ProcessThread property of the System.Diagnostics.Process component.
    ///    </para>
    /// </devdoc>
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

        /// <devdoc>
        ///      Returns or sets whether this thread would like a priority boost if the user interacts
        ///      with user interface associated with this thread.
        /// </devdoc>
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

        /// <devdoc>
        ///     Returns or sets the priority level of the associated thread.  The priority level is
        ///     not an absolute level, but instead contributes to the actual thread priority by
        ///     considering the priority class of the process.
        /// </devdoc>
        public ThreadPriorityLevel PriorityLevel
        {
            get
            {
                if (!_priorityLevel.HasValue)
                {
                    _priorityLevel = PriorityLevelCore;
                }
                return _priorityLevel.Value;
            }
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

        /// <devdoc>
        ///     Helper to check preconditions for property access.
        /// </devdoc>
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
