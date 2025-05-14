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

        // wrapper to await a notifier
        private struct AwaitableProxy : ICriticalNotifyCompletion
        {
            private readonly INotifyCompletion _notifier;

            public AwaitableProxy(INotifyCompletion notifier)
            {
                _notifier = notifier;
            }

            public bool IsCompleted => false;

            public void OnCompleted(Action action)
            {
                _notifier!.OnCompleted(action);
            }

            public AwaitableProxy GetAwaiter() { return this; }

            public void UnsafeOnCompleted(Action action)
            {
                if (_notifier is ICriticalNotifyCompletion criticalNotification)
                {
                    criticalNotification.UnsafeOnCompleted(action);
                }
                else
                {
                    _notifier!.OnCompleted(action);
                }
            }

            public void GetResult() { }
        }

        private static Continuation UnlinkHeadContinuation(out AwaitableProxy awaitableProxy)
        {
            ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
            awaitableProxy = new AwaitableProxy(state.Notifier!);
            state.Notifier = null;

            Continuation sentinelContinuation = state.SentinelContinuation!;
            Continuation head = sentinelContinuation.Next!;
            sentinelContinuation.Next = null;
            return head;
        }

        // When a Task-returning thunk gets a continuation result
        // it calls here to make a Task that awaits on the current async state.
        // NOTE: This cannot be Runtime Async. Must use C# state machine or make one by hand.
        private static async Task<T?> FinalizeTaskReturningThunk<T>(Continuation continuation)
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

            while (true)
            {
                Continuation headContinuation = UnlinkHeadContinuation(out var awaitableProxy);
                await awaitableProxy;
                Continuation? finalResult = DispatchContinuations(headContinuation);
                if (finalResult != null)
                {
                    Debug.Assert(finalResult == finalContinuation);
                    if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    {
                        if (typeof(T).IsValueType)
                        {
                            return Unsafe.As<byte, T>(ref finalResult.GCData![0]!.GetRawData());
                        }

                        return Unsafe.As<object, T>(ref finalResult.GCData![0]!);
                    }
                    else
                    {
                        return Unsafe.As<byte, T>(ref finalResult.Data![0]);
                    }
                }
            }
        }

        private static async Task FinalizeTaskReturningThunk(Continuation continuation)
        {
            Continuation finalContinuation = new Continuation
            {
                Flags = CorInfoContinuationFlags.CORINFO_CONTINUATION_NEEDS_EXCEPTION,
            };
            continuation.Next = finalContinuation;

            while (true)
            {
                Continuation headContinuation = UnlinkHeadContinuation(out var awaitableProxy);
                await awaitableProxy;
                Continuation? finalResult = DispatchContinuations(headContinuation);
                if (finalResult != null)
                {
                    Debug.Assert(finalResult == finalContinuation);
                    return;
                }
            }
        }

        private static async ValueTask<T?> FinalizeValueTaskReturningThunk<T>(Continuation continuation)
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

            while (true)
            {
                Continuation headContinuation = UnlinkHeadContinuation(out var awaitableProxy);
                await awaitableProxy;
                Continuation? finalResult = DispatchContinuations(headContinuation);
                if (finalResult != null)
                {
                    Debug.Assert(finalResult == finalContinuation);
                    if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    {
                        if (typeof(T).IsValueType)
                        {
                            return Unsafe.As<byte, T>(ref finalResult.GCData![0]!.GetRawData());
                        }

                        return Unsafe.As<object, T>(ref finalResult.GCData![0]!);
                    }
                    else
                    {
                        return Unsafe.As<byte, T>(ref finalResult.Data![0]);
                    }
                }
            }
        }

        private static async ValueTask FinalizeValueTaskReturningThunk(Continuation continuation)
        {
            Continuation finalContinuation = new Continuation
            {
                Flags = CorInfoContinuationFlags.CORINFO_CONTINUATION_NEEDS_EXCEPTION,
            };
            continuation.Next = finalContinuation;

            while (true)
            {
                Continuation headContinuation = UnlinkHeadContinuation(out var awaitableProxy);
                await awaitableProxy;
                Continuation? finalResult = DispatchContinuations(headContinuation);
                if (finalResult != null)
                {
                    Debug.Assert(finalResult == finalContinuation);
                    return;
                }
            }
        }

        // Return a continuation object if that is the one which has the final
        // result of the Task, if the real output of the series of continuations was
        // an exception, it is allowed to propagate out.
        // OR
        // return NULL to indicate that this isn't yet done.
        private static unsafe Continuation? DispatchContinuations(Continuation? continuation)
        {
            Debug.Assert(continuation != null);

            while (true)
            {
                Continuation? newContinuation;
                try
                {
                    newContinuation = continuation.Resume(continuation);
                }
                catch (Exception ex)
                {
                    continuation = UnwindToPossibleHandler(continuation);
                    if (continuation.Resume == null)
                    {
                        throw;
                    }

                    continuation.GCData![(continuation.Flags & CorInfoContinuationFlags.CORINFO_CONTINUATION_RESULT_IN_GCDATA) != 0 ? 1 : 0] = ex;
                    continue;
                }

                if (newContinuation != null)
                {
                    newContinuation.Next = continuation.Next;
                    return null;
                }

                continuation = continuation.Next;
                Debug.Assert(continuation != null);

                if (continuation.Resume == null)
                {
                    return continuation; // Return the result containing Continuation
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
    }
}
