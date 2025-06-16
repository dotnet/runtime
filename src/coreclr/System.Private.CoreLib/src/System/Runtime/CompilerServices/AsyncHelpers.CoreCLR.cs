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

        public void PushExceptDefault()
        {
            Thread currentThread = Thread.CurrentThread;
            _thread = currentThread;
            ExecutionContext? previousExecutionCtx = currentThread._executionContext;
            if (previousExecutionCtx != null && !previousExecutionCtx.IsDefault)
            {
                _previousExecutionCtx = previousExecutionCtx;
            }

            _previousSyncCtx = currentThread._synchronizationContext;
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
        // If this bit is set the continuation has a SynchronizationContext
        // that we should continue on
        CORINFO_CONTINUATION_CONTINUE_ON_CAPTURED_SYNC_CONTEXT = 8,
    }

    internal sealed unsafe class Continuation
    {
        public Continuation? Next;
        public delegate*<Continuation, Continuation?> Resume;
        public uint State;
        public CorInfoContinuationFlags Flags;

        public ExecutionContext? ExecutionContext;
        // TODO: For ConfigureAwait(false) we should create a layout that does
        // not store the synchronization context.
        public SynchronizationContext? SynchronizationContext;
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
        }

        [ThreadStatic]
        private static RuntimeAsyncAwaitState t_runtimeAsyncAwaitState;

        private static Continuation AllocContinuation(Continuation prevContinuation, nuint numGCRefs, nuint dataSize)
        {
            Continuation newContinuation = new Continuation
            {
                Data = new byte[dataSize],
                GCData = new object[numGCRefs],
                ExecutionContext = ExecutionContext.Capture(),
                SynchronizationContext = SynchronizationContext.Current,
            };
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

            Continuation newContinuation = new Continuation
            {
                Data = new byte[dataSize],
                GCData = gcData,
                ExecutionContext = ExecutionContext.Capture(),
                SynchronizationContext = SynchronizationContext.Current,
            };
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

            Continuation newContinuation = new Continuation
            {
                Data = new byte[dataSize],
                GCData = gcData,
                ExecutionContext = ExecutionContext.Capture(),
                SynchronizationContext = SynchronizationContext.Current,
            };
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

        private static Continuation UnlinkHeadContinuation(out ICriticalNotifyCompletion? criticalNotifier, out INotifyCompletion? notifier)
        {
            ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
            criticalNotifier = state.CriticalNotifier;
            state.CriticalNotifier = null;
            notifier = state.Notifier;
            state.Notifier = null;

            Continuation sentinelContinuation = state.SentinelContinuation!;
            Continuation head = sentinelContinuation.Next!;
            sentinelContinuation.Next = null;
            return head;
        }

        private sealed class ThunkTask<T> : Task<T>
        {
            public ThunkTask()
            {
                m_action = MoveNext;
                m_stateFlags |= (int)InternalTaskOptions.HiddenState;
            }

            public unsafe void MoveNext()
            {
                Debug.Assert(!((Continuation)m_stateObject!).Flags.HasFlag(CorInfoContinuationFlags.CORINFO_CONTINUATION_CONTINUE_ON_CAPTURED_SYNC_CONTEXT));

                ExecutionAndSyncBlockStore contexts = default;
                contexts.PushExceptDefault();
                while (true)
                {
                    try
                    {
                        Continuation continuation = (Continuation)m_stateObject!;

                        while (true)
                        {
                            // Inner hot loop where continuations are executed.
                            if (continuation.ExecutionContext != null)
                            {
                                // ExecutionContext.RunInternal, except we need
                                // the result, and we do not have to restore
                                // contexts after (we will do it once on exit).
                                ExecutionContext? newExecContext = continuation.ExecutionContext;
                                if (newExecContext != null && newExecContext.IsDefault)
                                    newExecContext = null;

                                ExecutionContext? curExecContext = contexts._thread!._executionContext;
                                if (curExecContext != null && curExecContext.IsDefault)
                                    curExecContext = null;

                                if (newExecContext != curExecContext)
                                {
                                    ExecutionContext.RestoreChangedContextToThread(
                                        contexts._thread,
                                        newExecContext,
                                        curExecContext);
                                }
                            }

                            Continuation? nextContinuation = continuation.Resume(continuation);
                            if (nextContinuation != null)
                            {
                                nextContinuation.Next = continuation.Next;
                                HandleSuspended();
                                contexts.Pop();
                                return;
                            }

                            Debug.Assert(continuation.Next != null);
                            continuation = continuation.Next;

                            if (continuation.Resume == null)
                            {
                                // Final continuation, extract the result.
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

                                if (!TrySetResult(result))
                                {
                                    contexts.Pop();
                                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted);
                                }

                                contexts.Pop();
                                return;
                            }

                            if ((continuation.Flags & CorInfoContinuationFlags.CORINFO_CONTINUATION_CONTINUE_ON_CAPTURED_SYNC_CONTEXT) != 0 &&
                                PostToSyncContextIfNecessary(continuation, ref contexts))
                            {
                                contexts.Pop();
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Continuation nextContinuation = UnwindToPossibleHandler((Continuation)m_stateObject!);
                        if (nextContinuation.Resume == null)
                        {
                            // Tail of AsyncTaskMethodBuilderT.SetException
                            bool successfullySet = ex is OperationCanceledException oce ?
                                TrySetCanceled(oce.CancellationToken, oce) :
                                TrySetException(ex);

                            if (!successfullySet)
                            {
                                contexts.Pop();
                                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted);
                            }

                            return;
                        }

                        nextContinuation.GCData![(nextContinuation.Flags & CorInfoContinuationFlags.CORINFO_CONTINUATION_RESULT_IN_GCDATA) != 0 ? 1 : 0] = ex;

                        if ((nextContinuation.Flags & CorInfoContinuationFlags.CORINFO_CONTINUATION_CONTINUE_ON_CAPTURED_SYNC_CONTEXT) != 0 &&
                            PostToSyncContextIfNecessary(nextContinuation, ref contexts))
                        {
                            contexts.Pop();
                            return;
                        }

                        m_stateObject = nextContinuation;
                    }
                }
            }

            private bool PostToSyncContextIfNecessary(Continuation continuation, ref ExecutionAndSyncBlockStore contexts)
            {
                SynchronizationContext? continuationSyncCtx = continuation.SynchronizationContext;

                if (continuationSyncCtx == null || continuationSyncCtx.GetType() == typeof(SynchronizationContext) ||
                    continuationSyncCtx == SynchronizationContext.Current)
                {
                    return false;
                }

                try
                {
                    // TODO: Does this need a volatile write?
                    m_stateObject = continuation;
                    continuationSyncCtx.Post(s_postCallback, this);
                }
                catch (Exception ex)
                {
                    contexts.Pop();
                    ThrowAsync(ex, targetContext: null);
                }

                return true;
            }

            public void HandleSuspended()
            {
                Continuation headContinuation = UnlinkHeadContinuation(out ICriticalNotifyCompletion? criticalNotifier, out INotifyCompletion? notifier);
                // Head continuation should be the result of async call to
                // AwaitAwaiter or UnsafeAwaitAwaiter, and these cannot be
                // configured.
                Debug.Assert(!headContinuation.Flags.HasFlag(CorInfoContinuationFlags.CORINFO_CONTINUATION_CONTINUE_ON_CAPTURED_SYNC_CONTEXT));
                m_stateObject = headContinuation;

                try
                {
                    if (criticalNotifier != null)
                    {
                        criticalNotifier.UnsafeOnCompleted((Action)m_action!);
                    }
                    else
                    {
                        Debug.Assert(notifier != null);
                        notifier.OnCompleted((Action)m_action!);
                    }
                }
                catch (Exception ex)
                {
                    ThrowAsync(ex, targetContext: null);
                }
            }

            private static readonly SendOrPostCallback s_postCallback = static state =>
            {
                Debug.Assert(state is ThunkTask<T>);
                ((ThunkTask<T>)state).MoveNext();
            };
        }

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

            ThunkTask<T?> result = new();
            result.HandleSuspended();
            return result;
        }

        private sealed class ThunkTask : Task
        {
            public ThunkTask()
            {
                m_action = MoveNext;
                m_stateFlags |= (int)InternalTaskOptions.HiddenState;
            }

            public unsafe void MoveNext()
            {
                Debug.Assert(!((Continuation)m_stateObject!).Flags.HasFlag(CorInfoContinuationFlags.CORINFO_CONTINUATION_CONTINUE_ON_CAPTURED_SYNC_CONTEXT));

                ExecutionAndSyncBlockStore contexts = default;
                contexts.PushExceptDefault();
                while (true)
                {
                    try
                    {
                        Continuation continuation = (Continuation)m_stateObject!;

                        while (true)
                        {
                            // Inner hot loop where continuations are executed.
                            if (continuation.ExecutionContext != null)
                            {
                                // ExecutionContext.RunInternal, except we need
                                // the result, and we do not have to restore
                                // contexts after (we will do it once on exit).
                                ExecutionContext? newExecContext = continuation.ExecutionContext;
                                if (newExecContext != null && newExecContext.IsDefault)
                                    newExecContext = null;

                                ExecutionContext? curExecContext = contexts._thread!._executionContext;
                                if (curExecContext != null && curExecContext.IsDefault)
                                    curExecContext = null;

                                if (newExecContext != curExecContext)
                                {
                                    ExecutionContext.RestoreChangedContextToThread(
                                        contexts._thread,
                                        newExecContext,
                                        curExecContext);
                                }
                            }

                            Continuation? nextContinuation = continuation.Resume(continuation);
                            if (nextContinuation != null)
                            {
                                nextContinuation.Next = continuation.Next;
                                HandleSuspended();
                                contexts.Pop();
                                return;
                            }

                            Debug.Assert(continuation.Next != null);
                            continuation = continuation.Next;

                            if (continuation.Resume == null)
                            {
                                if (!TrySetResult())
                                {
                                    contexts.Pop();
                                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted);
                                }

                                contexts.Pop();
                                return;
                            }

                            if ((continuation.Flags & CorInfoContinuationFlags.CORINFO_CONTINUATION_CONTINUE_ON_CAPTURED_SYNC_CONTEXT) != 0 &&
                                PostToSyncContextIfNecessary(continuation, ref contexts))
                            {
                                contexts.Pop();
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Continuation nextContinuation = UnwindToPossibleHandler((Continuation)m_stateObject!);
                        if (nextContinuation.Resume == null)
                        {
                            // Tail of AsyncTaskMethodBuilderT.SetException
                            bool successfullySet = ex is OperationCanceledException oce ?
                                TrySetCanceled(oce.CancellationToken, oce) :
                                TrySetException(ex);

                            if (!successfullySet)
                            {
                                contexts.Pop();
                                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted);
                            }

                            return;
                        }

                        nextContinuation.GCData![(nextContinuation.Flags & CorInfoContinuationFlags.CORINFO_CONTINUATION_RESULT_IN_GCDATA) != 0 ? 1 : 0] = ex;

                        if ((nextContinuation.Flags & CorInfoContinuationFlags.CORINFO_CONTINUATION_CONTINUE_ON_CAPTURED_SYNC_CONTEXT) != 0 &&
                            PostToSyncContextIfNecessary(nextContinuation, ref contexts))
                        {
                            contexts.Pop();
                            return;
                        }

                        m_stateObject = nextContinuation;
                    }
                }
            }

            private bool PostToSyncContextIfNecessary(Continuation continuation, ref ExecutionAndSyncBlockStore contexts)
            {
                SynchronizationContext? continuationSyncCtx = continuation.SynchronizationContext;

                if (continuationSyncCtx == null || continuationSyncCtx.GetType() == typeof(SynchronizationContext) ||
                    continuationSyncCtx == SynchronizationContext.Current)
                {
                    return false;
                }

                try
                {
                    // TODO: Does this need a volatile write?
                    m_stateObject = continuation;
                    continuationSyncCtx.Post(s_postCallback, this);
                }
                catch (Exception ex)
                {
                    contexts.Pop();
                    ThrowAsync(ex, targetContext: null);
                }

                return true;
            }

            public void HandleSuspended()
            {
                Continuation headContinuation = UnlinkHeadContinuation(out ICriticalNotifyCompletion? criticalNotifier, out INotifyCompletion? notifier);
                // Head continuation should be the result of async call to
                // AwaitAwaiter or UnsafeAwaitAwaiter, and these cannot be
                // configured.
                Debug.Assert(!headContinuation.Flags.HasFlag(CorInfoContinuationFlags.CORINFO_CONTINUATION_CONTINUE_ON_CAPTURED_SYNC_CONTEXT));
                m_stateObject = headContinuation;

                try
                {
                    if (criticalNotifier != null)
                    {
                        criticalNotifier.UnsafeOnCompleted((Action)m_action!);
                    }
                    else
                    {
                        Debug.Assert(notifier != null);
                        notifier.OnCompleted((Action)m_action!);
                    }
                }
                catch (Exception ex)
                {
                    ThrowAsync(ex, targetContext: null);
                }
            }

            private static readonly SendOrPostCallback s_postCallback = static state =>
            {
                Debug.Assert(state is ThunkTask);
                ((ThunkTask)state).MoveNext();
            };
        }

#pragma warning disable CA1859
        private static Task FinalizeTaskReturningThunk(Continuation continuation)
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

        private static ValueTask<T?> FinalizeValueTaskReturningThunk<T>(Continuation continuation)
        {
            return new ValueTask<T?>(FinalizeTaskReturningThunk<T>(continuation));
        }

        private static ValueTask FinalizeValueTaskReturningThunk(Continuation continuation)
        {
            return new ValueTask(FinalizeTaskReturningThunk(continuation));
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
    }
}
