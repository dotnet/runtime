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
        public Task? Dispatcher;

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

        internal object? NextContinuationForDiagnostics
        {
            get
            {
                IAsyncStateMachineBox? last = AsyncProfilerInfo.LastContinuation;
                if (last is Task task)
                {
                    return task.ContinuationForDiagnostics;
                }

                return last is not null && last.GetDiagnosticData(out _, out _, out object? next) ? next : null;
            }
        }

        internal bool ContinuationChainChanged => NextContinuationForDiagnostics != null;

        internal static unsafe IAsyncStateMachineBox CreateDispatcher(IAsyncStateMachineBox box, AsyncInstrumentation.Flags flags)
        {
            if (!IsSupported)
            {
                return box;
            }

            IAsyncStateMachineDispatcher? dispatcherBox = box as IAsyncStateMachineDispatcher;

            if (dispatcherBox?.IsLeaf == true)
            {
                return box;
            }

            AsyncStateMachineDispatcherInfo* info = AsyncStateMachineDispatcherInfo.t_current;
            Task? activeDispatcher = info != null ? info->Dispatcher : null;

            if (activeDispatcher is AsyncStateMachineDispatcher reusedDispatcher)
            {
                if (ReferenceEquals(reusedDispatcher.InnerBox, box))
                {
                    if (AsyncInstrumentation.IsEnabled.ResumeAsyncContext(flags))
                    {
                        AsyncProfiler.CreateAsyncContext.Append(ref *info);
                    }

                    info->AsyncProfilerInfo.CurrentContinuationResumes = true;
                    return reusedDispatcher;
                }
            }
            else if (ReferenceEquals(activeDispatcher, box))
            {
                if (AsyncInstrumentation.IsEnabled.ResumeAsyncContext(flags))
                {
                    AsyncProfiler.CreateAsyncContext.Append(ref *info);
                }

                Debug.Assert(dispatcherBox != null);
                dispatcherBox!.IsLeaf = true;

                info->AsyncProfilerInfo.CurrentContinuationResumes = true;
                return box;
            }

            if (dispatcherBox is Task)
            {
                EmitCreateAsyncContext(info, dispatcherBox, flags);
                dispatcherBox.IsLeaf = true;
                return box;
            }

            AsyncStateMachineDispatcher dispatcher = new AsyncStateMachineDispatcher(box);
            EmitCreateAsyncContext(info, dispatcher, flags);
            return dispatcher;
        }

        private static unsafe void EmitCreateAsyncContext(AsyncStateMachineDispatcherInfo* info, IAsyncStateMachineDispatcher dispatcher, AsyncInstrumentation.Flags flags)
        {
            if (AsyncInstrumentation.IsEnabled.CreateAsyncContext(flags) || AsyncInstrumentation.IsEnabled.ResumeAsyncContext(flags))
            {
                if (info != null)
                {
                    AsyncProfiler.CreateAsyncContext.Create(ref *info, dispatcher);
                }
                else
                {
                    AsyncProfiler.CreateAsyncContext.Create(dispatcher);
                }
            }
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
            Task? activeDispatcher = info != null ? info->Dispatcher : null;
            if (activeDispatcher == null)
            {
                return;
            }

            info->AsyncProfilerInfo.CurrentContinuation = box;
            info->AsyncProfilerInfo.CurrentContinuationCompleted = false;

            AsyncProfiler.SyncPoint.Check(ref info->AsyncProfilerInfo);

            bool methodEventEnabled = AsyncInstrumentation.IsEnabled.ResumeAsyncMethod(flags);
            bool callstackEnabled = AsyncInstrumentation.IsEnabled.ResumeAsyncContext(flags);
            if (!methodEventEnabled && !(callstackEnabled && info->AsyncProfilerInfo.LastContinuation != null))
            {
                return;
            }

            ResumeAsyncMethod(info, box, methodEventEnabled, callstackEnabled);
        }

        private static unsafe void ResumeAsyncMethod(AsyncStateMachineDispatcherInfo* info, IAsyncStateMachineBox box, bool methodEventEnabled, bool callstackEnabled)
        {
            bool callstackEventEnabled = callstackEnabled && info->AsyncProfilerInfo.ReachedLastContinuation;

            if (!info->AsyncProfilerInfo.ReachedLastContinuation && ReferenceEquals(info->AsyncProfilerInfo.LastContinuation, box))
            {
                info->AsyncProfilerInfo.ReachedLastContinuation = true;
            }

            if (methodEventEnabled || callstackEventEnabled)
            {
                AsyncProfiler.ResumeAsyncMethod.Resume(ref *info, box);
            }
        }

        internal static bool SuspendOrCompleteContext(ref AsyncStateMachineDispatcherInfo info, AsyncInstrumentation.Flags flags)
        {
            bool suspended = false;

            try
            {
                // A node ends this dispatch in a suspend only when the method has not completed and
                // it re-armed itself as a leaf (it will be resumed again under this same node).
                // Otherwise the node is done: the method completed, or leaf-ship was handed off to a
                // child context that took over the chain (this node won't be resumed again).
                suspended = !info.AsyncProfilerInfo.CurrentContinuationCompleted && info.AsyncProfilerInfo.CurrentContinuationResumes;
                if (suspended)
                {
                    if (AsyncInstrumentation.IsEnabled.SuspendAsyncContext(flags))
                    {
                        AsyncProfiler.SuspendAsyncContext.Suspend(ref info.AsyncProfilerInfo);
                    }
                }
                else
                {
                    if (AsyncInstrumentation.IsEnabled.CompleteAsyncContext(flags))
                    {
                        AsyncProfiler.CompleteAsyncContext.Complete(ref info);
                    }
                }
            }
            catch (Exception)
            {
                // Best-effort instrumentation: swallow so the dispatch frame is always popped.
            }

            return suspended;
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

    internal interface IAsyncStateMachineDispatcher
    {
        bool IsLeaf { get; set; }
        ulong DispatcherId { get; }
    }

    internal sealed class AsyncStateMachineDispatcher : Task<VoidTaskResult>, IAsyncStateMachineBox, IAsyncStateMachineDispatcher
    {
        private IAsyncStateMachineBox? _inner;

        internal IAsyncStateMachineBox? InnerBox => _inner;

        internal AsyncStateMachineDispatcher(IAsyncStateMachineBox inner) : base()
        {
            _inner = inner;
        }

        // The wrapper is always the leaf dispatcher for its inner box, so this is permanently true;
        bool IAsyncStateMachineDispatcher.IsLeaf
        {
            get => true;
            set { }
        }

        ulong IAsyncStateMachineDispatcher.DispatcherId => (ulong)Id;

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
            info.AsyncProfilerInfo.DispatcherId = (ulong)Id;
            info.AsyncProfilerInfo.CurrentContinuation = inner;

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
                AsyncStateMachineDispatcherInfo.SuspendOrCompleteContext(ref info, flags);
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
