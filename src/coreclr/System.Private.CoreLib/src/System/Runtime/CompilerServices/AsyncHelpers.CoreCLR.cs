// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

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
    internal enum CorInfoContinuationFlags
    {
        // Whether or not the continuation expects the result to be boxed and
        // placed in the GCData array at index 0. Not set if the callee is void.
        CORINFO_CONTINUATION_RESULT_IN_GCDATA = 1,
        // If this bit is set the continuation resumes inside a try block and thus
        // if an exception is being propagated, needs to be resumed. The exception
        // should be placed at index 0 or 1 depending on whether the continuation
        // also expects a result.
        CORINFO_CONTINUATION_NEEDS_EXCEPTION = 2,
        // If this bit is set the continuation has the IL offset that inspired the
        // OSR method saved in the beginning of 'Data', or -1 if the continuation
        // belongs to a tier 0 method.
        CORINFO_CONTINUATION_OSR_IL_OFFSET_IN_DATA = 4,
        // If this bit is set the continuation should continue on the thread
        // pool.
        CORINFO_CONTINUATION_CONTINUE_ON_THREAD_POOL = 8,
        // If this bit is set the continuation has a SynchronizationContext
        // that we should continue on.
        CORINFO_CONTINUATION_CONTINUE_ON_CAPTURED_SYNCHRONIZATION_CONTEXT = 16,
        // If this bit is set the continuation has a TaskScheduler
        // that we should continue on.
        CORINFO_CONTINUATION_CONTINUE_ON_CAPTURED_TASK_SCHEDULER = 32,
    }

    internal sealed unsafe class Continuation
    {
        public Continuation? Next;
        public delegate*<Continuation, Continuation?> Resume;
        public uint State;
        public CorInfoContinuationFlags Flags;

        // Data and GCData contain the state of the continuation.
        // Note: The JIT is ultimately responsible for laying out these arrays.
        // However, other parts of the system depend on the layout to
        // know where to locate or place various pieces of data:
        //
        // 1. Resumption stubs need to know where to place the return value
        // inside the next continuation. If the return value has GC references
        // then it is boxed and placed at GCData[0]; otherwise, it is placed
        // inside Data at offset 0 if
        // CORINFO_CONTINUATION_OSR_IL_OFFSET_IN_DATA is NOT set and otherwise
        // at offset 4.
        //
        // 2. Likewise, Finalize[Value]TaskReturningThunk needs to know from
        // where to extract the return value.
        //
        // 3. The dispatcher needs to know where to place the exception inside
        // the next continuation with a handler. Continuations with handlers
        // have CORINFO_CONTINUATION_NEEDS_EXCEPTION set. The exception is
        // placed at GCData[0] if CORINFO_CONTINUATION_RESULT_IN_GCDATA is NOT
        // set, and otherwise at GCData[1].
        //
        public byte[]? Data;
        public object?[]? GCData;

        public object GetContinuationContext()
        {
            int index = 0;
            if ((Flags & CorInfoContinuationFlags.CORINFO_CONTINUATION_RESULT_IN_GCDATA) != 0)
                index++;
            if ((Flags & CorInfoContinuationFlags.CORINFO_CONTINUATION_NEEDS_EXCEPTION) != 0)
                index++;
            Debug.Assert(GCData != null && GCData.Length > index);
            object? continuationContext = GCData[index];
            Debug.Assert(continuationContext != null);
            return continuationContext;
        }

        public void SetException(Exception ex)
        {
            int index = 0;
            if ((Flags & CorInfoContinuationFlags.CORINFO_CONTINUATION_RESULT_IN_GCDATA) != 0)
                index++;

            Debug.Assert(GCData != null && GCData.Length > index);
            GCData[index] = ex;
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

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "AsyncHelpers_AddContinuationToExInternal")]
        private static unsafe partial void AddContinuationToExInternal(void* resume, uint state, ObjectHandleOnStack ex);

        internal static unsafe void AddContinuationToExInternal(Continuation continuation, Exception e)
            => AddContinuationToExInternal(continuation.Resume, continuation.State, ObjectHandleOnStack.Create(ref e));

        private static Continuation AllocContinuation(Continuation prevContinuation, nuint numGCRefs, nuint dataSize)
        {
            Continuation newContinuation = new Continuation { Data = new byte[dataSize], GCData = new object[numGCRefs] };
            prevContinuation.Next = newContinuation;
            return newContinuation;
        }

        private static unsafe Continuation AllocContinuationMethod(Continuation prevContinuation, nuint numGCRefs, nuint dataSize, MethodDesc* method)
        {
            LoaderAllocator loaderAllocator = RuntimeMethodHandle.GetLoaderAllocator(new RuntimeMethodHandleInternal((IntPtr)method));
            object?[] gcData;
            if (loaderAllocator != null)
            {
                gcData = new object[numGCRefs + 1];
                gcData[numGCRefs] = loaderAllocator;
            }
            else
            {
                gcData = new object[numGCRefs];
            }

            Continuation newContinuation = new Continuation { Data = new byte[dataSize], GCData = gcData };
            prevContinuation.Next = newContinuation;
            return newContinuation;
        }

        private static unsafe Continuation AllocContinuationClass(Continuation prevContinuation, nuint numGCRefs, nuint dataSize, MethodTable* methodTable)
        {
            IntPtr loaderAllocatorHandle = methodTable->GetLoaderAllocatorHandle();
            object?[] gcData;
            if (loaderAllocatorHandle != IntPtr.Zero)
            {
                gcData = new object[numGCRefs + 1];
                gcData[numGCRefs] = GCHandle.FromIntPtr(loaderAllocatorHandle).Target;
            }
            else
            {
                gcData = new object[numGCRefs];
            }

            Continuation newContinuation = new Continuation { Data = new byte[dataSize], GCData = gcData };
            prevContinuation.Next = newContinuation;
            return newContinuation;
        }

        // Used to box the return value before storing into caller's continuation
        // if the value is an object-containing struct.
        // We are allocating a box directly instead of relying on regular boxing because we want
        // to store structs without changing layout, including nullables.
        private static unsafe object AllocContinuationResultBox(void* ptr)
        {
            MethodTable* pMT = (MethodTable*)ptr;
            Debug.Assert(pMT->IsValueType);
            // We need no type/cctor checks since we will be storing an instance that already exists.
            return RuntimeTypeHandle.InternalAllocNoChecks((MethodTable*)pMT);
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
            static abstract bool SetCompleted(T task, Continuation continuation);
            static abstract void PostToSyncContext(T task, SynchronizationContext syncCtx);
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
                public static void MoveNext(RuntimeAsyncTask<T> task) => task.MoveNext();
                public static Continuation GetContinuationState(RuntimeAsyncTask<T> task) => (Continuation)task.m_stateObject!;
                public static void SetContinuationState(RuntimeAsyncTask<T> task, Continuation value)
                {
                    task.m_stateObject = value;
                }

                public static bool SetCompleted(RuntimeAsyncTask<T> task, Continuation continuation)
                {
                    T result;
                    if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    {
                        if (typeof(T).IsValueType)
                        {
                            result = Unsafe.As<byte, T>(ref continuation.GCData![0]!.GetRawData());
                        }
                        else
                        {
                            result = Unsafe.As<object, T>(ref continuation.GCData![0]!);
                        }
                    }
                    else
                    {
                        result = Unsafe.As<byte, T>(ref continuation.Data![0]);
                    }

                    return task.TrySetResult(result);
                }

                public static void PostToSyncContext(RuntimeAsyncTask<T> task, SynchronizationContext syncContext)
                {
                    syncContext.Post(s_postCallback, task);
                }
            }
        }

        // Represents execution of a chain of suspended and resuming runtime
        // async functions.
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
                public static void MoveNext(RuntimeAsyncTask task) => task.MoveNext();
                public static Continuation GetContinuationState(RuntimeAsyncTask task) => (Continuation)task.m_stateObject!;
                public static void SetContinuationState(RuntimeAsyncTask task, Continuation value)
                {
                    task.m_stateObject = value;
                }

                public static bool SetCompleted(RuntimeAsyncTask task, Continuation continuation)
                {
                    return task.TrySetResult();
                }

                public static void PostToSyncContext(RuntimeAsyncTask task, SynchronizationContext syncContext)
                {
                    syncContext.Post(s_postCallback, task);
                }
            }
        }

        private static class RuntimeAsyncTaskCore
        {
            private unsafe struct NextContinuationData
            {
                public NextContinuationData* Next;
                public Continuation* NextContinuation;
            }

            // To be used for async stack walking
            [ThreadStatic]
            private static unsafe NextContinuationData* t_nextContinuation;

            public static unsafe void DispatchContinuations<T, TOps>(T task) where T : Task, ITaskCompletionAction where TOps : IRuntimeAsyncTaskOps<T>
            {
                ExecutionAndSyncBlockStore contexts = default;
                contexts.Push();
                Continuation continuation = TOps.GetContinuationState(task);

                ref NextContinuationData* nextContRef = ref t_nextContinuation;
                NextContinuationData nextContinuationData;
                nextContinuationData.Next = nextContRef;
                nextContinuationData.NextContinuation = &continuation;

                nextContRef = &nextContinuationData;

                while (true)
                {
                    try
                    {
                        Continuation? curContinuation = continuation;
                        Debug.Assert(curContinuation != null);
                        Debug.Assert(curContinuation.Next != null);
                        continuation = curContinuation.Next;

                        Continuation? newContinuation = curContinuation.Resume(curContinuation);

                        if (newContinuation != null)
                        {
                            newContinuation.Next = continuation;
                            HandleSuspended<T, TOps>(task);
                            contexts.Pop();
                            t_nextContinuation = nextContinuationData.Next;
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Continuation nextContinuation = UnwindToPossibleHandler(continuation, ex);
                        if (nextContinuation.Resume == null)
                        {
                            // Tail of AsyncTaskMethodBuilderT.SetException
                            bool successfullySet = ex is OperationCanceledException oce ?
                                task.TrySetCanceled(oce.CancellationToken, oce) :
                                task.TrySetException(ex);

                            contexts.Pop();

                            t_nextContinuation = nextContinuationData.Next;

                            if (!successfullySet)
                            {
                                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted);
                            }

                            return;
                        }

                        handlerContinuation.SetException(ex);
                        continuation = handlerContinuation;
                    }

                    if (continuation.Resume == null)
                    {
                        bool successfullySet = TOps.SetCompleted(task, continuation);

                        contexts.Pop();

                        t_nextContinuation = nextContinuationData.Next;

                        if (!successfullySet)
                        {
                            ThrowHelper.ThrowInvalidOperationException(ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted);
                        }

                        return;
                    }

                    if (QueueContinuationFollowUpActionIfNecessary<T, TOps>(task, continuation))
                    {
                        contexts.Pop();
                        t_nextContinuation = nextContinuationData.Next;
                        return;
                    }
                }
            }

            private static unsafe Continuation UnwindToPossibleHandler(Continuation continuation, Exception ex)
            {
                while (true)
                {
                    if (continuation.Resume != null) AddContinuationToExInternal(continuation, ex);
                    if ((continuation.Flags & CorInfoContinuationFlags.CORINFO_CONTINUATION_NEEDS_EXCEPTION) != 0)
                        return continuation;

                    Debug.Assert(continuation.Next != null);
                    continuation = continuation.Next;

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
                const CorInfoContinuationFlags continueFlags =
                    CorInfoContinuationFlags.CORINFO_CONTINUATION_CONTINUE_ON_CAPTURED_SYNCHRONIZATION_CONTEXT |
                    CorInfoContinuationFlags.CORINFO_CONTINUATION_CONTINUE_ON_THREAD_POOL |
                    CorInfoContinuationFlags.CORINFO_CONTINUATION_CONTINUE_ON_CAPTURED_TASK_SCHEDULER;
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
                if ((continuation.Flags & CorInfoContinuationFlags.CORINFO_CONTINUATION_CONTINUE_ON_THREAD_POOL) != 0)
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

                if ((continuation.Flags & CorInfoContinuationFlags.CORINFO_CONTINUATION_CONTINUE_ON_CAPTURED_SYNCHRONIZATION_CONTEXT) != 0)
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

                if ((continuation.Flags & CorInfoContinuationFlags.CORINFO_CONTINUATION_CONTINUE_ON_CAPTURED_TASK_SCHEDULER) != 0)
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
        private static Task<T?> FinalizeTaskReturningThunk<T>(Continuation continuation)
        {
            Continuation finalContinuation = new Continuation();

            // Note that the exact location the return value is placed is tied
            // into getAsyncResumptionStub in the VM, so do not change this
            // without also changing that code (and the JIT).
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                finalContinuation.Flags = CorInfoContinuationFlags.CORINFO_CONTINUATION_RESULT_IN_GCDATA | CorInfoContinuationFlags.CORINFO_CONTINUATION_NEEDS_EXCEPTION;
                finalContinuation.GCData = new object[1];
            }
            else
            {
                finalContinuation.Flags = CorInfoContinuationFlags.CORINFO_CONTINUATION_NEEDS_EXCEPTION;
                finalContinuation.Data = new byte[Unsafe.SizeOf<T>()];
            }

            continuation.Next = finalContinuation;

            RuntimeAsyncTask<T?> result = new();
            result.HandleSuspended();
            return result;
        }

        private static Task FinalizeTaskReturningThunk(Continuation continuation)
        {
            Continuation finalContinuation = new Continuation
            {
                Flags = CorInfoContinuationFlags.CORINFO_CONTINUATION_NEEDS_EXCEPTION,
            };
            continuation.Next = finalContinuation;

            RuntimeAsyncTask result = new();
            result.HandleSuspended();
            return result;
        }

        private static ValueTask<T?> FinalizeValueTaskReturningThunk<T>(Continuation continuation)
        {
            // We only come to these methods in the expensive case (already
            // suspended), so ValueTask optimization here is not relevant.
            return new ValueTask<T?>(FinalizeTaskReturningThunk<T>(continuation));
        }

        private static ValueTask FinalizeValueTaskReturningThunk(Continuation continuation)
        {
            return new ValueTask(FinalizeTaskReturningThunk(continuation));
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

        private static void CaptureContinuationContext(SynchronizationContext syncCtx, ref object context, ref CorInfoContinuationFlags flags)
        {
            if (syncCtx != null && syncCtx.GetType() != typeof(SynchronizationContext))
            {
                flags |= CorInfoContinuationFlags.CORINFO_CONTINUATION_CONTINUE_ON_CAPTURED_SYNCHRONIZATION_CONTEXT;
                context = syncCtx;
                return;
            }

            TaskScheduler? sched = TaskScheduler.InternalCurrent;
            if (sched != null && sched != TaskScheduler.Default)
            {
                flags |= CorInfoContinuationFlags.CORINFO_CONTINUATION_CONTINUE_ON_CAPTURED_TASK_SCHEDULER;
                context = sched;
                return;
            }

            flags |= CorInfoContinuationFlags.CORINFO_CONTINUATION_CONTINUE_ON_THREAD_POOL;
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
