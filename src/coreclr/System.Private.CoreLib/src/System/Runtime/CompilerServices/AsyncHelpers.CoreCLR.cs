// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

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

#pragma warning disable CA1852 // "Type can be sealed" -- no it cannot because the runtime constructs subtypes dynamically
    internal unsafe class Continuation
    {
        public Continuation? Next;
        public delegate*<Continuation, ref byte, Continuation?> Resume;
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

    public static partial class AsyncHelpers
    {
        // This is the "magic" method on wich other "Await" methods are built.
        // Calling this from an Async method returns the continuation to the caller thus
        // explicitly initiates suspension.
        [Intrinsic]
        private static void AsyncSuspend(Continuation continuation) => throw new UnreachableException();

        // Used during suspensions to hold the continuation chain and on what we are waiting.
        // Methods like FinalizeTaskReturningThunk will unlink the state and wrap into a Task.
        private struct RuntimeAsyncAwaitState
        {
            public Continuation? SentinelContinuation;
            public ICriticalNotifyCompletion? CriticalNotifier;
            public INotifyCompletion? Notifier;
            public Task? CalledTask;
        }

        [ThreadStatic]
        private static RuntimeAsyncAwaitState t_runtimeAsyncAwaitState;

        private static unsafe Continuation AllocContinuation(Continuation prevContinuation, MethodTable* contMT)
        {
            Continuation newContinuation = (Continuation)RuntimeTypeHandle.InternalAllocNoChecks(contMT);
            prevContinuation.Next = newContinuation;
            return newContinuation;
        }

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

        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.Async)]
        [RequiresPreviewFeatures]
        private static void TransparentAwaitTask(Task t)
        {
            ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
            Continuation? sentinelContinuation = state.SentinelContinuation;
            if (sentinelContinuation == null)
                state.SentinelContinuation = sentinelContinuation = new Continuation();

            state.CalledTask = t;
            AsyncSuspend(sentinelContinuation);
        }

        private interface IRuntimeAsyncTaskOps<T>
        {
            static abstract Action GetContinuationAction(T task);
            static abstract Continuation GetContinuationState(T task);
            static abstract void SetContinuationState(T task, Continuation value);
            static abstract bool SetCompleted(T task);
            static abstract void PostToSyncContext(T task, SynchronizationContext syncCtx);
            static abstract ref byte GetResultStorage(T task);
        }

        /// <summary>
        /// Represents a wrapped runtime async operation.
        /// </summary>
        private sealed class RuntimeAsyncTask<T> : Task<T>, ITaskCompletionAction
        {
            public RuntimeAsyncTask()
            {
                // We use the base Task's state object field to store the Continuation while posting the task around.
                // Ensure that state object isn't published out for others to see.
                Debug.Assert((m_stateFlags & (int)InternalTaskOptions.PromiseTask) != 0, "Expected state flags to already be configured.");
                Debug.Assert(m_stateObject is null, "Expected to be able to use the state object field for Continuation.");
                m_action = MoveNext;
                m_stateFlags |= (int)InternalTaskOptions.HiddenState;
            }

            internal override void ExecuteFromThreadPool(Thread threadPoolThread)
            {
                MoveNext();
            }

            private void MoveNext()
            {
                RuntimeAsyncTaskCore.DispatchContinuations<RuntimeAsyncTask<T>, Ops>(this);
            }

            public void HandleSuspended()
            {
                RuntimeAsyncTaskCore.HandleSuspended<RuntimeAsyncTask<T>, Ops>(this);
            }

            void ITaskCompletionAction.Invoke(Task completingTask)
            {
                MoveNext();
            }

            bool ITaskCompletionAction.InvokeMayRunArbitraryCode => true;

            private static readonly SendOrPostCallback s_postCallback = static state =>
            {
                Debug.Assert(state is RuntimeAsyncTask<T>);
                ((RuntimeAsyncTask<T>)state).MoveNext();
            };

            private struct Ops : IRuntimeAsyncTaskOps<RuntimeAsyncTask<T>>
            {
                public static Action GetContinuationAction(RuntimeAsyncTask<T> task) => (Action)task.m_action!;
                public static Continuation GetContinuationState(RuntimeAsyncTask<T> task) => (Continuation)task.m_stateObject!;
                public static void SetContinuationState(RuntimeAsyncTask<T> task, Continuation value)
                {
                    task.m_stateObject = value;
                }

                public static bool SetCompleted(RuntimeAsyncTask<T> task)
                {
                    return task.TrySetResult(task.m_result);
                }

                public static void PostToSyncContext(RuntimeAsyncTask<T> task, SynchronizationContext syncContext)
                {
                    syncContext.Post(s_postCallback, task);
                }

                public static ref byte GetResultStorage(RuntimeAsyncTask<T> task) => ref Unsafe.As<T?, byte>(ref task.m_result);
            }
        }

        /// <summary>
        /// Represents a wrapped runtime async operation.
        /// </summary>
        private sealed class RuntimeAsyncTask : Task, ITaskCompletionAction
        {
            public RuntimeAsyncTask()
            {
                // We use the base Task's state object field to store the Continuation while posting the task around.
                // Ensure that state object isn't published out for others to see.
                Debug.Assert((m_stateFlags & (int)InternalTaskOptions.PromiseTask) != 0, "Expected state flags to already be configured.");
                Debug.Assert(m_stateObject is null, "Expected to be able to use the state object field for Continuation.");
                m_action = MoveNext;
                m_stateFlags |= (int)InternalTaskOptions.HiddenState;
            }

            internal override void ExecuteFromThreadPool(Thread threadPoolThread)
            {
                MoveNext();
            }

            private void MoveNext()
            {
                RuntimeAsyncTaskCore.DispatchContinuations<RuntimeAsyncTask, Ops>(this);
            }

            public void HandleSuspended()
            {
                RuntimeAsyncTaskCore.HandleSuspended<RuntimeAsyncTask, Ops>(this);
            }

            void ITaskCompletionAction.Invoke(Task completingTask)
            {
                MoveNext();
            }

            bool ITaskCompletionAction.InvokeMayRunArbitraryCode => true;

            private static readonly SendOrPostCallback s_postCallback = static state =>
            {
                Debug.Assert(state is RuntimeAsyncTask);
                ((RuntimeAsyncTask)state).MoveNext();
            };

            private struct Ops : IRuntimeAsyncTaskOps<RuntimeAsyncTask>
            {
                public static Action GetContinuationAction(RuntimeAsyncTask task) => (Action)task.m_action!;
                public static Continuation GetContinuationState(RuntimeAsyncTask task) => (Continuation)task.m_stateObject!;
                public static void SetContinuationState(RuntimeAsyncTask task, Continuation value)
                {
                    task.m_stateObject = value;
                }

                public static bool SetCompleted(RuntimeAsyncTask task)
                {
                    return task.TrySetResult();
                }

                public static void PostToSyncContext(RuntimeAsyncTask task, SynchronizationContext syncContext)
                {
                    syncContext.Post(s_postCallback, task);
                }

                public static ref byte GetResultStorage(RuntimeAsyncTask task) => ref Unsafe.NullRef<byte>();
            }
        }

        private static class RuntimeAsyncTaskCore
        {
            public static unsafe void DispatchContinuations<T, TOps>(T task) where T : Task, ITaskCompletionAction where TOps : IRuntimeAsyncTaskOps<T>
            {
                ExecutionAndSyncBlockStore contexts = default;
                contexts.Push();
                Continuation? continuation = TOps.GetContinuationState(task);

                while (true)
                {
                    Debug.Assert(continuation != null);
                    try
                    {
                        ref byte resultLoc = ref continuation.Next != null ? ref continuation.Next.GetResultStorageOrNull() : ref TOps.GetResultStorage(task);
                        Continuation? newContinuation = continuation.Resume(continuation, ref resultLoc);

                        if (newContinuation != null)
                        {
                            newContinuation.Next = continuation.Next;
                            HandleSuspended<T, TOps>(task);
                            contexts.Pop();
                            return;
                        }

                        continuation = continuation.Next;
                    }
                    catch (Exception ex)
                    {
                        Debug.Assert(continuation != null);
                        Continuation? nextContinuation = UnwindToPossibleHandler(continuation);
                        if (nextContinuation == null)
                        {
                            // Tail of AsyncTaskMethodBuilderT.SetException
                            bool successfullySet = ex is OperationCanceledException oce ?
                                task.TrySetCanceled(oce.CancellationToken, oce) :
                                task.TrySetException(ex);

                            contexts.Pop();

                            if (!successfullySet)
                            {
                                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted);
                            }

                            return;
                        }

                        nextContinuation.SetException(ex);

                        continuation = nextContinuation;
                    }

                    if (continuation == null)
                    {
                        bool successfullySet = TOps.SetCompleted(task);

                        contexts.Pop();

                        if (!successfullySet)
                        {
                            ThrowHelper.ThrowInvalidOperationException(ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted);
                        }

                        return;
                    }

                    if (QueueContinuationFollowUpActionIfNecessary<T, TOps>(task, continuation))
                    {
                        contexts.Pop();
                        return;
                    }
                }
            }

            private static Continuation? UnwindToPossibleHandler(Continuation continuation)
            {
                while (true)
                {
                    Continuation? nextContinuation = continuation.Next;
                    if (nextContinuation == null)
                        return null;

                    if ((nextContinuation.Flags & ContinuationFlags.HasException) != 0)
                        return nextContinuation;

                    continuation = nextContinuation;
                }
            }

            public static void HandleSuspended<T, TOps>(T task) where T : Task, ITaskCompletionAction where TOps : IRuntimeAsyncTaskOps<T>
            {
                ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
                ICriticalNotifyCompletion? critNotifier = state.CriticalNotifier;
                INotifyCompletion? notifier = state.Notifier;
                Task? calledTask = state.CalledTask;

                state.CriticalNotifier = null;
                state.Notifier = null;
                state.CalledTask = null;

                Continuation sentinelContinuation = state.SentinelContinuation!;
                Continuation headContinuation = sentinelContinuation.Next!;
                sentinelContinuation.Next = null;

                // Head continuation should be the result of async call to AwaitAwaiter or UnsafeAwaitAwaiter.
                // These never have special continuation handling.
                const ContinuationFlags continueFlags =
                    ContinuationFlags.ContinueOnCapturedSynchronizationContext |
                    ContinuationFlags.ContinueOnThreadPool |
                    ContinuationFlags.ContinueOnCapturedTaskScheduler;
                Debug.Assert((headContinuation.Flags & continueFlags) == 0);

                TOps.SetContinuationState(task, headContinuation);

                try
                {
                    if (critNotifier != null)
                    {
                        critNotifier.UnsafeOnCompleted(TOps.GetContinuationAction(task));
                    }
                    else if (calledTask != null)
                    {
                        // Runtime async callable wrapper for task returning
                        // method. This implements the context transparent
                        // forwarding and makes these wrappers minimal cost.
                        if (!calledTask.TryAddCompletionAction(task))
                        {
                            ThreadPool.UnsafeQueueUserWorkItemInternal(task, preferLocal: true);
                        }
                    }
                    else
                    {
                        Debug.Assert(notifier != null);
                        notifier.OnCompleted(TOps.GetContinuationAction(task));
                    }
                }
                catch (Exception ex)
                {
                    Task.ThrowAsync(ex, targetContext: null);
                }
            }

            private static bool QueueContinuationFollowUpActionIfNecessary<T, TOps>(T task, Continuation continuation) where T : Task where TOps : IRuntimeAsyncTaskOps<T>
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

                    TOps.SetContinuationState(task, continuation);
                    ThreadPool.UnsafeQueueUserWorkItemInternal(task, preferLocal: true);
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

                    TOps.SetContinuationState(task, continuation);

                    try
                    {
                        TOps.PostToSyncContext(task, continuationSyncCtx);
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

                    TOps.SetContinuationState(task, continuation);
                    // TODO: We do not need TaskSchedulerAwaitTaskContinuation here, just need to refactor its Run method...
                    var taskSchedCont = new TaskSchedulerAwaitTaskContinuation(sched, TOps.GetContinuationAction(task), flowExecutionContext: false);
                    taskSchedCont.Run(Task.CompletedTask, canInlineContinuationTask: true);

                    return true;
                }

                return false;
            }
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
            RuntimeAsyncTask result = new();
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ExecutionContext? CaptureExecutionContext()
        {
            return Thread.CurrentThreadAssumedInitialized._executionContext;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RestoreExecutionContext(ExecutionContext? previousExecCtx)
        {
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
            execCtx = thread._executionContext;
            syncCtx = thread._synchronizationContext;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RestoreContexts(bool suspended, ExecutionContext? previousExecCtx, SynchronizationContext? previousSyncCtx)
        {
            Thread thread = Thread.CurrentThreadAssumedInitialized;
            if (!suspended && previousSyncCtx != thread._synchronizationContext)
            {
                thread._synchronizationContext = previousSyncCtx;
            }

            ExecutionContext? currentExecCtx = thread._executionContext;
            if (previousExecCtx != currentExecCtx)
            {
                ExecutionContext.RestoreChangedContextToThread(thread, previousExecCtx, currentExecCtx);
            }
        }

        private static void CaptureContinuationContext(SynchronizationContext syncCtx, ref object context, ref ContinuationFlags flags)
        {
            if (syncCtx != null && syncCtx.GetType() != typeof(SynchronizationContext))
            {
                flags |= ContinuationFlags.ContinueOnCapturedSynchronizationContext;
                context = syncCtx;
                return;
            }

            TaskScheduler? sched = TaskScheduler.InternalCurrent;
            if (sched != null && sched != TaskScheduler.Default)
            {
                flags |= ContinuationFlags.ContinueOnCapturedTaskScheduler;
                context = sched;
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
