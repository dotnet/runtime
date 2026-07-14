// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe ref struct AsyncStateMachineDispatcherInfo
    {
        [FieldOffset(0)]
        public AsyncStateMachineDispatcherInfo* Next;

#if TARGET_64BIT
        [FieldOffset(8)]
#else
        [FieldOffset(4)]
#endif
        public AsyncStateMachineDispatcher? Dispatcher;

#if TARGET_64BIT
        [FieldOffset(16)]
#else
        [FieldOffset(8)]
#endif
        public AsyncProfiler.Info AsyncProfilerInfo;

        [ThreadStatic]
        internal static unsafe AsyncStateMachineDispatcherInfo* t_current;

        public static bool IsSupported
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NATIVEAOT
            // Asyncv1 instrumentation is disabled on Native AOT until support for
            // async callstack IP and state has been implemented in ILC. Native AOT
            // currently have limited asyncv1 diagnostic support in tooling, we can
            // postpone the support until proven needed.
            get => false;
#else
            get => AsyncInstrumentation.IsAsyncProfilerSupported;
#endif
        }

        internal static unsafe AsyncStateMachineDispatcher? GetActiveDispatcher()
        {
            if (!IsSupported)
            {
                return null;
            }

            AsyncStateMachineDispatcherInfo* info = AsyncStateMachineDispatcherInfo.t_current;
            return info != null ? info->Dispatcher : null;
        }

        internal static unsafe IAsyncStateMachineBox CreateDispatcher(IAsyncStateMachineBox box, AsyncInstrumentation.Flags flags)
        {
            if (!IsSupported)
            {
                return box;
            }

            if (box is AsyncStateMachineDispatcher)
            {
                return box;
            }

            AsyncStateMachineDispatcherInfo* info = AsyncStateMachineDispatcherInfo.t_current;
            AsyncStateMachineDispatcher? activeDispatcher = info != null ? info->Dispatcher : null;

            if (activeDispatcher != null && ReferenceEquals(activeDispatcher.InnerBox, box))
            {
                if (AsyncInstrumentation.IsEnabled.ResumeAsyncContext(flags))
                {
                    AsyncProfiler.CreateAsyncContext.Append(activeDispatcher, ref info->AsyncProfilerInfo);
                }

                return activeDispatcher;
            }

            AsyncStateMachineDispatcher dispatcher = new AsyncStateMachineDispatcher(box);

            if (AsyncInstrumentation.IsEnabled.CreateAsyncContext(flags) || AsyncInstrumentation.IsEnabled.ResumeAsyncContext(flags))
            {
                ulong parentDispatcherId = AsyncProfiler.DispatcherIds.CaptureParentDispatcherId();
                ulong dispatcherId = AsyncProfiler.DispatcherIds.GetDispatcherId(dispatcher);

                if (activeDispatcher != null)
                {
                    AsyncProfiler.CreateAsyncContext.Create(activeDispatcher, ref info->AsyncProfilerInfo, parentDispatcherId, dispatcherId);
                }
                else
                {
                    AsyncProfiler.CreateAsyncContext.Create(parentDispatcherId, dispatcherId);
                }
            }

            return dispatcher;
        }

        internal static unsafe void UnwindAsyncFrame(object completingBox, AsyncInstrumentation.Flags flags)
        {
            if (!IsSupported)
            {
                return;
            }

            AsyncStateMachineDispatcherInfo* info = t_current;
            if (info != null && ReferenceEquals(info->AsyncProfilerInfo.CurrentContinuation, completingBox))
            {
                info->AsyncProfilerInfo.CurrentContinuationCompleted = true;

                if (AsyncInstrumentation.IsEnabled.UnwindAsyncException(flags))
                {
                    AsyncProfiler.AsyncMethodException.UnwindFrames(ref *info, 1);
                }
            }
        }

        internal static unsafe void ResumeAsyncMethod(IAsyncStateMachineBox box, AsyncInstrumentation.Flags flags)
        {
            if (!IsSupported)
            {
                return;
            }

            AsyncStateMachineDispatcherInfo* info = t_current;
            AsyncStateMachineDispatcher? activeDispatcher = info != null ? info->Dispatcher : null;
            if (activeDispatcher == null)
            {
                return;
            }

            info->AsyncProfilerInfo.CurrentContinuation = box;
            info->AsyncProfilerInfo.CurrentContinuationCompleted = false;

            AsyncProfiler.SyncPoint.Check(ref info->AsyncProfilerInfo);

            bool methodEventEnabled = AsyncInstrumentation.IsEnabled.ResumeAsyncMethod(flags);
            bool callstackEnabled = AsyncInstrumentation.IsEnabled.ResumeAsyncContext(flags);
            if (!methodEventEnabled && !(callstackEnabled && activeDispatcher.LastContinuation != null))
            {
                return;
            }

            ResumeAsyncMethod(activeDispatcher, info, box, methodEventEnabled, callstackEnabled);
        }

        private static unsafe void ResumeAsyncMethod(AsyncStateMachineDispatcher activeDispatcher, AsyncStateMachineDispatcherInfo* info, IAsyncStateMachineBox box, bool methodEventEnabled, bool callstackEnabled)
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

        internal static unsafe void CompleteAsyncMethod(object completingBox, AsyncInstrumentation.Flags flags)
        {
            if (!IsSupported)
            {
                return;
            }

            AsyncStateMachineDispatcherInfo* info = t_current;
            if (info != null && ReferenceEquals(info->AsyncProfilerInfo.CurrentContinuation, completingBox))
            {
                info->AsyncProfilerInfo.CurrentContinuationCompleted = true;

                if (AsyncInstrumentation.IsEnabled.CompleteAsyncMethod(flags))
                {
                    AsyncProfiler.CompleteAsyncMethod.Complete(ref *info);
                }
            }
        }
    }

    internal sealed class AsyncStateMachineDispatcher : Task<VoidTaskResult>, IAsyncStateMachineBox
    {
        private IAsyncStateMachineBox? _inner;

        internal IAsyncStateMachineBox? InnerBox => _inner;

        internal IAsyncStateMachineBox? LastContinuation;

        internal bool ReachedLastContinuation;

        internal object? NextContinuationForDiagnostics
        {
            get
            {
                IAsyncStateMachineBox? last = LastContinuation;
                if (last is Task task)
                {
                    return task.ContinuationForDiagnostics;
                }

                return last is not null && last.GetDiagnosticData(out _, out _, out object? next) ? next : null;
            }
        }

        internal bool ContinuationChainChanged => NextContinuationForDiagnostics != null;

        internal AsyncStateMachineDispatcher(IAsyncStateMachineBox inner) : base()
        {
            _inner = inner;
        }

        internal sealed override void ExecuteDirectly(Thread? threadPoolThread) => MoveNext();

        public unsafe void MoveNext()
        {
            IAsyncStateMachineBox? inner = _inner;
            if (inner is null)
            {
                return;
            }

            AsyncStateMachineDispatcherInfo info;
            ref AsyncStateMachineDispatcherInfo* refInfo = ref AsyncStateMachineDispatcherInfo.t_current;
            AsyncStateMachineDispatcherInfo* refPreviousInfo = refInfo;
            refInfo = &info;
            info.Next = refPreviousInfo;

            AsyncProfiler.InitInfo(ref info.AsyncProfilerInfo);

            info.Dispatcher = this;
            info.AsyncProfilerInfo.CurrentContinuation = inner;

            LastContinuation = null;
            ReachedLastContinuation = false;

            try
            {
                InstrumentedMoveNext(ref info, inner);
            }
            finally
            {
                refInfo = info.Next;
            }
        }

        public Action MoveNextAction => (Action)(m_action ??= new Action(MoveNext));

        public IAsyncStateMachine GetStateMachineObject()
        {
            IAsyncStateMachineBox? inner = _inner;
            return inner != null ? inner.GetStateMachineObject() : null!;
        }

        public void ClearStateUponCompletion()
        {
            _inner?.ClearStateUponCompletion();
            _inner = null;

            LastContinuation = null;
            ReachedLastContinuation = false;
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

        private void InstrumentedMoveNext(ref AsyncStateMachineDispatcherInfo info, IAsyncStateMachineBox inner)
        {
            AsyncInstrumentation.Flags flags = AsyncInstrumentation.LoadFlags();
            try
            {
                if (AsyncInstrumentation.IsEnabled.ResumeAsyncContext(flags))
                {
                    AsyncProfiler.ResumeAsyncContext.Resume(ref info);
                }

                inner.MoveNext();
            }
            finally
            {
                bool isCompleted = info.AsyncProfilerInfo.CurrentContinuationCompleted;
                if (AsyncInstrumentation.IsEnabled.CompleteAsyncContext(flags) && isCompleted)
                {
                    AsyncProfiler.CompleteAsyncContext.Complete(this, ref info.AsyncProfilerInfo);
                }
                else if (AsyncInstrumentation.IsEnabled.SuspendAsyncContext(flags) && !isCompleted)
                {
                    AsyncProfiler.SuspendAsyncContext.Suspend(this, ref info.AsyncProfilerInfo);
                }
            }
        }
    }
}
