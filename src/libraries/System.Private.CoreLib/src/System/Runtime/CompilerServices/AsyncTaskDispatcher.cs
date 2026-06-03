// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.CompilerServices.AsyncInstrumentation;
using static System.Runtime.CompilerServices.AsyncProfiler;

namespace System.Runtime.CompilerServices
{
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe ref struct AsyncTaskDispatcherInfo
    {
        [FieldOffset(0)]
        public AsyncTaskDispatcherInfo* Next;

#if TARGET_64BIT
        [FieldOffset(8)]
#else
        [FieldOffset(4)]
#endif
        public AsyncTaskDispatcher? Dispatcher;

#if TARGET_64BIT
        [FieldOffset(16)]
#else
        [FieldOffset(8)]
#endif
        public AsyncProfiler.Info AsyncProfilerInfo;

        [ThreadStatic]
        internal static unsafe AsyncTaskDispatcherInfo* t_current;

        public static bool InstrumentCheckPoint
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => AsyncInstrumentation.IsSupported && AsyncInstrumentation.ActiveFlags != AsyncInstrumentation.Flags.Disabled;
        }

        internal static bool IsSuspended => t_current != null && t_current->Dispatcher is { Suspended: true };

        internal static unsafe AsyncTaskDispatcher? SuspendAsyncContext(AsyncInstrumentation.Flags flags)
        {
            AsyncTaskDispatcherInfo* current = AsyncTaskDispatcherInfo.t_current;
            if (current != null && current->Dispatcher is AsyncTaskDispatcher activeDispatcher)
            {
                Debug.Assert(!activeDispatcher.Suspended);
                activeDispatcher.Suspended = true;

                if (AsyncInstrumentation.IsEnabled.SuspendAsyncContext(flags))
                {
                    AsyncProfiler.SuspendAsyncContext.Suspend(activeDispatcher, ref current->AsyncProfilerInfo);
                }

                return activeDispatcher;
            }

            return null;
        }

