// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

#if NATIVEAOT
using Internal.Runtime;
#endif

namespace System.Runtime.CompilerServices
{
    internal struct ExecutionAndSyncBlockStore
    {
        // Store current ExecutionContext and SynchronizationContext as "previousXxx".
        // This allows us to restore them and undo any Context changes made in stateMachine.MoveNext
        // so that they won't "leak" out of the first await.
        public ExecutionContext? _previousExecutionCtx;
        public SynchronizationContext? _previousSyncCtx;
        public Thread _thread;

        public void Push()
        {
            _thread = Thread.CurrentThread;
            // Here we get the execution context for synchronous restoring,
            // not for flowing across suspension to potentially another thread.
            // Therefore we do not need to worry about IsFlowSuppressed
            _previousExecutionCtx = _thread._executionContext;
            _previousSyncCtx = _thread._synchronizationContext;
        }

        public void Pop()
        {
            // The common case is that these have not changed, so avoid the cost of a write barrier if not needed.
            if (_previousSyncCtx != _thread._synchronizationContext)
            {
                // Restore changed SynchronizationContext back to previous
                _thread._synchronizationContext = _previousSyncCtx;
            }

            ExecutionContext? currentExecutionCtx = _thread._executionContext;
            if (_previousExecutionCtx != currentExecutionCtx)
            {
                ExecutionContext.RestoreChangedContextToThread(_thread, _previousExecutionCtx, currentExecutionCtx);
            }
        }
    }

    [Flags]
    // Keep in sync with CORINFO_CONTINUATION_FLAGS
    internal enum ContinuationFlags
    {
        // Note: the following 'Has' members determine the members present at
        // the beginning of the continuation's data chunk. Each field is
        // pointer sized when present, apart from the result that has variable
        // size.

        // Whether or not the continuation starts with an OSR IL offset.
        HasOsrILOffset = 1,
        // If this bit is set the continuation resumes inside a try block and
        // thus if an exception is being propagated, needs to be resumed.
        HasException = 2,
        // If this bit is set the continuation has space for a continuation
        // context.
        HasContinuationContext = 4,
        // If this bit is set the continuation has space to store a result
        // returned by the callee.
        HasResult = 8,
        // If this bit is set the continuation should continue on the thread
        // pool.
        ContinueOnThreadPool = 16,
        // If this bit is set the continuation context is a
        // SynchronizationContext that we should continue on.
        ContinueOnCapturedSynchronizationContext = 32,
        // If this bit is set the continuation context is a TaskScheduler that
        // we should continue on.
        ContinueOnCapturedTaskScheduler = 64,
    }

    // Keep in sync with CORINFO_AsyncResumeInfo in corinfo.h
    internal unsafe struct ResumeInfo
    {
        public delegate*<Continuation, ref byte, Continuation?> Resume;
        // IP to use for diagnostics. Points into the jitted suspension code.
        // For debug codegen the IP resolves via an ASYNC native->IL mapping to
        // the IL AsyncHelpers.Await (or other async function) call which
        // caused the suspension.
        // For optimized codegen the mapping into the root method may be more
        // approximate (e.g. because of inlining).
        // For all codegens the offset of DiagnosticsIP matches
        // DiagnosticNativeOffset for the corresponding AsyncSuspensionPoint in
        // the debug info.
        public void* DiagnosticIP;
    }

#pragma warning disable CA1852 // "Type can be sealed" -- no it cannot because the runtime constructs subtypes dynamically
    internal unsafe class Continuation
    {
        public Continuation? Next;
        public ResumeInfo* ResumeInfo;
        public ContinuationFlags Flags;
        public int State;

#if TARGET_64BIT
        private const int PointerSize = 8;
#else
        private const int PointerSize = 4;
#endif

        private const int DataOffset = PointerSize /* Next */ + PointerSize /* Resume */ + 8 /* Flags + State */;

