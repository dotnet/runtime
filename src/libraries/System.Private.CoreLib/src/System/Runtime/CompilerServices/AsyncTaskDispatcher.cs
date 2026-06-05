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

        public static bool AsyncProfilerInstrumentCheckPoint
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => InstrumentCheckPoint && AsyncInstrumentation.IsEnabled.AsyncProfiler(AsyncInstrumentation.SyncActiveFlags());
        }

        internal static unsafe AsyncTaskDispatcher? GetActiveDispatcher()
        {
            AsyncTaskDispatcherInfo* current = AsyncTaskDispatcherInfo.t_current;
            if (current != null && current->Dispatcher is AsyncTaskDispatcher activeDispatcher)
            {
                // V1 dispatchers emit only Resume/Complete per MoveNext invocation — no Suspend
                // events. Each dispatcher MoveNext is treated as a discrete unit. When a child
                // wrapper is created here (parent box yielded), the parent's MoveNext will simply
                // emit Complete on return; the child's lifecycle (Resume + Complete) fires later
                // when its continuation runs. The logical context spans multiple dispatchers via
                // shared contextId, with Resume count == Complete count as the balance invariant.
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

        internal Task? LastContinuation;

        internal bool ReachedLastContinuation;

        internal AsyncTaskDispatcher(IAsyncStateMachineBox inner) : base()
        {
            _inner = inner;
            _contextId = 0;
        }

        internal AsyncTaskDispatcher(IAsyncStateMachineBox inner, AsyncTaskDispatcher parent) : base()
        {
            _inner = inner;
            _contextId = parent.ContextId;
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

        internal bool ContinuationChainChanged => LastContinuation?.ContinuationForDiagnostics != null;

        internal static unsafe AsyncTaskDispatcher Create(IAsyncStateMachineBox box)
        {
            AsyncTaskDispatcherInfo* activeInfo = AsyncTaskDispatcherInfo.t_current;
            AsyncTaskDispatcher? activeDispatcher = (activeInfo != null && activeInfo->Dispatcher is AsyncTaskDispatcher d) ? d : null;
            AsyncTaskDispatcher dispatcher = activeDispatcher != null
                ? new AsyncTaskDispatcher(box, activeDispatcher)
                : new AsyncTaskDispatcher(box);

            AsyncInstrumentation.Flags flags = AsyncInstrumentation.ActiveFlags;
            if (AsyncInstrumentation.IsEnabled.CreateAsyncContext(flags) || AsyncInstrumentation.IsEnabled.ResumeAsyncContext(flags))
            {
                if (activeDispatcher is not null)
                {
                    AsyncProfiler.CreateAsyncContext.Create(activeDispatcher, ref activeInfo->AsyncProfilerInfo, (ulong)dispatcher.ContextId);
                }
                else
                {
                    AsyncProfiler.CreateAsyncContext.Create((ulong)dispatcher.ContextId);
                }
            }

            return dispatcher;
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
                if (AsyncInstrumentation.IsEnabled.CompleteAsyncContext(flags))
                {
                    AsyncProfiler.CompleteAsyncContext.Complete(this, ref dispatcherInfo.AsyncProfilerInfo);
                }

                refCurrent = dispatcherInfo.Next;
            }
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