        internal static unsafe void UnwindAsyncFrame()
        {
            AsyncTaskDispatcherInfo* current = t_current;
            if (current != null)
            {
                AsyncProfiler.AsyncMethodException.UnwindFrames(ref current->AsyncProfilerInfo, 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void TryFireResumeAsyncMethod(IAsyncStateMachineBox box, AsyncInstrumentation.Flags flags)
        {
            AsyncTaskDispatcherInfo* current = t_current;
            if (current == null || current->Dispatcher is not AsyncTaskDispatcher activeDispatcher)
            {
                return;
            }

            bool methodEventEnabled = AsyncInstrumentation.IsEnabled.ResumeAsyncMethod(flags);
            bool callstackEnabled = AsyncInstrumentation.IsEnabled.ResumeAsyncContext(flags);
            if (!methodEventEnabled && !(callstackEnabled && activeDispatcher.LastContinuation != null))
            {
                return;
            }

            ResumeAsyncMethod(activeDispatcher, current, box, methodEventEnabled, callstackEnabled);
        }

        private static unsafe void ResumeAsyncMethod(AsyncTaskDispatcher activeDispatcher, AsyncTaskDispatcherInfo* info, IAsyncStateMachineBox box, bool methodEventEnabled, bool callstackEnabled)
        {
            bool callstackEventEnabled = callstackEnabled && activeDispatcher.ReachedLastContinuation;

            if (!activeDispatcher.ReachedLastContinuation && ReferenceEquals(activeDispatcher.LastContinuation, box))
            {
                activeDispatcher.ReachedLastContinuation = true;
            }

            if (methodEventEnabled || callstackEventEnabled)
            {
                AsyncProfiler.ResumeAsyncMethod.Resume(activeDispatcher, box, ref info->AsyncProfilerInfo);
            }
        }

        internal static unsafe void CompleteAsyncMethod()
        {
            AsyncTaskDispatcherInfo* current = t_current;
            if (current != null)
            {
                AsyncProfiler.CompleteAsyncMethod.Complete(ref current->AsyncProfilerInfo);
            }
        }
    }

    internal sealed class AsyncTaskDispatcher : Task<VoidTaskResult>, IAsyncStateMachineBox
    {
        private IAsyncStateMachineBox? _inner;
        private Action? _moveNextAction;
        private ulong _contextId;

        internal IAsyncStateMachineBox? InnerBox => _inner;

        internal bool Suspended;

        internal Task? LastContinuation;

        internal bool ReachedLastContinuation;

        internal AsyncTaskDispatcher(IAsyncStateMachineBox inner) : base()
        {
            _inner = inner;
            _contextId = 0;
        }

        internal AsyncTaskDispatcher(IAsyncStateMachineBox inner, AsyncTaskDispatcher suspended) : base()
        {
            _inner = inner;
            _contextId = suspended.ContextId;
        }

        internal ulong ContextId
        {
            get
            {
                if (_contextId == 0)
                {
                    return (ulong)this.Id;
                }

                return _contextId;
            }
        }

        internal bool ContinuationChainChanged => LastContinuation?.DiagnosticContinuationObject != null;

        /// <summary>
        /// Creates a new dispatcher for the given box. If a dispatcher is already active on the
        /// current thread (mid-chain yield), marks the current frame as suspended and emit a suspend event.
        /// </summary>
        internal static AsyncTaskDispatcher Create(IAsyncStateMachineBox box)
        {
            AsyncInstrumentation.Flags flags = AsyncInstrumentation.SyncActiveFlags();
            AsyncTaskDispatcher? activeDispatcher = AsyncTaskDispatcherInfo.SuspendAsyncContext(flags);
            if (activeDispatcher != null)
            {
                return new AsyncTaskDispatcher(box, activeDispatcher);
            }

            AsyncTaskDispatcher newDispatcher = new AsyncTaskDispatcher(box);
            if (AsyncInstrumentation.IsEnabled.CreateAsyncContext(AsyncInstrumentation.SyncActiveFlags()))
            {
                AsyncProfiler.CreateAsyncContext.Create((ulong)newDispatcher.ContextId);
            }

            return newDispatcher;
        }

        internal sealed override void ExecuteDirectly(Thread? threadPoolThread) => MoveNext();

        public unsafe void MoveNext()
        {
            IAsyncStateMachineBox? inner = _inner;
            if (inner is null)
            {
                return;
            }

            AsyncTaskDispatcherInfo dispatcherInfo;
            ref AsyncTaskDispatcherInfo* refCurrent = ref AsyncTaskDispatcherInfo.t_current;
            AsyncTaskDispatcherInfo* previous = refCurrent;
            refCurrent = &dispatcherInfo;
            dispatcherInfo.Next = previous;

            dispatcherInfo.Dispatcher = this;

            AsyncInstrumentation.Flags flags = AsyncInstrumentation.SyncActiveFlags();
            AsyncProfiler.InitInfo(ref dispatcherInfo.AsyncProfilerInfo);

            if (AsyncInstrumentation.IsEnabled.ResumeAsyncContext(flags))
            {
                AsyncProfiler.ResumeAsyncContext.Resume(ref dispatcherInfo);
            }

            try
            {
                inner.MoveNext();
            }
            finally
            {
                if (!Suspended && AsyncInstrumentation.IsEnabled.CompleteAsyncContext(flags))
                {
                    AsyncProfiler.CompleteAsyncContext.Complete(this, ref dispatcherInfo.AsyncProfilerInfo);
                }

                // If Suspended, the Suspend event was already emitted inline by Create.
            }

            refCurrent = dispatcherInfo.Next;
        }

        public Action MoveNextAction => _moveNextAction ??= MoveNext;

        public IAsyncStateMachine GetStateMachineObject()
        {
            IAsyncStateMachineBox? inner = _inner;
            return inner is not null ? inner.GetStateMachineObject() : null!;
        }

        public void ClearStateUponCompletion()
        {
            _inner?.ClearStateUponCompletion();
            _inner = null;
        }

        public bool GetDiagnosticData(out ulong methodId, out int state, out object? nextContinuation)
        {
            IAsyncStateMachineBox? inner = _inner;
            if (inner != null)
            {
                return inner.GetDiagnosticData(out methodId, out state, out nextContinuation);
            }

            methodId = 0;
            state = -1;
            nextContinuation = null;
            return false;
        }
    }
}