        public unsafe object GetContinuationContext()
        {
            Debug.Assert((Flags & ContinuationFlags.HasContinuationContext) != 0);
            uint contIndex = (uint)BitOperations.PopCount((uint)Flags & ((uint)ContinuationFlags.HasContinuationContext - 1));
            ref byte data = ref RuntimeHelpers.GetRawData(this);
            return Unsafe.As<byte, object>(ref Unsafe.Add(ref data, DataOffset + contIndex * PointerSize));
        }

        public void SetException(Exception ex)
        {
            Debug.Assert((Flags & ContinuationFlags.HasException) != 0);
            uint contIndex = (uint)BitOperations.PopCount((uint)Flags & ((uint)ContinuationFlags.HasException - 1));
            ref byte data = ref RuntimeHelpers.GetRawData(this);
            Unsafe.As<byte, Exception>(ref Unsafe.Add(ref data, DataOffset + contIndex * PointerSize)) = ex;
        }

        public ref byte GetResultStorageOrNull()
        {
            if ((Flags & ContinuationFlags.HasResult) == 0)
                return ref Unsafe.NullRef<byte>();

            uint contIndex = (uint)BitOperations.PopCount((uint)Flags & ((uint)ContinuationFlags.HasResult - 1));
            ref byte data = ref RuntimeHelpers.GetRawData(this);
            return ref Unsafe.Add(ref data, DataOffset + contIndex * PointerSize);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal unsafe ref struct AsyncDispatcherInfo
    {
        // Dispatcher info for next dispatcher present on stack, or
        // null if none.
        [FieldOffset(0)]
        public AsyncDispatcherInfo* Next;

        // Next continuation the dispatcher will process.
#if TARGET_64BIT
        [FieldOffset(8)]
#else
        [FieldOffset(4)]
#endif
        public Continuation? NextContinuation;

        // Information about current task dispatching, to be used for async
        // stackwalking.
        [ThreadStatic]
        internal static unsafe AsyncDispatcherInfo* t_current; // Debugger depends on the exact name of this field.
    }

    public static partial class AsyncHelpers
    {
#if FEATURE_INTERPRETER
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "AsyncHelpers_ResumeInterpreterContinuation")]
        private static partial void AsyncHelpers_ResumeInterpreterContinuation(ObjectHandleOnStack cont, ref byte resultStorage);

        internal static Continuation? ResumeInterpreterContinuation(Continuation cont, ref byte resultStorage)
        {
            ObjectHandleOnStack contHandle = ObjectHandleOnStack.Create(ref cont);
            AsyncHelpers_ResumeInterpreterContinuation(contHandle, ref resultStorage);
            return cont;
        }
#endif

        // This is the "magic" method on which other "Await" methods are built.
        // Calling this from an Async method returns the continuation to the caller thus
        // explicitly initiates suspension.
        [Intrinsic]
        private static void AsyncSuspend(Continuation continuation) => throw new UnreachableException();

        // An intrinsic that provides access to continuations produced by Async calls.
        // Calling this after an Async method call returns:
        //   * `null` if the call has completed synchronously, or
        //   * a continuation object if the call requires suspension.
        //     In this case the formal result of the call is undefined.
        [Intrinsic]
        private static Continuation? AsyncCallContinuation() => throw new UnreachableException();

        // Used during suspensions to hold the continuation chain and on what we are waiting.
        // Methods like FinalizeTaskReturningThunk will unlink the state and wrap into a Task.
        private struct RuntimeAsyncAwaitState
        {
            public Continuation? SentinelContinuation;

            // The following are the possible introducers of asynchrony into a chain of awaits.
            // In other words - when we build a chain of continuations it would be logicaly attached
            // to one of these notifiers.
            public ICriticalNotifyCompletion? CriticalNotifier;
            public INotifyCompletion? Notifier;
            public IValueTaskSourceNotifier? ValueTaskSourceNotifier;
            public Task? TaskNotifier;

            public ExecutionContext? ExecutionContext;
            public SynchronizationContext? SynchronizationContext;

            public void CaptureContexts()
            {
                Thread curThread = Thread.CurrentThreadAssumedInitialized;
                // Here we get the execution context for presenting to the notifier,
                // not for flowing across suspension to potentially another thread.
                // Therefore we do not need to worry about IsFlowSuppressed
                ExecutionContext = curThread._executionContext;
                SynchronizationContext = curThread._synchronizationContext;
            }
        }

