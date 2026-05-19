// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Stack-allocated TLS frame for V1 (Task-based) async profiler dispatch.
    /// Mirrors <c>AsyncDispatcherInfo</c> used by V2 (RuntimeAsync) dispatch.
    /// Pushed/popped by <see cref="AsyncTaskDispatcher"/> during chain execution.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe ref struct AsyncTaskDispatcherInfo
    {
        /// <summary>Linked list pointer to the parent dispatcher info on the stack, or null if this is the outermost.</summary>
        [FieldOffset(0)]
        public AsyncTaskDispatcherInfo* Next;

        /// <summary>The inner async state machine box being dispatched. Used by debugger/SOS to identify the async method.</summary>
#if TARGET_64BIT
        [FieldOffset(8)]
#else
        [FieldOffset(4)]
#endif
        public IAsyncStateMachineBox? InnerBox;

        /// <summary>The dispatcher wrapping this chain. Used to identify the current dispatcher context.</summary>
#if TARGET_64BIT
        [FieldOffset(16)]
#else
        [FieldOffset(8)]
#endif
        public AsyncTaskDispatcher? Dispatcher;

        /// <summary>Set to true when a method yields during dispatch (Create fires inside active frame).</summary>
#if TARGET_64BIT
        [FieldOffset(24)]
#else
        [FieldOffset(12)]
#endif
        public bool Suspended;

        /// <summary>Set to true by SetException when an async method faults. Consumed by the next MoveNext to emit an unwind event.</summary>
#if TARGET_64BIT
        [FieldOffset(25)]
#else
        [FieldOffset(13)]
#endif
        public bool PendingUnwind;

        /// <summary>Async profiler bulk buffer and continuation wrapper state.</summary>
#if TARGET_64BIT
        [FieldOffset(32)]
#else
        [FieldOffset(16)]
#endif
        public AsyncProfiler.Info AsyncProfilerInfo;

        /// <summary>TLS linked list head for V1 async dispatch tracking.</summary>
        [ThreadStatic]
        internal static unsafe AsyncTaskDispatcherInfo* t_current;

        /// <summary>
        /// Marks a pending unwind on the current TLS frame. Called by SetException
        /// so the next MoveNext in the chain emits the unwind event between methods.
        /// </summary>
        internal static unsafe void MarkPendingUnwind()
        {
            AsyncTaskDispatcherInfo* current = t_current;
            if (current != null)
            {
                current->PendingUnwind = true;
            }
        }

        /// <summary>
        /// Checks and consumes a pending unwind on the current TLS frame.
        /// Called at the top of MoveNext before the state machine runs.
        /// </summary>
        /// <returns>True if an unwind was pending and has been consumed.</returns>
        internal static unsafe bool ConsumePendingUnwind()
        {
            AsyncTaskDispatcherInfo* current = t_current;
            if (current != null && current->PendingUnwind)
            {
                current->PendingUnwind = false;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks and consumes the Suspended flag on the current TLS frame.
        /// Called by CompleteAsyncMethod — if the frame was suspended but a method is completing,
        /// the chain has resumed execution past the yield point.
        /// </summary>
        /// <returns>True if the frame was suspended and has been cleared.</returns>
        internal static unsafe bool ConsumeSuspended()
        {
            AsyncTaskDispatcherInfo* current = t_current;
            if (current != null && current->Suspended)
            {
                current->Suspended = false;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if a dispatcher context is active on the current thread.
        /// Used to guard method-level events so they only emit inside a dispatch context.
        /// </summary>
        internal static unsafe bool IsActive => t_current != null;
    }

    /// <summary>
    /// V1 (Task-based) async profiler dispatch node. Wraps an <see cref="IAsyncStateMachineBox"/>
    /// to manage TLS context frame push/pop and emit context-level profiler events.
    /// Each yield point creates a new dispatcher; the chain is linked via Suspend/Resume events.
    /// </summary>
    /// <remarks>
    /// Injected at <c>UnsafeOnCompletedInternal</c> (when awaiting a non-async task)
    /// and at <c>YieldAwaiter.AwaitUnsafeOnCompleted</c> (yield is always a root).
    /// Only allocated when the async profiler is active. A new dispatcher is created
    /// per yield; each runs independently with no shared mutable state.
    /// </remarks>
    internal sealed class AsyncTaskDispatcher : Task<VoidTaskResult>, IAsyncStateMachineBox
    {
        private IAsyncStateMachineBox? _inner;
        private Action? _moveNextAction;
        private readonly bool _resumesFromSuspension;

        internal AsyncTaskDispatcher(IAsyncStateMachineBox inner, bool resumesFromSuspension = false) : base()
        {
            _inner = inner;
            _resumesFromSuspension = resumesFromSuspension;
        }

        /// <summary>
        /// Creates a new dispatcher for the given box. If a dispatcher is already active on the
        /// current thread (mid-chain yield), marks the current frame as suspended and emits
        /// a Suspend event. The new dispatcher is flagged to emit a Resume on its first PUSH.
        /// </summary>
        internal static unsafe AsyncTaskDispatcher Create(IAsyncStateMachineBox box)
        {
            AsyncTaskDispatcherInfo* current = AsyncTaskDispatcherInfo.t_current;
            if (current != null && current->Dispatcher is AsyncTaskDispatcher activeDispatcher)
            {
                // Chain is yielding — mark current frame and emit Suspend inline
                current->Suspended = true;
                Debug.WriteLine($"[AsyncProfiler:V1] SuspendAsyncContext dispatcher={activeDispatcher.Id} thread={Environment.CurrentManagedThreadId}");

                // New dispatcher continues the suspended chain
                var dispatcher = new AsyncTaskDispatcher(box, resumesFromSuspension: true);
                return dispatcher;
            }

            var newDispatcher = new AsyncTaskDispatcher(box);
            Debug.WriteLine($"[AsyncProfiler:V1] CreateAsyncContext dispatcher={newDispatcher.Id} innerBox={((Task)box).Id} thread={Environment.CurrentManagedThreadId}");
            return newDispatcher;
        }

        /// <summary>
        /// Creates a new dispatcher for a continuation being queued (not inlined).
        /// If a dispatcher is active, marks the frame as suspended and creates a new dispatcher
        /// that will resume the chain. Otherwise returns the box unchanged (no dispatcher needed).
        /// </summary>
        internal static unsafe IAsyncStateMachineBox ReuseOrPassthrough(IAsyncStateMachineBox box)
        {
            AsyncTaskDispatcherInfo* current = AsyncTaskDispatcherInfo.t_current;
            if (current != null && current->Dispatcher is AsyncTaskDispatcher activeDispatcher)
            {
                // Chain is yielding — mark current frame and emit Suspend inline
                current->Suspended = true;
                Debug.WriteLine($"[AsyncProfiler:V1] SuspendAsyncContext dispatcher={activeDispatcher.Id} thread={Environment.CurrentManagedThreadId}");

                // New dispatcher continues the suspended chain
                var dispatcher = new AsyncTaskDispatcher(box, resumesFromSuspension: true);
                return dispatcher;
            }

            return box;
        }

        internal sealed override void ExecuteFromThreadPool(Thread threadPoolThread)
        {
            MoveNext();
        }

        public void MoveNext()
        {
            IAsyncStateMachineBox? inner = _inner;
            if (inner is null)
                return;

            unsafe
            {
                AsyncTaskDispatcherInfo dispatcherInfo;
                ref AsyncTaskDispatcherInfo* refCurrent = ref AsyncTaskDispatcherInfo.t_current;
                AsyncTaskDispatcherInfo* previous = refCurrent;
                refCurrent = &dispatcherInfo;
                dispatcherInfo.Next = previous;
                dispatcherInfo.InnerBox = inner;
                dispatcherInfo.Dispatcher = this;
                dispatcherInfo.Suspended = false;
                AsyncProfiler.InitInfo(ref dispatcherInfo.AsyncProfilerInfo);

                if (_resumesFromSuspension)
                {
                    Debug.WriteLine($"[AsyncProfiler:V1] ResumeAsyncContext dispatcher={Id} innerBox={((Task)inner).Id} thread={Environment.CurrentManagedThreadId}");
                }
                else
                {
                    Debug.WriteLine($"[AsyncProfiler:V1] AsyncTaskDispatcher.MoveNext PUSH dispatcher={Id} innerBox={((Task)inner).Id} thread={Environment.CurrentManagedThreadId}");
                }

                try
                {
                    inner.MoveNext();
                }
                finally
                {
                    if (dispatcherInfo.PendingUnwind)
                    {
                        dispatcherInfo.PendingUnwind = false;
                        Debug.WriteLine($"[AsyncProfiler:V1] UnwindAsyncException (escaped) dispatcher={Id} innerBox={((Task)inner).Id} thread={Environment.CurrentManagedThreadId}");
                    }

                    if (!dispatcherInfo.Suspended)
                    {
                        Debug.WriteLine($"[AsyncProfiler:V1] CompleteAsyncContext dispatcher={Id} innerBox={((Task)inner).Id} thread={Environment.CurrentManagedThreadId}");
                    }
                    // If Suspended, the Suspend event was already emitted inline by Create

                    refCurrent = dispatcherInfo.Next;
                }
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
    }
}
