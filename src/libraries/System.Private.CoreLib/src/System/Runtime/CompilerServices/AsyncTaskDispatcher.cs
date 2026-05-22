// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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
        public IAsyncStateMachineBox? InnerBox;

#if TARGET_64BIT
        [FieldOffset(16)]
#else
        [FieldOffset(8)]
#endif
        public AsyncTaskDispatcher? Dispatcher;

#if TARGET_64BIT
        [FieldOffset(24)]
#else
        [FieldOffset(12)]
#endif
        public bool Suspended;

#if TARGET_64BIT
        [FieldOffset(32)]
#else
        [FieldOffset(16)]
#endif
        public AsyncProfiler.Info AsyncProfilerInfo;

        [ThreadStatic]
        internal static unsafe AsyncTaskDispatcherInfo* t_current;

        internal static bool IsSuspended => t_current != null && t_current->Suspended;

        internal static unsafe AsyncTaskDispatcher? SuspendAsyncContext()
        {
            AsyncTaskDispatcherInfo* info = AsyncTaskDispatcherInfo.t_current;
            if (info != null && info->Dispatcher is AsyncTaskDispatcher activeDispatcher)
            {
                Debug.Assert(!info->Suspended);
                info->Suspended = true;
                if (AsyncInstrumentation.IsEnabled.SuspendAsyncContext(AsyncInstrumentation.SyncActiveFlags()))
                {
                    AsyncProfiler.SuspendAsyncContext.Suspend(ref info->AsyncProfilerInfo);
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

        internal static unsafe void ResumeAsyncMethod()
        {
            AsyncTaskDispatcherInfo* current = t_current;
            if (current != null)
            {
                AsyncProfiler.ResumeAsyncMethod.Resume(ref current->AsyncProfilerInfo);
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

        /// <summary>
        /// Creates a new dispatcher for the given box. If a dispatcher is already active on the
        /// info thread (mid-chain yield), marks the info frame as suspended and emit a suspend event.
        /// </summary>
        internal static AsyncTaskDispatcher Create(IAsyncStateMachineBox box)
        {
            Debug.WriteLine($"[AsyncTaskDispatcher.Create] Thread={Environment.CurrentManagedThreadId}, box={box.GetType().Name}, tid={Environment.CurrentManagedThreadId}");

            AsyncTaskDispatcher? activeDispatcher = AsyncTaskDispatcherInfo.SuspendAsyncContext();
            if (activeDispatcher != null)
            {
                Debug.WriteLine($"[AsyncTaskDispatcher.MoveNext] Suspended Id={activeDispatcher.ContextId}, tid={Environment.CurrentManagedThreadId}");
                return new AsyncTaskDispatcher(box, activeDispatcher);
            }

            AsyncTaskDispatcher newDispatcher = new AsyncTaskDispatcher(box);
            Debug.WriteLine($"[AsyncTaskDispatcher.Create] New dispatcher Id={newDispatcher.ContextId}, tid={Environment.CurrentManagedThreadId}");
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

            Debug.WriteLine($"[AsyncTaskDispatcher.MoveNext] Thread={Environment.CurrentManagedThreadId}, Id={ContextId}, inner={inner.GetType().Name}, tid={Environment.CurrentManagedThreadId}");

            AsyncTaskDispatcherInfo dispatcherInfo;
            ref AsyncTaskDispatcherInfo* refCurrent = ref AsyncTaskDispatcherInfo.t_current;
            AsyncTaskDispatcherInfo* previous = refCurrent;
            refCurrent = &dispatcherInfo;
            dispatcherInfo.Next = previous;

            dispatcherInfo.InnerBox = inner;
            dispatcherInfo.Dispatcher = this;
            dispatcherInfo.Suspended = false;

            AsyncInstrumentation.Flags flags = AsyncInstrumentation.SyncActiveFlags();
            AsyncProfiler.InitInfo(ref dispatcherInfo.AsyncProfilerInfo);

            if (AsyncInstrumentation.IsEnabled.ResumeAsyncContext(flags))
            {
                Debug.WriteLine($"[AsyncTaskDispatcher.MoveNext] Resuming Id={ContextId}, tid={Environment.CurrentManagedThreadId}");
                AsyncProfiler.ResumeAsyncContext.Resume(ref dispatcherInfo);
            }

            try
            {
                inner.MoveNext();
            }
            finally
            {
                if (!dispatcherInfo.Suspended && AsyncInstrumentation.IsEnabled.CompleteAsyncContext(flags))
                {
                    Debug.WriteLine($"[AsyncTaskDispatcher.MoveNext] Completed Id={ContextId}, tid={Environment.CurrentManagedThreadId}");
                    AsyncProfiler.CompleteAsyncContext.Complete(ref dispatcherInfo.AsyncProfilerInfo);
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
    }
}