        [ThreadStatic]
        private static RuntimeAsyncAwaitState t_runtimeAsyncAwaitState;

        private static unsafe Continuation AllocContinuation(Continuation prevContinuation, MethodTable* contMT)
        {
#if NATIVEAOT
            Continuation newContinuation = (Continuation)RuntimeImports.RhNewObject(contMT);
#else
            Continuation newContinuation = (Continuation)RuntimeTypeHandle.InternalAllocNoChecks(contMT);
#endif
            prevContinuation.Next = newContinuation;
            return newContinuation;
        }

#if !NATIVEAOT
        private static unsafe Continuation AllocContinuationMethod(Continuation prevContinuation, MethodTable* contMT, int keepAliveOffset, MethodDesc* method)
        {
            LoaderAllocator loaderAllocator = RuntimeMethodHandle.GetLoaderAllocator(new RuntimeMethodHandleInternal((IntPtr)method));
            Continuation newContinuation = (Continuation)RuntimeTypeHandle.InternalAllocNoChecks(contMT);
            Unsafe.As<byte, object?>(ref Unsafe.Add(ref RuntimeHelpers.GetRawData(newContinuation), keepAliveOffset)) = loaderAllocator;
            prevContinuation.Next = newContinuation;
            return newContinuation;
        }

        private static unsafe Continuation AllocContinuationClass(Continuation prevContinuation, MethodTable* contMT, int keepAliveOffset, MethodTable* methodTable)
        {
            IntPtr loaderAllocatorHandle = methodTable->GetLoaderAllocatorHandle();

            Continuation newContinuation = (Continuation)RuntimeTypeHandle.InternalAllocNoChecks(contMT);
            prevContinuation.Next = newContinuation;
            if (loaderAllocatorHandle != IntPtr.Zero)
            {
                Unsafe.As<byte, object?>(ref Unsafe.Add(ref RuntimeHelpers.GetRawData(newContinuation), keepAliveOffset)) = GCHandle.FromIntPtr(loaderAllocatorHandle).Target;
            }
            return newContinuation;
        }
#endif

