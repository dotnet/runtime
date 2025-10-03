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
            public INotifyCompletion? Notifier;
        }

        [ThreadStatic]
        private static RuntimeAsyncAwaitState t_runtimeAsyncAwaitState;

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

        private interface IThunkTaskOps<T>
        {
            static abstract Action GetContinuationAction(T task);
            static abstract Continuation GetContinuationState(T task);
            static abstract void SetContinuationState(T task, Continuation value);
            static abstract bool SetCompleted(T task, Continuation continuation);
            static abstract void PostToSyncContext(T task, SynchronizationContext syncCtx);
        }

        private sealed class ThunkTask<T> : Task<T>
        {
            public ThunkTask()
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
                ThunkTaskCore.MoveNext<ThunkTask<T>, Ops>(this);
            }

            public void HandleSuspended()
            {
                ThunkTaskCore.HandleSuspended<ThunkTask<T>, Ops>(this);
            }

            private static readonly SendOrPostCallback s_postCallback = static state =>
            {
                Debug.Assert(state is ThunkTask<T>);
                ((ThunkTask<T>)state).MoveNext();
            };

            private struct Ops : IThunkTaskOps<ThunkTask<T>>
            {
                public static Action GetContinuationAction(ThunkTask<T> task) => (Action)task.m_action!;
                public static void MoveNext(ThunkTask<T> task) => task.MoveNext();
                public static Continuation GetContinuationState(ThunkTask<T> task) => (Continuation)task.m_stateObject!;
                public static void SetContinuationState(ThunkTask<T> task, Continuation value)
                {
                    task.m_stateObject = value;
                }

                public static bool SetCompleted(ThunkTask<T> task, Continuation continuation)
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

                public static void PostToSyncContext(ThunkTask<T> task, SynchronizationContext syncContext)
                {
                    syncContext.Post(s_postCallback, task);
                }
            }
        }

        private sealed class ThunkTask : Task
        {
            public ThunkTask()
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
                ThunkTaskCore.MoveNext<ThunkTask, Ops>(this);
            }

            public void HandleSuspended()
            {
                ThunkTaskCore.HandleSuspended<ThunkTask, Ops>(this);
            }

            private static readonly SendOrPostCallback s_postCallback = static state =>
            {
                Debug.Assert(state is ThunkTask);
                ((ThunkTask)state).MoveNext();
            };

            private struct Ops : IThunkTaskOps<ThunkTask>
            {
                public static Action GetContinuationAction(ThunkTask task) => (Action)task.m_action!;
                public static void MoveNext(ThunkTask task) => task.MoveNext();
                public static Continuation GetContinuationState(ThunkTask task) => (Continuation)task.m_stateObject!;
                public static void SetContinuationState(ThunkTask task, Continuation value)
                {
                    task.m_stateObject = value;
                }

                public static bool SetCompleted(ThunkTask task, Continuation continuation)
                {
                    return task.TrySetResult();
                }

                public static void PostToSyncContext(ThunkTask task, SynchronizationContext syncContext)
                {
                    syncContext.Post(s_postCallback, task);
                }
            }
        }

        private static class ThunkTaskCore
        {
            public static unsafe void MoveNext<T, TOps>(T task) where T : Task where TOps : IThunkTaskOps<T>
            {
                ExecutionAndSyncBlockStore contexts = default;
                contexts.Push();
                Continuation continuation = TOps.GetContinuationState(task);

                while (true)
                {
                    try
                    {
                        Continuation? newContinuation = continuation.Resume(continuation);

                        if (newContinuation != null)
                        {
                            newContinuation.Next = continuation.Next;
                            HandleSuspended<T, TOps>(task);
                            contexts.Pop();
                            return;
                        }

                        Debug.Assert(continuation.Next != null);
                        continuation = continuation.Next;
                    }
                    catch (Exception ex)
                    {
                        Continuation nextContinuation = UnwindToPossibleHandler(continuation);
                        if (nextContinuation.Resume == null)
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

                    if (continuation.Resume == null)
                    {
                        bool successfullySet = TOps.SetCompleted(task, continuation);

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

            private static Continuation UnwindToPossibleHandler(Continuation continuation)
            {
                while (true)
                {
                    Debug.Assert(continuation.Next != null);
                    continuation = continuation.Next;
                    if ((continuation.Flags & CorInfoContinuationFlags.CORINFO_CONTINUATION_NEEDS_EXCEPTION) != 0)
                        return continuation;
                }
            }

            public static void HandleSuspended<T, TOps>(T task) where T : Task where TOps : IThunkTaskOps<T>
            {
                Continuation headContinuation = UnlinkHeadContinuation(out INotifyCompletion? notifier);

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
                    if (notifier is ICriticalNotifyCompletion crit)
                    {
                        crit.UnsafeOnCompleted(TOps.GetContinuationAction(task));
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

            private static Continuation UnlinkHeadContinuation(out INotifyCompletion? notifier)
            {
                ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
                notifier = state.Notifier;
                state.Notifier = null;

                Continuation sentinelContinuation = state.SentinelContinuation!;
                Continuation head = sentinelContinuation.Next!;
                sentinelContinuation.Next = null;
                return head;
            }

            private static bool QueueContinuationFollowUpActionIfNecessary<T, TOps>(T task, Continuation continuation) where T : Task where TOps : IThunkTaskOps<T>
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

        // Change return type to ThunkTask<T?> -- no benefit since this is used for Task returning thunks only
#pragma warning disable CA1859
        // When a Task-returning thunk gets a continuation result
        // it calls here to make a Task that awaits on the current async state.
        private static Task<T?> FinalizeTaskReturningThunk<T>(Continuation continuation, Exception ex)
        {
            if (continuation is not null)
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

                ThunkTask<T?> result = new();
                result.HandleSuspended();
                return result;
            }
            else
            {
                Task<T?> task = new();
                // Tail of AsyncTaskMethodBuilderT.SetException
                bool successfullySet = ex is OperationCanceledException oce ?
                    task.TrySetCanceled(oce.CancellationToken, oce) :
                    task.TrySetException(ex);

                Debug.Assert(successfullySet);
                return task;
            }
        }

        // We come here when a Task-returning thunk called into async method and did not see a normal return.
        // The result will be:
        // * continuation => return an incomplete Task that represents the continuation chain.
        // * exception    => return a Faulted or Canceled task, accordingly.
        private static Task FinalizeTaskReturningThunk(Continuation continuation, Exception ex)
        {
            if (continuation is not null)
            {
                Continuation finalContinuation = new Continuation
                {
                    Flags = CorInfoContinuationFlags.CORINFO_CONTINUATION_NEEDS_EXCEPTION,
                };
                continuation.Next = finalContinuation;

                ThunkTask result = new();
                result.HandleSuspended();
                return result;
            }
            else
            {
                Debug.Assert(ex is not null);
                Task task = new();
                // Tail of AsyncTaskMethodBuilderT.SetException
                bool successfullySet = ex is OperationCanceledException oce ?
                    task.TrySetCanceled(oce.CancellationToken, oce) :
                    task.TrySetException(ex);

                Debug.Assert(successfullySet);
                return task;
            }
        }

        private static ValueTask<T?> FinalizeValueTaskReturningThunk<T>(Continuation continuation, Exception ex)
        {
            // We only come to these methods to handle abnormal cases (suspended, faulted, canceled),
            // so ValueTask optimization here is not relevant.
            return new ValueTask<T?>(FinalizeTaskReturningThunk<T>(continuation, ex));
        }

        private static ValueTask FinalizeValueTaskReturningThunk(Continuation continuation, Exception ex)
        {
            return new ValueTask(FinalizeTaskReturningThunk(continuation, ex));
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
    }
}
