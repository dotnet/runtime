// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

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
            return current != null ? current->Dispatcher : null;
        }

        internal static unsafe void UnwindAsyncFrame()
        {
            AsyncTaskDispatcherInfo* current = t_current;
            if (current != null)
            {
                AsyncProfiler.AsyncMethodException.UnwindFrames(ref *current, 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void ResumeAsyncMethod(IAsyncStateMachineBox box, AsyncInstrumentation.Flags flags)
        {
            AsyncTaskDispatcherInfo* current = t_current;
            AsyncTaskDispatcher? activeDispatcher = current != null ? current->Dispatcher : null;
            if (activeDispatcher == null)
            {
                return;
            }

            activeDispatcher.CurrentContinuation = box;

            AsyncProfiler.SyncPoint.Check(ref current->AsyncProfilerInfo);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void CompleteAsyncMethod()
        {
            AsyncTaskDispatcherInfo* current = t_current;
            if (current != null)
            {
                AsyncProfiler.CompleteAsyncMethod.Complete(ref *current);
            }
        }
    }

    internal sealed class AsyncTaskDispatcher : Task<VoidTaskResult>, IAsyncStateMachineBox
    {
        private IAsyncStateMachineBox? _inner;
        private Action? _moveNextAction;

        internal IAsyncStateMachineBox? CurrentContinuation;

        internal Task? LastContinuation;

        internal bool ReachedLastContinuation;

        internal bool ContinuationChainChanged => LastContinuation?.ContinuationForDiagnostics != null;

        internal AsyncTaskDispatcher(IAsyncStateMachineBox inner) : base()
        {
            _inner = inner;
            CurrentContinuation = inner;
        }

        internal static unsafe AsyncTaskDispatcher Create(IAsyncStateMachineBox box)
        {
            if (box is AsyncTaskDispatcher existing)
            {
                return existing;
            }

            AsyncTaskDispatcherInfo* current = AsyncTaskDispatcherInfo.t_current;
            AsyncTaskDispatcher? activeDispatcher = current != null ? current->Dispatcher : null;

            AsyncTaskDispatcher dispatcher = new AsyncTaskDispatcher(box);

            AsyncInstrumentation.Flags flags = AsyncInstrumentation.ActiveFlags;
            if (AsyncInstrumentation.IsEnabled.CreateAsyncContext(flags) || AsyncInstrumentation.IsEnabled.ResumeAsyncContext(flags))
            {
                ulong parentDispatcherId = AsyncProfiler.DispatcherIds.CaptureParentDispatcherId();
                ulong dispatcherId = AsyncProfiler.DispatcherIds.GetDispatcherId(dispatcher);

                if (activeDispatcher != null)
                {
                    AsyncProfiler.CreateAsyncContext.Create(activeDispatcher, ref current->AsyncProfilerInfo, parentDispatcherId, dispatcherId);
                }
                else
                {
                    AsyncProfiler.CreateAsyncContext.Create(parentDispatcherId, dispatcherId);
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

                if (IsCompleted)
                {
                    CurrentContinuation = null;
                    LastContinuation = null;
                }
            }
        }

        public Action MoveNextAction => _moveNextAction ??= MoveNext;

        public IAsyncStateMachine GetStateMachineObject()
        {
            IAsyncStateMachineBox? inner = _inner;
            return inner != null ? inner.GetStateMachineObject() : null!;
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