        /// <summary>
        /// Used by internal thunks that implement awaiting on Task or a ValueTask.
        /// A ValueTask may wrap:
        /// - Completed result   (we never await this)
        /// - Task
        /// - ValueTaskSource
        /// Therefore, when we are awaiting a ValueTask completion we are really
        /// awaiting a completion of an underlying Task or ValueTaskSource.
        /// </summary>
        /// <param name="o"> Task or a ValueTaskNotifier whose completion we are awaiting.</param>
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.Async)]
        [RequiresPreviewFeatures]
        private static void TransparentAwait(object o)
        {
            ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
            Continuation? sentinelContinuation = state.SentinelContinuation;
            if (sentinelContinuation == null)
                state.SentinelContinuation = sentinelContinuation = new Continuation();

            if (o is Task t)
            {
                state.TaskNotifier = t;
            }
            else
            {
                state.ValueTaskSourceNotifier = (IValueTaskSourceNotifier)o;
            }

            state.CaptureContexts();
            AsyncSuspend(sentinelContinuation);
        }

        // Represents execution of a chain of suspended and resuming runtime
        // async functions.
        private sealed class RuntimeAsyncTask<T> : Task<T>, ITaskCompletionAction
        {
            public RuntimeAsyncTask()
            {
                // We use the base Task's state object field to store the Continuation while posting the task around.
                // Ensure that state object isn't published out for others to see.
                Debug.Assert((m_stateFlags & (int)InternalTaskOptions.PromiseTask) != 0, "Expected state flags to already be configured.");
                Debug.Assert(m_stateObject is null, "Expected to be able to use the state object field for Continuation.");
                m_action = DispatchContinuations;
                m_stateFlags |= (int)InternalTaskOptions.HiddenState;
            }

            internal override void ExecuteFromThreadPool(Thread threadPoolThread)
            {
                DispatchContinuations();
            }

            void ITaskCompletionAction.Invoke(Task completingTask)
            {
                DispatchContinuations();
            }

            bool ITaskCompletionAction.InvokeMayRunArbitraryCode => true;

            private Action GetContinuationAction() => (Action)m_action!;

            private Continuation MoveContinuationState()
            {
                Continuation continuation = (Continuation)m_stateObject!;
                m_stateObject = null;
                return continuation;
            }

            private void SetContinuationState(Continuation value)
            {
                Debug.Assert(m_stateObject == null);
                m_stateObject = value;
            }

            internal void HandleSuspended()
            {
                ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;

                RestoreContextsOnSuspension(false, state.ExecutionContext, state.SynchronizationContext);

                ICriticalNotifyCompletion? critNotifier = state.CriticalNotifier;
                INotifyCompletion? notifier = state.Notifier;
                IValueTaskSourceNotifier? vtsNotifier = state.ValueTaskSourceNotifier;
                Task? taskNotifier = state.TaskNotifier;

                state.CriticalNotifier = null;
                state.Notifier = null;
                state.ValueTaskSourceNotifier = null;
                state.TaskNotifier = null;
                state.ExecutionContext = null;
                state.SynchronizationContext = null;

                Continuation sentinelContinuation = state.SentinelContinuation!;
                Continuation headContinuation = sentinelContinuation.Next!;
                sentinelContinuation.Next = null;

                // Head continuation should be the result of async call to AwaitAwaiter or UnsafeAwaitAwaiter.
                // These never have special continuation context handling.
                const ContinuationFlags continueFlags =
                    ContinuationFlags.ContinueOnCapturedSynchronizationContext |
                    ContinuationFlags.ContinueOnThreadPool |
                    ContinuationFlags.ContinueOnCapturedTaskScheduler;

                Debug.Assert((headContinuation.Flags & continueFlags) == 0);

                SetContinuationState(headContinuation);

                try
                {
                    if (critNotifier != null)
                    {
                        critNotifier.UnsafeOnCompleted(GetContinuationAction());
                    }
                    else if (taskNotifier != null)
                    {
                        // Runtime async callable wrapper for task returning
                        // method. This implements the context transparent
                        // forwarding and makes these wrappers minimal cost.
                        if (!taskNotifier.TryAddCompletionAction(this))
                        {
                            ThreadPool.UnsafeQueueUserWorkItemInternal(this, preferLocal: true);
                        }
                    }
                    else if (vtsNotifier != null)
                    {
                        // The awaiter must inform the ValueTaskSource on whether the continuation
                        // wants to run on a context, although the source may decide to ignore the suggestion.
                        // Since the behavior of the source takes precedence, we clear the context flags of
                        // the awaiting continuation (so it will run transparently on what the source decides)
                        // and then tell the source if the awaiting frame prefers to continue on a context.
                        // The reason why we do it here and not when the notifier is created is because
                        // the continuation chain builds from the innermost frame out and at the time when the
                        // notifier is created we do not know yet if the caller wants to continue on a context.
                        ValueTaskSourceOnCompletedFlags configFlags = ValueTaskSourceOnCompletedFlags.None;

                        // Skip to a nontransparent/user continuation. Such continuaton must exist.
                        // Since we see a VTS notifier, something was directly or indirectly
                        // awaiting an async thunk for a ValueTask-returning method.
                        // That can only happen in nontransparent/user code.
                        Continuation nextUserContinuation = headContinuation.Next!;
                        while ((nextUserContinuation.Flags & continueFlags) == 0 && nextUserContinuation.Next != null)
                        {
                            nextUserContinuation = nextUserContinuation.Next;
                        }

                        ContinuationFlags continuationFlags = nextUserContinuation.Flags;
                        const ContinuationFlags continueOnContextFlags =
                            ContinuationFlags.ContinueOnCapturedSynchronizationContext |
                            ContinuationFlags.ContinueOnCapturedTaskScheduler;

                        if ((continuationFlags & continueOnContextFlags) != 0)
                        {
                            // if await has captured some context, inform the source
                            configFlags |= ValueTaskSourceOnCompletedFlags.UseSchedulingContext;
                        }

                        // Clear continuation flags, so that continuation runs transparently
                        nextUserContinuation.Flags &= ~continueFlags;
                        vtsNotifier.OnCompleted(s_runContinuationAction, this, configFlags);
                    }
                    else
                    {
                        Debug.Assert(notifier != null);
                        notifier.OnCompleted(GetContinuationAction());
                    }
                }
                catch (Exception ex)
                {
                    Task.ThrowAsync(ex, targetContext: null);
                }
            }

            private unsafe void DispatchContinuations()
            {
                ExecutionAndSyncBlockStore contexts = default;
                contexts.Push();

                AsyncDispatcherInfo asyncDispatcherInfo;
                asyncDispatcherInfo.Next = AsyncDispatcherInfo.t_current;
                asyncDispatcherInfo.NextContinuation = MoveContinuationState();
                AsyncDispatcherInfo.t_current = &asyncDispatcherInfo;

                while (true)
                {
                    Debug.Assert(asyncDispatcherInfo.NextContinuation != null);
                    try
                    {
                        Continuation curContinuation = asyncDispatcherInfo.NextContinuation;
                        Continuation? nextContinuation = curContinuation.Next;
                        asyncDispatcherInfo.NextContinuation = nextContinuation;

                        ref byte resultLoc = ref nextContinuation != null ? ref nextContinuation.GetResultStorageOrNull() : ref GetResultStorage();
                        Continuation? newContinuation = curContinuation.ResumeInfo->Resume(curContinuation, ref resultLoc);

                        if (newContinuation != null)
                        {
                            newContinuation.Next = nextContinuation;
                            HandleSuspended();
                            contexts.Pop();
                            AsyncDispatcherInfo.t_current = asyncDispatcherInfo.Next;
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Continuation? handlerContinuation = UnwindToPossibleHandler(asyncDispatcherInfo.NextContinuation);
                        if (handlerContinuation == null)
                        {
                            // Tail of AsyncTaskMethodBuilderT.SetException
                            bool successfullySet = ex is OperationCanceledException oce ?
                                TrySetCanceled(oce.CancellationToken, oce) :
                                TrySetException(ex);

                            contexts.Pop();

                            AsyncDispatcherInfo.t_current = asyncDispatcherInfo.Next;

                            if (!successfullySet)
                            {
                                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted);
                            }

                            return;
                        }

                        handlerContinuation.SetException(ex);
                        asyncDispatcherInfo.NextContinuation = handlerContinuation;
                    }

                    if (asyncDispatcherInfo.NextContinuation == null)
                    {
                        bool successfullySet = TrySetResult(m_result);

                        contexts.Pop();

                        AsyncDispatcherInfo.t_current = asyncDispatcherInfo.Next;

                        if (!successfullySet)
                        {
                            ThrowHelper.ThrowInvalidOperationException(ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted);
                        }

                        return;
                    }

                    if (QueueContinuationFollowUpActionIfNecessary(asyncDispatcherInfo.NextContinuation))
                    {
                        contexts.Pop();
                        AsyncDispatcherInfo.t_current = asyncDispatcherInfo.Next;
                        return;
                    }
                }
            }

            private ref byte GetResultStorage() => ref Unsafe.As<T?, byte>(ref m_result);

            private static Continuation? UnwindToPossibleHandler(Continuation? continuation)
            {
                while (true)
                {
                    if (continuation == null || (continuation.Flags & ContinuationFlags.HasException) != 0)
                        return continuation;

                    continuation = continuation.Next;
                }
            }

            private bool QueueContinuationFollowUpActionIfNecessary(Continuation continuation)
            {
                if ((continuation.Flags & ContinuationFlags.ContinueOnThreadPool) != 0)
                {
                    SynchronizationContext? ctx = Thread.CurrentThreadAssumedInitialized._synchronizationContext;
                    if (ctx == null || ctx.GetType() == typeof(SynchronizationContext))
                    {
                        TaskScheduler? sched = TaskScheduler.InternalCurrent;
                        if (sched == null || sched == TaskScheduler.Default)
                        {
                            // Can inline
                            return false;
                        }
                    }

                    SetContinuationState(continuation);
                    ThreadPool.UnsafeQueueUserWorkItemInternal(this, preferLocal: true);
                    return true;
                }

                if ((continuation.Flags & ContinuationFlags.ContinueOnCapturedSynchronizationContext) != 0)
                {
                    object continuationContext = continuation.GetContinuationContext();
                    Debug.Assert(continuationContext is SynchronizationContext { });
                    SynchronizationContext continuationSyncCtx = (SynchronizationContext)continuationContext;

                    if (continuationSyncCtx == Thread.CurrentThreadAssumedInitialized._synchronizationContext)
                    {
                        // Inline
                        return false;
                    }

                    SetContinuationState(continuation);

                    try
                    {
                        continuationSyncCtx.Post(s_postCallback, this);
                    }
                    catch (Exception ex)
                    {
                        Task.ThrowAsync(ex, targetContext: null);
                    }

                    return true;
                }

                if ((continuation.Flags & ContinuationFlags.ContinueOnCapturedTaskScheduler) != 0)
                {
                    object continuationContext = continuation.GetContinuationContext();
                    Debug.Assert(continuationContext is TaskScheduler { });
                    TaskScheduler sched = (TaskScheduler)continuationContext;

                    SetContinuationState(continuation);
                    // TODO: We do not need TaskSchedulerAwaitTaskContinuation here, just need to refactor its Run method...
                    var taskSchedCont = new TaskSchedulerAwaitTaskContinuation(sched, GetContinuationAction(), flowExecutionContext: false);
                    taskSchedCont.Run(Task.CompletedTask, canInlineContinuationTask: true);

                    return true;
                }

                return false;
            }

            private static readonly SendOrPostCallback s_postCallback = static state =>
            {
                Debug.Assert(state is RuntimeAsyncTask<T>);
                ((RuntimeAsyncTask<T>)state).DispatchContinuations();
            };

            private static readonly Action<object?> s_runContinuationAction = static state =>
            {
                Debug.Assert(state is RuntimeAsyncTask<T>);
                ((RuntimeAsyncTask<T>)state).DispatchContinuations();
            };
        }

        // Change return type to RuntimeAsyncTask<T?> -- no benefit since this is used for Task returning thunks only
#pragma warning disable CA1859
        // When a Task-returning thunk gets a continuation result
        // it calls here to make a Task that awaits on the current async state.
        private static Task<T?> FinalizeTaskReturningThunk<T>()
        {
            RuntimeAsyncTask<T?> result = new();
            result.HandleSuspended();
            return result;
        }

        private static Task FinalizeTaskReturningThunk()
        {
            RuntimeAsyncTask<VoidTaskResult> result = new();
            result.HandleSuspended();
            return result;
        }

        private static ValueTask<T?> FinalizeValueTaskReturningThunk<T>()
        {
            // We only come to these methods in the expensive case (already
            // suspended), so ValueTask optimization here is not relevant.
            return new ValueTask<T?>(FinalizeTaskReturningThunk<T>());
        }

        private static ValueTask FinalizeValueTaskReturningThunk()
        {
            return new ValueTask(FinalizeTaskReturningThunk());
        }

        private static Task<T?> TaskFromException<T>(Exception ex)
        {
            Task<T?> task = new();
            bool successfullySet = ex is OperationCanceledException oce ?
                task.TrySetCanceled(oce.CancellationToken, oce) :
                task.TrySetException(ex);

            Debug.Assert(successfullySet);
            return task;
        }

        private static Task TaskFromException(Exception ex)
        {
            Task task = new();
            // Tail of AsyncTaskMethodBuilderT.SetException
            bool successfullySet = ex is OperationCanceledException oce ?
                task.TrySetCanceled(oce.CancellationToken, oce) :
                task.TrySetException(ex);

            Debug.Assert(successfullySet);
            return task;
        }

        private static ValueTask ValueTaskFromException(Exception ex)
        {
            // We only come to these methods in the expensive case (exception),
            // so ValueTask optimization here is not relevant.
            return new ValueTask(TaskFromException(ex));
        }

        private static ValueTask<T?> ValueTaskFromException<T>(Exception ex)
        {
            return new ValueTask<T?>(TaskFromException<T>(ex));
        }

        // Called when capturing execution context for suspension.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ExecutionContext? CaptureExecutionContext()
        {
            return ExecutionContext.CaptureForSuspension(Thread.CurrentThreadAssumedInitialized);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RestoreExecutionContext(ExecutionContext? previousExecCtx)
        {
            if (previousExecCtx == ExecutionContext.DefaultFlowSuppressed)
            {
                return;
            }

            Thread thread = Thread.CurrentThreadAssumedInitialized;
            ExecutionContext? currentExecCtx = thread._executionContext;
            if (previousExecCtx != currentExecCtx)
            {
                ExecutionContext.RestoreChangedContextToThread(thread, previousExecCtx, currentExecCtx);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CaptureContexts(out ExecutionContext? execCtx, out SynchronizationContext? syncCtx)
        {
            Thread thread = Thread.CurrentThreadAssumedInitialized;
            // Here we get the execution context for synchronous restoring,
            // not for flowing across suspension to potentially another thread.
            // Therefore we do not need to worry about IsFlowSuppressed
            execCtx = thread._executionContext;
            syncCtx = thread._synchronizationContext;
        }

        // Restore contexts onto current Thread. If "resumed" then this is not the first starting call for the async method.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RestoreContexts(bool resumed, ExecutionContext? previousExecCtx, SynchronizationContext? previousSyncCtx)
        {
            if (!resumed)
            {
                Thread thread = Thread.CurrentThreadAssumedInitialized;
                if (previousSyncCtx != thread._synchronizationContext)
                {
                    thread._synchronizationContext = previousSyncCtx;
                }

                ExecutionContext? currentExecCtx = thread._executionContext;
                if (previousExecCtx != currentExecCtx)
                {
                    ExecutionContext.RestoreChangedContextToThread(thread, previousExecCtx, currentExecCtx);
                }
            }
        }

        // Restore contexts onto current Thread as we unwind during suspension. We control the code that runs
        // during suspension and we do not need to raise ExecutionContext notifications -- we know that it is
        // not going to be accessed and that DispatchContinuations will return it back to the leaf's context
        // before calling user code, and restore the original contexts with appropriate notifications before
        // returning.
        private static void RestoreContextsOnSuspension(bool resumed, ExecutionContext? previousExecCtx, SynchronizationContext? previousSyncCtx)
        {
            if (!resumed)
            {
                Thread thread = Thread.CurrentThreadAssumedInitialized;
                if (previousSyncCtx != thread._synchronizationContext)
                {
                    thread._synchronizationContext = previousSyncCtx;
                }

                if (previousExecCtx != thread._executionContext)
                {
                    thread._executionContext = previousExecCtx;
                }
            }
        }

        private static void CaptureContinuationContext(ref object continuationContext, ref ContinuationFlags flags)
        {
            SynchronizationContext? syncCtx = Thread.CurrentThreadAssumedInitialized._synchronizationContext;
            if (syncCtx != null && syncCtx.GetType() != typeof(SynchronizationContext))
            {
                flags |= ContinuationFlags.ContinueOnCapturedSynchronizationContext;
                continuationContext = syncCtx;
                return;
            }

            TaskScheduler? sched = TaskScheduler.InternalCurrent;
            if (sched != null && sched != TaskScheduler.Default)
            {
                flags |= ContinuationFlags.ContinueOnCapturedTaskScheduler;
                continuationContext = sched;
                return;
            }

            flags |= ContinuationFlags.ContinueOnThreadPool;
        }

        internal static T CompletedTaskResult<T>(Task<T> task)
        {
            TaskAwaiter.ValidateEnd(task);
            return task.ResultOnSuccess;
        }

        internal static void CompletedTask(Task task)
        {
            TaskAwaiter.ValidateEnd(task);
        }
    }
}
