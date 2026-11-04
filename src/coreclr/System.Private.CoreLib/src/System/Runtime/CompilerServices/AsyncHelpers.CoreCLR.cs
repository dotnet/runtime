// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Internal;

#if NATIVEAOT
using Internal.Runtime;
#endif

namespace System.Runtime.CompilerServices
{
    [Flags]
    // Keep in sync with CORINFO_CONTINUATION_FLAGS
    internal enum ContinuationFlags
    {
        ContinueOnThreadPool = 1 << 0,
        ContinueOnCapturedSynchronizationContext = 1 << 1,
        ContinueOnCapturedTaskScheduler = 1 << 2,
        // This is an await of valueTask.AsTask() (e.g. valueTask.AsTask()
        // returned from an async version). This flag affects how
        // ValueTaskSourceContinuation handling computes the flags to pass to
        // IValueTaskSource.OnCompleted.
        ValueTaskAdaptedToTask = 1 << 3,

        AllContinuationFlags = ContinueOnThreadPool | ContinueOnCapturedSynchronizationContext | ContinueOnCapturedTaskScheduler,

        // The flags encode where in the continuation various members are stored.
        // If the encoded index is 0, it means no such member is present.
        // Otherwise the exact offset of the member is computed as
        //   DataOffset + (index - 1) * PointerSize
        //
        ExecutionContextIndexFirstBit = 4,
        ExecutionContextIndexNumBits = 2,

        ContinuationContextIndexFirstBit = 6,
        ContinuationContextIndexNumBits = 2,

        ExceptionIndexFirstBit = 8,
        ExceptionIndexNumBits = 3,

        // For JIT, the continuation stores space for every possible type of
        // async callee's result. We need to represent the offset to each of
        // these, so we allocate the rest of the bits for this.
        ResultIndexFirstBit = 11,
        ResultIndexNumBits = 20,

        // The suspending frame had already been resumed by the dispatcher, so it left
        // capturing its ExecutionContext and (when it has an index) its continuation
        // context to the dispatcher, which does that when the frame returns this
        // continuation. See FinishSuspensionInDispatcher.
        FinishSuspensionInDispatcher = 1 << 31,
    }

    // Keep in sync with CORINFO_AsyncResumeInfo in corinfo.h
    internal unsafe struct ResumeInfo
    {
        public delegate*<Continuation, ref byte, Continuation?> Resume;
        // IP to use for diagnostics. Can be null for hand-rolled continuations.
        // For normal JIT-created continuations this points into the jitted
        // suspension code.
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

        // See note in ContinuationFlags above for the computation of these offsets.

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe object GetContinuationContext()
        {
            const uint mask = (1u << (int)ContinuationFlags.ContinuationContextIndexNumBits) - 1;
            uint index = ((uint)Flags >> (int)ContinuationFlags.ContinuationContextIndexFirstBit) & mask;
            Debug.Assert(index != 0);
            ref byte data = ref RuntimeHelpers.GetRawData(this);
            return Unsafe.As<byte, object>(ref Unsafe.Add(ref data, (DataOffset - PointerSize) + index * PointerSize));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref object? GetContinuationContextRefOrNull()
        {
            const uint mask = (1u << (int)ContinuationFlags.ContinuationContextIndexNumBits) - 1;
            uint index = ((uint)Flags >> (int)ContinuationFlags.ContinuationContextIndexFirstBit) & mask;
            if (index == 0)
                return ref Unsafe.NullRef<object?>();
            ref byte data = ref RuntimeHelpers.GetRawData(this);
            return ref Unsafe.As<byte, object?>(ref Unsafe.Add(ref data, (DataOffset - PointerSize) + index * PointerSize));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref ExecutionContext? GetExecutionContextRef()
        {
            const uint mask = (1u << (int)ContinuationFlags.ExecutionContextIndexNumBits) - 1;
            uint index = ((uint)Flags >> (int)ContinuationFlags.ExecutionContextIndexFirstBit) & mask;
            Debug.Assert(index != 0);
            ref byte data = ref RuntimeHelpers.GetRawData(this);
            return ref Unsafe.As<byte, ExecutionContext?>(ref Unsafe.Add(ref data, (DataOffset - PointerSize) + index * PointerSize));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasException()
        {
            const uint mask = (1u << (int)ContinuationFlags.ExceptionIndexNumBits) - 1;
            return ((uint)Flags & (mask << (int)ContinuationFlags.ExceptionIndexFirstBit)) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryGetExecutionContext(out ExecutionContext? execContext)
        {
            const uint mask = (1u << (int)ContinuationFlags.ExecutionContextIndexNumBits) - 1;
            uint index = ((uint)Flags >> (int)ContinuationFlags.ExecutionContextIndexFirstBit) & mask;
            if (index == 0)
            {
                execContext = null;
                return false;
            }

            Debug.Assert(index != 0);
            ref byte data = ref RuntimeHelpers.GetRawData(this);
            execContext = Unsafe.As<byte, ExecutionContext?>(ref Unsafe.Add(ref data, (DataOffset - PointerSize) + index * PointerSize));
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception ex)
        {
            const uint mask = (1u << (int)ContinuationFlags.ExceptionIndexNumBits) - 1;
            uint index = ((uint)Flags >> (int)ContinuationFlags.ExceptionIndexFirstBit) & mask;
            Debug.Assert(index != 0);
            ref byte data = ref RuntimeHelpers.GetRawData(this);
            Unsafe.As<byte, Exception>(ref Unsafe.Add(ref data, (DataOffset - PointerSize) + index * PointerSize)) = ex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref byte GetResultStorageOrNull()
        {
            const uint mask = (1u << (int)ContinuationFlags.ResultIndexNumBits) - 1;
            uint index = ((uint)Flags >> (int)ContinuationFlags.ResultIndexFirstBit) & mask;
            if (index == 0)
                return ref Unsafe.NullRef<byte>();
            ref byte data = ref RuntimeHelpers.GetRawData(this);
            return ref Unsafe.Add(ref data, (DataOffset - PointerSize) + index * PointerSize);
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
#if TARGET_64BIT
        [FieldOffset(16)]
#else
        [FieldOffset(8)]
#endif
        // The runtime async Task being dispatched.
        // This is used by debuggers in the case of nested dispatcher info (multiple runtime-async Tasks on the same thread)
        // to match an inflight Task to the corresponding Continuation chain.
        public Task? CurrentTask;

#if TARGET_64BIT
        [FieldOffset(24)]
#else
        [FieldOffset(12)]
#endif
        public AsyncProfiler.Info AsyncProfilerInfo;

        // Information about current task dispatching, to be used for async
        // stackwalking.
        [ThreadStatic]
        internal static unsafe AsyncDispatcherInfo* t_current; // Debugger depends on the exact name of this field.
    }

    public static partial class AsyncHelpers
    {
#if FEATURE_INTERPRETER
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern Continuation? ResumeInterpreterContinuation(Continuation cont, ref byte resultStorage);
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

        // Indicate that an upcoming await should be done as a "tail await" that does not introduce a new suspension point.
        [Intrinsic]
        private static void TailAwait() => throw new UnreachableException();

        // This is state used by suspension/resumption machinery and stored in
        // the two places that initiate runtime async chains: either a
        // task-returning thunk, or DispatchContinuations. A pointer to this
        // state is kept in the runtime async TLS. This storage method avoids
        // costly write barriers on the hot path of suspension/resumption.
        [NonVersionable]
        private ref struct RuntimeAsyncStackState
        {
            // The introducer of asynchrony into the chain of awaits, i.e. what the chain of continuations is
            // logically attached to. How it's interpreted is determined by NotifierKind:
            // - CriticalNotifier: an ICriticalNotifyCompletion.
            // - Notifier: an INotifyCompletion.
            // - ValueTaskSource: the IValueTaskSource being awaited. The rest of what's needed to await it is
            //   stored in the RuntimeAsyncAwaitState.
            // - AwaitedTask: the Task being awaited. The RuntimeAsyncTask registers itself directly as the
            //   awaited task's continuation, and AwaitedContinuationContext (encoded as described by
            //   EncodeAwaitedContinuationContext) describes where to resume after it.
            // - Yield: unused; the suspension is for "await Task.Yield()".
            // - AwaiterContinuation: unused; AwaiterContinuation and AwaiterOffset describe the awaiter.
            public object? Notifier;
            public object? AwaitedContinuationContext;

            public delegate*<Continuation, int, Action, void> AwaiterContinuation;
            public int AwaiterOffset;
            public SuspensionNotifierKind NotifierKind;

            // When we suspend in the leaf, the contexts are captured into these fields (except when awaiting a
            // Task, whose notification doesn't depend on them).
            public ExecutionContext? LeafExecutionContext;
            public SynchronizationContext? LeafSynchronizationContext;

            public unsafe RuntimeAsyncStackState* Next;
        }

        private enum SuspensionNotifierKind
        {
            None,
            AwaitedTask,
            ValueTaskSource,
            CriticalNotifier,
            Notifier,
            Yield,
            AwaiterContinuation,
        }

        // Used during suspensions to hold the continuation chain and on what we are waiting.
        // Methods like CreateRuntimeAsyncTask will unlink the state and wrap into a Task.
        [NonVersionable]
        private unsafe struct RuntimeAsyncAwaitState
        {
            public Continuation? SentinelContinuation;
            public ValueTaskSourceContinuation? CachedValueTaskSourceContinuation;

            // When suspending on an IValueTaskSource (RuntimeAsyncStackState.ValueTaskSource is non-null), these
            // describe how to await it. They're only valid until the suspension is handled.
            public delegate*<object, Action<object?>, object?, short, ValueTaskSourceOnCompletedFlags, void> ValueTaskSourceOnCompleted;
            public delegate*<object, short, ref byte, void> ValueTaskSourceGetResult;
            public short ValueTaskSourceToken;
            public ContinuationFlags ValueTaskSourceFlags;

            // We cache the thread here to avoid unnecessary repeated TLS lookups.
            public Thread? CurrentThread;

            public RuntimeAsyncStackState* StackState;

            public void CaptureContexts()
            {
                // CaptureContext is called from leaf await helpers. We either just started a runtime async chain
                // (from a thunk), or we came from DispatchContinuations (on resumption).
                // Both cases have already initialized CurrentThread.
                Thread? curThread = CurrentThread;
                RuntimeAsyncStackState* stackState = StackState;
                Debug.Assert(curThread != null);
                Debug.Assert(stackState != null);
                // Here we get the execution context for presenting to the notifier,
                // not for flowing across suspension to potentially another thread.
                // Therefore we do not need to worry about IsFlowSuppressed
                stackState->LeafExecutionContext = curThread._executionContext;
                stackState->LeafSynchronizationContext = curThread._synchronizationContext;
            }

            // At the start of an async chain (task-returning thunk or DispatchContinuations) this function
            // is called
            [NonVersionable]
            public void Push(RuntimeAsyncStackState* stackState)
            {
                stackState->NotifierKind = SuspensionNotifierKind.None;
                stackState->Next = StackState;
                StackState = stackState;
                CurrentThread ??= Thread.CurrentThread;
            }

            // This function is called at the end of an async chain
            [NonVersionable]
            public void Pop()
            {
                Debug.Assert(CurrentThread != null);
                StackState = StackState->Next;
            }
        }

        private struct AsyncContexts
        {
            private SynchronizationContext? _synchronizationContext;
            private ExecutionContext? _executionContext;

            public AsyncContexts(Thread thread)
            {
                _synchronizationContext = thread._synchronizationContext;
                _executionContext = thread._executionContext;
            }

            public void Pop(Thread thread)
            {
                // The common case is that these have not changed, so avoid the cost of a write barrier if not needed.
                if (_synchronizationContext != thread._synchronizationContext)
                {
                    // Restore changed SynchronizationContext back to previous
                    thread._synchronizationContext = _synchronizationContext;
                }

                ExecutionContext? currentExecutionCtx = thread._executionContext;
                if (_executionContext != currentExecutionCtx)
                {
                    ExecutionContext.RestoreChangedContextToThread(thread, _executionContext, currentExecutionCtx);
                }
            }
        }

        [ThreadStatic]
        private static RuntimeAsyncAwaitState t_runtimeAsyncAwaitState;

#if !NATIVEAOT
        [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "AsyncHelpers_AddContinuationToExInternal")]
        private static unsafe partial void AddContinuationToExInternal(void* diagnosticIP, ObjectHandleOnStack ex);

        internal static unsafe void AddContinuationToExInternal(void* diagnosticIP, Exception e)
            => AddContinuationToExInternal(diagnosticIP, ObjectHandleOnStack.Create(ref e));
#endif

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

        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.Async)]
        private static unsafe void Suspend(Task task, ConfigureAwaitOptions options)
        {
            ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
            Continuation? sentinelContinuation = state.SentinelContinuation ??= new Continuation();
            SetAwaitedTask(state.StackState, task, options);
            AsyncSuspend(sentinelContinuation);
        }

        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.Async)]
        private static unsafe T Suspend<T>(Task<T> task, ConfigureAwaitOptions options)
        {
            ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
            Continuation? sentinelContinuation = state.SentinelContinuation ??= new Continuation();
            SetAwaitedTask(state.StackState, task, options);
            AsyncSuspend(sentinelContinuation);
            return default!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void SetAwaitedTask(RuntimeAsyncStackState* stackState, Task task, ConfigureAwaitOptions options)
        {
            object? encodedContext;
            if ((options & ConfigureAwaitOptions.ContinueOnCapturedContext) == 0)
            {
                // Nothing to capture; continue on the thread pool.
                encodedContext = (options & ConfigureAwaitOptions.SuppressThrowing) != 0 ?
                    SuppressThrowingAwaitContext.ThreadPool :
                    TaskScheduler.Default;
            }
            else
            {
                encodedContext = CaptureEncodedAwaitedContinuationContext(options);
            }

            stackState->Notifier = task;
            stackState->AwaitedContinuationContext = encodedContext;
            stackState->NotifierKind = SuspensionNotifierKind.AwaitedTask;
        }

        private static object? CaptureEncodedAwaitedContinuationContext(ConfigureAwaitOptions options)
        {
            object? continuationContext = null;
            ContinuationFlags flags = 0;
            CaptureContinuationContext(ref continuationContext, ref flags);

            object? encodedContext = EncodeAwaitedContinuationContext(continuationContext, flags);
            if ((options & ConfigureAwaitOptions.SuppressThrowing) != 0)
            {
                encodedContext = SuppressThrowingAwaitContext.Create(encodedContext);
            }

            return encodedContext;
        }

        // Wraps the encoded continuation context of a (non-generic) Task await configured with
        // ConfigureAwaitOptions.SuppressThrowing, such that a failed awaited task isn't propagated.
        private sealed class SuppressThrowingAwaitContext
        {
            public static readonly SuppressThrowingAwaitContext Transparent = new(null);
            public static readonly SuppressThrowingAwaitContext ThreadPool = new(TaskScheduler.Default);

            public readonly object? EncodedContext;

            private SuppressThrowingAwaitContext(object? encodedContext) => EncodedContext = encodedContext;

            public static SuppressThrowingAwaitContext Create(object? encodedContext) =>
                encodedContext is null ? Transparent :
                encodedContext == TaskScheduler.Default ? ThreadPool :
                new SuppressThrowingAwaitContext(encodedContext);
        }

        // A successfully completed task standing in for a failed task awaited with ConfigureAwaitOptions.SuppressThrowing,
        // such that the failure isn't propagated, while preserving the failed task for diagnostics.
        private sealed class SuppressedAwaitedTask : Task
        {
            public readonly Task AwaitedTask;

            public SuppressedAwaitedTask(Task awaitedTask) : base(canceled: false, TaskCreationOptions.None, CancellationToken.None) =>
                AwaitedTask = awaitedTask;
        }

        // Encodes where to continue after an awaited Task completes into a single object, so that
        // RuntimeAsyncTask needs only one field for it:
        //   null                   => no continuation flags (transparent)
        //   TaskScheduler.Default  => ContinuationFlags.ContinueOnThreadPool (CaptureContinuationContext
        //                             never captures the default scheduler, so it's free to use as a marker)
        //   SynchronizationContext => ContinuationFlags.ContinueOnCapturedSynchronizationContext
        //   TaskScheduler          => ContinuationFlags.ContinueOnCapturedTaskScheduler
        // SetAwaitedTask may additionally wrap the result in a SuppressThrowingAwaitContext.
        private static object? EncodeAwaitedContinuationContext(object? continuationContext, ContinuationFlags flags)
        {
            Debug.Assert((flags & ~ContinuationFlags.AllContinuationFlags) == 0);

            if ((flags & ContinuationFlags.ContinueOnThreadPool) != 0)
            {
                return TaskScheduler.Default;
            }

            Debug.Assert(flags == 0 ? continuationContext is null :
                (flags & ContinuationFlags.ContinueOnCapturedSynchronizationContext) != 0 ? continuationContext is SynchronizationContext :
                continuationContext is TaskScheduler && continuationContext != TaskScheduler.Default);
            return continuationContext;
        }

        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.Async)]
        private static unsafe void TransparentSuspend(IValueTaskSource source, short token)
        {
            ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
            Continuation? sentinelContinuation = state.SentinelContinuation ??= new Continuation();

            SetValueTaskSource(ref state, source, token, 0, &ValueTaskSourceContinuation.OnCompleted, &ValueTaskSourceContinuation.GetResult);
            state.CaptureContexts();
            AsyncSuspend(sentinelContinuation);
        }

        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.Async)]
        private static unsafe void Suspend(IValueTaskSource source, short token, bool continueOnCapturedContext)
        {
            ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
            Continuation? sentinelContinuation = state.SentinelContinuation ??= new Continuation();

            ContinuationFlags flags = GetValueTaskSourceContinuationFlags(continueOnCapturedContext, state.CurrentThread!);
            SetValueTaskSource(ref state, source, token, flags, &ValueTaskSourceContinuation.OnCompleted, &ValueTaskSourceContinuation.GetResult);
            state.CaptureContexts();
            AsyncSuspend(sentinelContinuation);
        }

        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.Async)]
        private static unsafe T TransparentSuspend<T>(IValueTaskSource<T> source, short token)
        {
            ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
            Continuation? sentinelContinuation = state.SentinelContinuation ??= new Continuation();

            SetValueTaskSource(ref state, source, token, 0, &ValueTaskSourceContinuation.OnCompleted<T>, &ValueTaskSourceContinuation.GetResult<T>);
            state.CaptureContexts();
            AsyncSuspend(sentinelContinuation);
            return default!;
        }

        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.Async)]
        private static unsafe T Suspend<T>(IValueTaskSource<T> source, short token, bool continueOnCapturedContext)
        {
            ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
            Continuation? sentinelContinuation = state.SentinelContinuation ??= new Continuation();

            ContinuationFlags flags = GetValueTaskSourceContinuationFlags(continueOnCapturedContext, state.CurrentThread!);
            SetValueTaskSource(ref state, source, token, flags, &ValueTaskSourceContinuation.OnCompleted<T>, &ValueTaskSourceContinuation.GetResult<T>);
            state.CaptureContexts();
            AsyncSuspend(sentinelContinuation);
            return default!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void SetValueTaskSource(
            ref RuntimeAsyncAwaitState state,
            object source,
            short token,
            ContinuationFlags flags,
            delegate*<object, Action<object?>, object?, short, ValueTaskSourceOnCompletedFlags, void> onCompleted,
            delegate*<object, short, ref byte, void> getResult)
        {
            state.StackState->Notifier = source;
            state.StackState->NotifierKind = SuspensionNotifierKind.ValueTaskSource;
            state.ValueTaskSourceToken = token;
            state.ValueTaskSourceFlags = flags;
            state.ValueTaskSourceOnCompleted = onCompleted;
            state.ValueTaskSourceGetResult = getResult;
        }

        private static ContinuationFlags GetValueTaskSourceContinuationFlags(bool continueOnCapturedContext, Thread currentThread)
        {
            if (!continueOnCapturedContext)
            {
                return ContinuationFlags.ContinueOnThreadPool;
            }

            // We only need to capture flags.
            // If needed, VTS will use the scheduling context captured in the "state".
            ContinuationFlags flags = 0;
            CaptureContinuationContextFlags(ref flags, currentThread);
            return flags;
        }

        /// <summary>
        /// Used by internal thunks that implement awaiting on Task.
        /// </summary>
        /// <param name="task">Task whose completion we are awaiting.</param>
        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.Async)]
        private static unsafe void TransparentSuspend(Task task)
        {
            ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
            Continuation? sentinelContinuation = state.SentinelContinuation ??= new Continuation();

            RuntimeAsyncStackState* stackState = state.StackState;
            stackState->Notifier = task;
            stackState->AwaitedContinuationContext = null;
            stackState->NotifierKind = SuspensionNotifierKind.AwaitedTask;

            AsyncSuspend(sentinelContinuation);
        }

        /// <summary>
        /// Used by internal thunks that implement awaiting on Task.
        /// </summary>
        /// <param name="task">Task whose completion we are awaiting.</param>
        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.Async)]
        private static unsafe T TransparentSuspend<T>(Task<T> task)
        {
            ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
            Continuation? sentinelContinuation = state.SentinelContinuation ??= new Continuation();

            RuntimeAsyncStackState* stackState = state.StackState;
            stackState->Notifier = task;
            stackState->AwaitedContinuationContext = null;
            stackState->NotifierKind = SuspensionNotifierKind.AwaitedTask;

            AsyncSuspend(sentinelContinuation);
            return default!;
        }

        // The next four overloads 'TransparentAwait' are called by the JIT in
        // async versions of task-returning methods to await the task that
        // would normally be returned.
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.Async)]
        private static void TransparentAwait(Task task)
        {
            if (!task.IsCompleted)
            {
                TailAwait();
                TransparentSuspend(task);
                return;
            }

            TaskAwaiter.ValidateEnd(task);
        }

        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.Async | MethodImplOptions.AggressiveInlining)]
        private static void TransparentAwait(ValueTask task)
        {
            object? obj = task._obj;
            if (obj == null)
            {
                return;
            }

            if (obj is Task t)
            {
                if (!t.IsCompleted)
                {
                    TailAwait();
                    TransparentSuspend(t);
                    return;
                }

                TaskAwaiter.ValidateEnd(t);
                return;
            }

            Debug.Assert(obj is IValueTaskSource);
            IValueTaskSource vts = Unsafe.As<object, IValueTaskSource>(ref obj);
            if (vts.GetStatus(task._token) == ValueTaskSourceStatus.Pending)
            {
                TailAwait();
                TransparentSuspend(vts, task._token);
                return;
            }

            vts.GetResult(task._token);
        }

        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.Async)]
        private static T TransparentAwait<T>(Task<T> task)
        {
            if (!task.IsCompleted)
            {
                TailAwait();
                return TransparentSuspend(task);
            }

            TaskAwaiter.ValidateEnd(task);
            return task.ResultOnSuccess;
        }

        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.Async | MethodImplOptions.AggressiveInlining)]
        private static T TransparentAwait<T>(ValueTask<T> task)
        {
            object? obj = task._obj;
            if (obj == null)
            {
                return task._result!;
            }

            if (obj is Task<T> t)
            {
                if (!t.IsCompleted)
                {
                    TailAwait();
                    return TransparentSuspend(t);
                }

                TaskAwaiter.ValidateEnd(t);
                return t.ResultOnSuccess;
            }

            Debug.Assert(obj is IValueTaskSource<T>);
            IValueTaskSource<T> vts = Unsafe.As<object, IValueTaskSource<T>>(ref obj);
            if (vts.GetStatus(task._token) == ValueTaskSourceStatus.Pending)
            {
                TailAwait();
                return TransparentSuspend(vts, task._token);
            }

            return vts.GetResult(task._token);
        }

        // Represents execution of a chain of suspended and resuming runtime
        // async functions.
        private class RuntimeAsyncTask<T> : Task<T>
        {
            // The base Task's m_action field (unused for invoking a delegate by this promise-style task)
            // stores state about what this task is awaiting, avoiding the need for an additional field.
            // When suspended on a Task, this RuntimeAsyncTask is registered directly as that task's
            // continuation (rather than via an intermediate continuation object). While registered,
            // the field holds where to resume, encoded as described by EncodeAwaitedContinuationContext.
            // Once the awaited task completes, it instead holds that completed task until the
            // dispatcher consumes it. When suspended on an IValueTaskSource, it holds the
            // ValueTaskSourceContinuation describing the source until the dispatcher consumes it.
            // Otherwise, it's null or holds the Action cached by GetContinuationAction.

            public RuntimeAsyncTask()
            {
                // We use the base Task's state object field to store the Continuation while posting the task around.
                // Ensure that state object isn't published out for others to see.
                Debug.Assert((m_stateFlags & (int)InternalTaskOptions.PromiseTask) != 0, "Expected state flags to already be configured.");
                Debug.Assert(m_stateObject is null, "Expected to be able to use the state object field for Continuation.");
                m_stateFlags |= (int)(InternalTaskOptions.HiddenState | InternalTaskOptions.RuntimeAsyncTask);
            }

            internal override void ExecuteDirectly(Thread? threadPoolThread)
            {
                DispatchContinuations();
            }

            // Invoked by the awaited task's completion (Task.RunContinuations) or directly when the
            // awaited task had already completed by the time we tried to register.
            internal override void ExecuteRuntimeAsyncAwaitContinuation(Task completedTask, bool canInline)
            {
                // Swap the continuation context for the completed task before possibly handing this
                // task to another thread, which will consume it in DispatchContinuations.
                object? continuationContext = m_action;
                m_action = completedTask;

                // When not inlining, a TaskScheduler.Default context needs no further processing before queueing.
                if (continuationContext is not null &&
                    (canInline || continuationContext != TaskScheduler.Default) &&
                    QueueAwaitContinuationIfNecessary(continuationContext, canInline))
                {
                    return;
                }

                if (canInline)
                {
                    DispatchContinuations();
                }
                else
                {
                    ThreadPool.UnsafeQueueUserWorkItemInternal(this, preferLocal: true);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool QueueAwaitContinuationIfNecessary(object continuationContext, bool canInline)
            {
                // Fast path for the common case of continuing on the thread pool (including a successful
                // SuppressThrowing await) from a thread pool thread that's not running a Task and has no
                // SynchronizationContext, in which case the continuation can be invoked inline. A successful
                // SuppressThrowing await without a context needs nothing at all.
                if (continuationContext == SuppressThrowingAwaitContext.Transparent)
                {
                    if (Unsafe.As<Task>(m_action!).IsCompletedSuccessfully)
                    {
                        return false;
                    }
                }
                else if (canInline &&
                    (continuationContext == TaskScheduler.Default ||
                     (continuationContext == SuppressThrowingAwaitContext.ThreadPool && Unsafe.As<Task>(m_action!).IsCompletedSuccessfully)) &&
                    Thread.CurrentThread._synchronizationContext is null &&
                    Task.t_currentTask is null)
                {
                    return false;
                }

                return QueueAwaitContinuationIfNecessarySlow(continuationContext, canInline);
            }

            private bool QueueAwaitContinuationIfNecessarySlow(object continuationContext, bool canInline)
            {
                if (continuationContext == TaskScheduler.Default)
                {
                    if (!canInline)
                    {
                        ThreadPool.UnsafeQueueUserWorkItemInternal(this, preferLocal: true);
                        return true;
                    }

                    SynchronizationContext? ctx = Thread.CurrentThread._synchronizationContext;
                    if (ctx == null || ctx.GetType() == typeof(SynchronizationContext))
                    {
                        TaskScheduler? sched = GetCurrentTaskScheduler();
                        if (sched == null || sched == TaskScheduler.Default)
                        {
                            // Can inline
                            return false;
                        }
                    }

                    ThreadPool.UnsafeQueueUserWorkItemInternal(this, preferLocal: true);
                    return true;
                }

                if (continuationContext is SuppressThrowingAwaitContext suppressThrowingContext)
                {
                    // Like TaskAwaiter.ValidateEnd with SuppressThrowing, observe any failure, and have the
                    // dispatcher complete the await from a successful task so that nothing is thrown.
                    Debug.Assert(m_action is Task { IsCompleted: true });
                    Task awaitedTask = Unsafe.As<Task>(m_action);
                    if (!awaitedTask.IsCompletedSuccessfully)
                    {
                        awaitedTask.NotifyDebuggerOfWaitCompletionIfNecessary();
                        awaitedTask.MarkExceptionsAsHandled();

                        // When async instrumentation is active, preserve the identity of the awaited task so
                        // that the wait can still be correlated when the continuation resumes.
                        m_action = AsyncInstrumentation.IsActive ? new SuppressedAwaitedTask(awaitedTask) : Task.CompletedTask;
                    }

                    return suppressThrowingContext.EncodedContext is { } encodedContext &&
                        QueueAwaitContinuationIfNecessarySlow(encodedContext, canInline);
                }

                if (continuationContext is TaskScheduler sched2)
                {
                    TaskSchedulerAwaitTaskContinuation.RunOrScheduleAction(s_dispatchCallback, this, sched2, capturedContext: null, allowInlining: canInline);
                    return true;
                }

                Debug.Assert(continuationContext is SynchronizationContext { });
                SynchronizationContext continuationSyncCtx = Unsafe.As<SynchronizationContext>(continuationContext);

                if (canInline && continuationSyncCtx == Thread.CurrentThread._synchronizationContext)
                {
                    return false;
                }

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

            // Takes the awaited Task or ValueTaskSourceContinuation (if this dispatch is resuming from
            // such an await), clearing the field so that it isn't retained.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private object? MoveAwaitedState()
            {
                object? awaitedState = m_action;
                if (awaitedState != null)
                {
                    Debug.Assert(awaitedState is Task or ValueTaskSourceContinuation or Action);
                    if (awaitedState is ValueTaskSourceContinuation vtsCont)
                    {
                        // The ValueTaskSourceContinuation stays installed for reuse by subsequent awaits.
                        return vtsCont.IsPending ? vtsCont : null;
                    }

                    if (awaitedState is Action)
                    {
                        // The cached continuation action stays installed for reuse by subsequent awaits.
                        return null;
                    }

                    m_action = null;
                }

                return awaitedState;
            }

            // Removes any ValueTaskSourceContinuation kept installed for reuse, returning it to the thread's cache.
            private void ReleaseValueTaskSourceContinuation(ref RuntimeAsyncAwaitState awaitState)
            {
                if (m_action is ValueTaskSourceContinuation vtsCont)
                {
                    Debug.Assert(!vtsCont.IsPending);
                    m_action = null;
                    vtsCont.Source = null;
                    awaitState.CachedValueTaskSourceContinuation = vtsCont;
                }
            }

            // Completes the await of the awaited Task or IValueTaskSource by propagating its result into the
            // storage of the continuation that awaited it. If the awaited operation didn't complete successfully,
            // returns the exception the await must propagate (or throws it).
            [StackTraceHidden]
            private static Exception? GetAwaitedResult(ref RuntimeAsyncAwaitState awaitState, object awaitedState, Continuation awaitingContinuation)
            {
                if (awaitedState is ValueTaskSourceContinuation vtsCont)
                {
                    vtsCont.GetResult(ref awaitingContinuation.GetResultStorageOrNull());
                    return null;
                }

                return GetAwaitedTaskResult(Unsafe.As<Task>(awaitedState), awaitingContinuation);
            }

            // Completes the await of the awaited task by propagating its result into the storage of the
            // continuation that awaited it. If the awaited task didn't complete successfully, returns the
            // exception the await must propagate (or throws it, if it can't be propagated without throwing).
            [StackTraceHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static unsafe Exception? GetAwaitedTaskResult(Task awaitedTask, Continuation awaitingContinuation)
            {
                ref byte resultStorage = ref awaitingContinuation.GetResultStorageOrNull();
                if (Unsafe.IsNullRef(ref resultStorage) && !awaitedTask.IsWaitNotificationEnabledOrNotRanToCompletion)
                {
                    // Successfully completed and there's no result to store.
                    return null;
                }

                Exception? ex = awaitedTask.GetRuntimeAsyncAwaitResult(ref resultStorage);
                if (ex is not null && (awaitingContinuation.ResumeInfo == null || awaitingContinuation.ResumeInfo->DiagnosticIP == null))
                {
                    // Restoring the exception's dispatch state is only completed by appending a frame to its
                    // stack trace, which UnwindToPossibleHandler won't immediately do for this continuation.
                    throw ex;
                }

                return ex;
            }

            // Lazily created, as it's only needed when suspending on something other than a Task or IValueTaskSource.
            // It's cached in m_action until that's next needed to store awaited state.
            private Action GetContinuationAction() => m_action as Action ?? CreateContinuationAction();

            private Action CreateContinuationAction()
            {
                Debug.Assert(m_action is null);
                Action action = DispatchContinuations;
                m_action = action;
                return action;
            }

            private static readonly Action<object?> s_dispatchCallback = static state =>
            {
                Debug.Assert(state is RuntimeAsyncTask<T>);
                try
                {
                    Unsafe.As<RuntimeAsyncTask<T>>(state).DispatchContinuations();
                }
                catch (Exception exception)
                {
                    Task.ThrowAsync(exception, targetContext: null);
                }
            };

            // Completes the operation with the result stored in m_result. The result was already written
            // there by the final continuation and nothing else can complete this task, so the result-less
            // completion can be used, publishing the result with a single atomic state transition.
            internal virtual bool TrySetRuntimeAsyncResult() => TrySetResult();

            // Completes the operation with the specified exception (canceled if it's an OperationCanceledException).
            internal virtual bool TrySetRuntimeAsyncException(Exception ex) =>
                ex is OperationCanceledException oce ?
                    TrySetCanceled(oce.CancellationToken, oce) :
                    TrySetException(ex);

            private Continuation MoveContinuationState()
            {
                // The continuation is left installed while dispatching; when this task suspends again it's commonly
                // on the same (reused) continuation, which then needn't be stored again. It's cleared upon completion.
                object? stateObject = m_stateObject;
                Debug.Assert(stateObject is Continuation);
                return Unsafe.As<Continuation>(stateObject);
            }

            private void SetContinuationState(Continuation value)
            {
                if (m_stateObject != value)
                {
                    m_stateObject = value;
                }
            }

            // Handles the common suspension on a Task without the cost of a call to HandleSuspended, when this task isn't
            // holding other awaited state. Returns false, having changed nothing, if HandleSuspended is needed instead.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal unsafe bool TryHandleSuspendedOnTask(ref RuntimeAsyncAwaitState state, RuntimeAsyncStackState* stackState)
            {
                if (stackState->NotifierKind != SuspensionNotifierKind.AwaitedTask || m_action is not null)
                {
                    return false;
                }

                Continuation sentinelContinuation = state.SentinelContinuation!;
                Continuation headContinuation = sentinelContinuation.Next!;
                sentinelContinuation.Next = null;

                SetContinuationState(headContinuation);

                Debug.Assert(stackState->Notifier is Task);
                Task awaitedTask = Unsafe.As<Task>(stackState->Notifier!);
                m_action = GetAwaitedTaskContinuationContext(headContinuation, stackState->AwaitedContinuationContext);
                if (!awaitedTask.TryAddFirstTaskContinuation(this))
                {
                    AddAwaitedTaskContinuation(awaitedTask);
                }

                return true;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void AddAwaitedTaskContinuation(Task awaitedTask)
            {
                try
                {
                    if (!awaitedTask.AddTaskContinuation(this, addBeforeOthers: false))
                    {
                        ExecuteRuntimeAsyncAwaitContinuation(awaitedTask, canInline: false);
                    }
                }
                catch (Exception ex)
                {
                    Task.ThrowAsync(ex, targetContext: null);
                }
            }

            // Continuing on the thread pool needs no context object, so when the awaiting frame doesn't
            // already specify where to continue, move that onto its continuation, as for an await the JIT
            // recognized. The dispatcher then checks it on the thread it's already running on, rather than
            // the awaited task's completion separately checking it beforehand.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static object? GetAwaitedTaskContinuationContext(Continuation headContinuation, object? awaitedContinuationContext)
            {
                if ((awaitedContinuationContext == TaskScheduler.Default || awaitedContinuationContext == SuppressThrowingAwaitContext.ThreadPool) &&
                    (headContinuation.Flags & (ContinuationFlags.AllContinuationFlags | ContinuationFlags.ValueTaskAdaptedToTask)) == 0)
                {
                    headContinuation.Flags |= ContinuationFlags.ContinueOnThreadPool;
                    awaitedContinuationContext = awaitedContinuationContext == TaskScheduler.Default ? null : SuppressThrowingAwaitContext.Transparent;
                }

                return awaitedContinuationContext;
            }

            internal unsafe bool HandleSuspended(ref RuntimeAsyncAwaitState state)
            {
                Thread? currentThread = state.CurrentThread;
                Debug.Assert(currentThread != null);

                RuntimeAsyncStackState* stackState = state.StackState;
                SuspensionNotifierKind notifierKind = stackState->NotifierKind;

                // Restore the contexts captured at the leaf so the notifier observes them. Registering with an
                // awaited Task runs no user code and doesn't consult the thread's contexts (where to resume is
                // already encoded in AwaitedContinuationContext), so the leaf doesn't capture them in that case.
                // Either way, the caller restores the thread's contexts once we return.
                if (notifierKind != SuspensionNotifierKind.AwaitedTask)
                {
                    ExecutionContext? suspendingExecutionContext = stackState->LeafExecutionContext;
                    SynchronizationContext? suspendingSyncContext = stackState->LeafSynchronizationContext;
                    if (suspendingExecutionContext != currentThread._executionContext)
                    {
                        currentThread._executionContext = suspendingExecutionContext;
                    }

                    if (suspendingSyncContext != currentThread._synchronizationContext)
                    {
                        currentThread._synchronizationContext = suspendingSyncContext;
                    }
                }

                Continuation sentinelContinuation = state.SentinelContinuation!;
                Continuation headContinuation = sentinelContinuation.Next!;
                sentinelContinuation.Next = null;

                SetContinuationState(headContinuation);

                if (m_action is not null && notifierKind != SuspensionNotifierKind.ValueTaskSource)
                {
                    // Only reuse a ValueTaskSourceContinuation for consecutive source awaits; don't keep
                    // its consumed source alive across waits on something else.
                    ReleaseValueTaskSourceContinuation(ref state);
                }

                try
                {
                    object? notifier = stackState->Notifier;
                    switch (notifierKind)
                    {
                        case SuspensionNotifierKind.AwaitedTask:
                            // Awaiting a Task. Register this task directly as its continuation; the
                            // dispatcher will fetch the awaited task's result into the head
                            // continuation when it resumes.
                            Debug.Assert(notifier is Task);
                            Task awaitedTask = Unsafe.As<Task>(notifier);
                            m_action = GetAwaitedTaskContinuationContext(headContinuation, stackState->AwaitedContinuationContext);
                            if (!awaitedTask.AddTaskContinuation(this, addBeforeOthers: false))
                            {
                                ExecuteRuntimeAsyncAwaitContinuation(awaitedTask, canInline: false);
                            }
                            break;

                        case SuspensionNotifierKind.ValueTaskSource:
                            Debug.Assert(notifier is not null);
                            SuspendOnValueTaskSource(ref state, headContinuation, notifier);
                            break;

                        case SuspensionNotifierKind.Yield:
                            // Result of async call to UnsafeAwaitAwaiter for "await Task.Yield()".
                            Debug.Assert((headContinuation.Flags & ContinuationFlags.AllContinuationFlags) == 0);
                            if (!TplEventSource.Log.IsEnabled())
                            {
                                RuntimeAsyncYielder.Yield(this);
                            }
                            else
                            {
                                RuntimeAsyncYielder.Instance.UnsafeOnCompleted(GetContinuationAction());
                            }
                            break;

                        case SuspensionNotifierKind.CriticalNotifier:
                            // Result of async call to UnsafeAwaitAwaiter.
                            // These never have special continuation context handling.
                            Debug.Assert((headContinuation.Flags & ContinuationFlags.AllContinuationFlags) == 0);
                            Debug.Assert(notifier is ICriticalNotifyCompletion);
                            Unsafe.As<ICriticalNotifyCompletion>(notifier).UnsafeOnCompleted(GetContinuationAction());
                            break;

                        case SuspensionNotifierKind.AwaiterContinuation:
                            // The awaiter is stored in the continuation for the caller of
                            // AwaitAwaiterInContinuation or UnsafeAwaitAwaiterInContinuation.
                            Debug.Assert((headContinuation.Flags & ContinuationFlags.AllContinuationFlags) == 0);
                            stackState->AwaiterContinuation(
                                headContinuation, stackState->AwaiterOffset, GetContinuationAction());
                            break;

                        default:
                            // Result of async call to AwaitAwaiter.
                            Debug.Assert(stackState->NotifierKind == SuspensionNotifierKind.Notifier);
                            Debug.Assert((headContinuation.Flags & ContinuationFlags.AllContinuationFlags) == 0);
                            Debug.Assert(notifier is INotifyCompletion);
                            Unsafe.As<INotifyCompletion>(notifier).OnCompleted(GetContinuationAction());
                            break;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Task.ThrowAsync(ex, targetContext: null);
                }

                return false;
            }

            private unsafe void SuspendOnValueTaskSource(ref RuntimeAsyncAwaitState state, Continuation headContinuation, object source)
            {
                // The ValueTaskSourceContinuation isn't linked into the continuation chain. Instead, like
                // when awaiting a Task, the dispatcher fetches the source's result directly into the head
                // continuation when it resumes. Reuse the one this task holds from a previous await, if any.
                if (m_action is not ValueTaskSourceContinuation valueTaskSourceCont)
                {
                    Debug.Assert(m_action is null or Action);
                    valueTaskSourceCont = state.CachedValueTaskSourceContinuation ?? new ValueTaskSourceContinuation();
                    state.CachedValueTaskSourceContinuation = null;
                    m_action = valueTaskSourceCont;
                }

                Debug.Assert(headContinuation != valueTaskSourceCont);
                short token = state.ValueTaskSourceToken;
                valueTaskSourceCont.Initialize(source, token, state.ValueTaskSourceGetResult);

                // The awaiter must inform the ValueTaskSource on whether the continuation
                // wants to run on a context, although the source may decide to ignore the suggestion.
                // Since the behavior of the source takes precedence, we clear the context flags of
                // the awaiting continuation (so it will run transparently on what the source decides)
                // and then tell the source if the awaiting frame prefers to continue on a context.
                // The reason why we do it here and not when the notifier is created is because
                // the continuation chain builds from the innermost frame out and at the time when the
                // notifier is created we do not know yet if the caller wants to continue on a context.
                ContinuationFlags continuationFlags = state.ValueTaskSourceFlags;
                if ((continuationFlags & ContinuationFlags.AllContinuationFlags) == 0)
                {
                    // Skip to a nontransparent/user continuation. Such continuation must exist.
                    // Since we see a VTS notifier, something was directly or indirectly
                    // awaiting either an async thunk for a ValueTask-returning method or
                    // the direct AsyncHelpers.Await(ValueTask/ValueTask<T>) path.
                    // In either case, that can only happen in nontransparent/user code.
                    Continuation contWithContinueFlags = headContinuation;
                    while ((contWithContinueFlags.Flags & (ContinuationFlags.AllContinuationFlags | ContinuationFlags.ValueTaskAdaptedToTask)) == 0 &&
                           contWithContinueFlags.Next != null)
                    {
                        contWithContinueFlags = contWithContinueFlags.Next;
                    }

                    continuationFlags = contWithContinueFlags.Flags;

                    // Clear continuation flags, so that continuation runs transparently
                    contWithContinueFlags.Flags &= ~ContinuationFlags.AllContinuationFlags;
                }

                const ContinuationFlags continueOnContextFlags =
                    ContinuationFlags.ContinueOnCapturedSynchronizationContext |
                    ContinuationFlags.ContinueOnCapturedTaskScheduler;

                ValueTaskSourceOnCompletedFlags configFlags = ValueTaskSourceOnCompletedFlags.None;
                if ((continuationFlags & continueOnContextFlags) != 0)
                {
                    // if await has captured some context, inform the source
                    configFlags = ValueTaskSourceOnCompletedFlags.UseSchedulingContext;
                }

                state.ValueTaskSourceOnCompleted(
                    source,
                    ThreadPool.s_dispatchRuntimeAsyncContinuationsCallback,
                    this,
                    token,
                    configFlags);
            }

            internal unsafe void InstrumentedHandleSuspended(AsyncInstrumentation.Flags flags, ref RuntimeAsyncAwaitState state)
            {
                if (AsyncInstrumentation.IsEnabled.AsyncDebugger(flags))
                {
                    Continuation? nextContinuation = state.SentinelContinuation!.Next;
                    RuntimeAsyncStackState* stackState = state.StackState;
                    Task? awaitedTask = stackState->NotifierKind == SuspensionNotifierKind.AwaitedTask ? Unsafe.As<Task>(stackState->Notifier) : null;
                    AsyncDebugger.HandleSuspended(this, nextContinuation, awaitedTask);

                    if (!HandleSuspended(ref state))
                    {
                        AsyncDebugger.HandleSuspendedFailed(this, nextContinuation, awaitedTask);
                    }

                    return;
                }

                HandleSuspended(ref state);
            }

#pragma warning disable CA1822 // Mark members as static
            [MethodImpl(MethodImplOptions.NoOptimization)]
            public void NotifyDebuggerOfRuntimeAsyncState()
            {
            }
#pragma warning restore CA1822

            [StackTraceHidden]
            // Diagnostic tooling depends on this name when classifying async callstack frames.
            // NOTE, any changes done to this method need to be replicated in InstrumentedDispatchContinuations as well.
            private unsafe void DispatchContinuations()
            {
                if (AsyncInstrumentation.IsActive && AsyncInstrumentation.LoadFlags(out AsyncInstrumentation.Flags flags))
                {
                    if (AsyncInstrumentation.IsEnabled.AsyncProfiler(flags) || AsyncInstrumentation.IsEnabled.AsyncDebugger(flags))
                    {
                        InstrumentedDispatchContinuations(flags);
                        return;
                    }
                }

                // Intentionally skip initialization for this state; the Push
                // call below will initialize non-GC refs, and GC refs will be
                // zeroed by prolog.
                RuntimeAsyncStackState stackState;

                ref RuntimeAsyncAwaitState awaitState = ref t_runtimeAsyncAwaitState;
                awaitState.Push(&stackState);

                AsyncContexts contexts = new AsyncContexts(awaitState.CurrentThread!);

                ref AsyncDispatcherInfo* refDispatcherInfo = ref AsyncDispatcherInfo.t_current;

                AsyncDispatcherInfo asyncDispatcherInfo;
                asyncDispatcherInfo.Next = refDispatcherInfo;
                asyncDispatcherInfo.NextContinuation = MoveContinuationState();
                asyncDispatcherInfo.CurrentTask = this;
                refDispatcherInfo = &asyncDispatcherInfo;

                object? awaitedState = MoveAwaitedState();

                while (true)
                {
                    Debug.Assert(asyncDispatcherInfo.NextContinuation != null);
                    Exception? exception = null;
                    try
                    {
                        if (awaitedState != null)
                        {
                            // Resuming from a Task or IValueTaskSource await: complete the await by storing the
                            // awaited result into the awaiting continuation, which is then resumed by the next iteration.
                            // If the awaited operation didn't complete successfully, its exception is instead
                            // propagated (generally without needing to be thrown) as if that continuation threw it.
                            object s = awaitedState;
                            awaitedState = null;
                            exception = GetAwaitedResult(ref awaitState, s, asyncDispatcherInfo.NextContinuation);
                        }
                        else
                        {
                            Continuation curContinuation = asyncDispatcherInfo.NextContinuation;
                            Continuation? nextContinuation = curContinuation.Next;
                            asyncDispatcherInfo.NextContinuation = nextContinuation;

                            Debug.Assert(awaitState.CurrentThread != null);
                            if (curContinuation.TryGetExecutionContext(out ExecutionContext? execContext))
                            {
                                RestoreExecutionContext(awaitState.CurrentThread, execContext);
                            }

                            ref byte resultLoc = ref nextContinuation != null ? ref nextContinuation.GetResultStorageOrNull() : ref GetResultStorage();

                            Continuation? newContinuation = curContinuation.ResumeInfo->Resume(curContinuation, ref resultLoc);

                            if (newContinuation != null)
                            {
                                if ((newContinuation.Flags & ContinuationFlags.FinishSuspensionInDispatcher) != 0)
                                {
                                    FinishSuspensionInDispatcher(newContinuation, awaitState.CurrentThread!);
                                }
                                if (newContinuation.Next != nextContinuation)
                                {
                                    newContinuation.Next = nextContinuation;
                                }
                                if (!TryHandleSuspendedOnTask(ref awaitState, &stackState))
                                {
                                    HandleSuspended(ref awaitState);
                                }

                                contexts.Pop(awaitState.CurrentThread!);
                                awaitState.Pop();
                                refDispatcherInfo = asyncDispatcherInfo.Next;
                                return;
                            }

                            // The frame completed, so stop keeping its continuation (and everything it captured) alive.
                            m_stateObject = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }

                    if (exception != null)
                    {
                        Exception ex = exception;
                        uint unwindedFrames = 1; // Count current frame.
                        Continuation? handlerContinuation = UnwindToPossibleHandler(asyncDispatcherInfo.NextContinuation, ex, ref unwindedFrames);
                        if (handlerContinuation == null)
                        {
                            // Tail of AsyncTaskMethodBuilderT.SetException
                            m_stateObject = null;
                            if (m_action is not null)
                            {
                                ReleaseValueTaskSourceContinuation(ref awaitState);
                            }

                            bool successfullySet = TrySetRuntimeAsyncException(ex);

                            contexts.Pop(awaitState.CurrentThread!);
                            awaitState.Pop();
                            refDispatcherInfo = asyncDispatcherInfo.Next;

                            if (!successfullySet)
                            {
                                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted);
                            }

                            return;
                        }

                        m_stateObject = null;
                        handlerContinuation.SetException(ex);
                        asyncDispatcherInfo.NextContinuation = handlerContinuation;
                    }

                    if (asyncDispatcherInfo.NextContinuation == null)
                    {
                        m_stateObject = null;
                        if (m_action is not null)
                        {
                            ReleaseValueTaskSourceContinuation(ref awaitState);
                        }

                        bool successfullySet = TrySetRuntimeAsyncResult();

                        contexts.Pop(awaitState.CurrentThread!);
                        awaitState.Pop();
                        refDispatcherInfo = asyncDispatcherInfo.Next;

                        if (!successfullySet)
                        {
                            ThrowHelper.ThrowInvalidOperationException(ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted);
                        }

                        return;
                    }

                    if (QueueContinuationFollowUpActionIfNecessary(asyncDispatcherInfo.NextContinuation, awaitState.CurrentThread!))
                    {
                        contexts.Pop(awaitState.CurrentThread!);
                        awaitState.Pop();
                        refDispatcherInfo = asyncDispatcherInfo.Next;
                        return;
                    }

                    if (AsyncInstrumentation.IsActive && AsyncInstrumentation.LoadFlags(out flags))
                    {
                        if (AsyncInstrumentation.IsEnabled.AsyncProfiler(flags) || AsyncInstrumentation.IsEnabled.AsyncDebugger(flags))
                        {
                            SetContinuationState(asyncDispatcherInfo.NextContinuation);

                            contexts.Pop(awaitState.CurrentThread!);
                            awaitState.Pop();
                            refDispatcherInfo = asyncDispatcherInfo.Next;

                            InstrumentedDispatchContinuations(flags);
                            return;
                        }
                    }
                }
            }

            [StackTraceHidden]
            // Diagnostic tooling depends on this name when classifying async callstack frames.
            private unsafe void InstrumentedDispatchContinuations(AsyncInstrumentation.Flags flags)
            {
                // Intentionally skip initialization for this state; the Push
                // call below will initialize non-GC refs, and GC refs will be
                // zeroed by prolog.
                RuntimeAsyncStackState stackState;

                ref RuntimeAsyncAwaitState awaitState = ref t_runtimeAsyncAwaitState;
                awaitState.Push(&stackState);

                AsyncContexts contexts = new AsyncContexts(awaitState.CurrentThread!);

                ref AsyncDispatcherInfo* refDispatcherInfo = ref AsyncDispatcherInfo.t_current;

                AsyncDispatcherInfo asyncDispatcherInfo;
                asyncDispatcherInfo.Next = refDispatcherInfo;
                asyncDispatcherInfo.NextContinuation = MoveContinuationState();
                refDispatcherInfo = &asyncDispatcherInfo;

                object? awaitedState = MoveAwaitedState();

                RuntimeAsyncInstrumentationHelpers.ResumeRuntimeAsyncContext(this, ref asyncDispatcherInfo, flags, awaitedState as Task);

                while (true)
                {
                    Debug.Assert(asyncDispatcherInfo.NextContinuation != null);
                    Continuation curContinuation = asyncDispatcherInfo.NextContinuation;
                    // Number of frames the current step accounts for if it throws: zero when completing
                    // the await of a Task or IValueTaskSource (no frame is running), one when resuming a continuation.
                    uint currentFrames = 0;
                    try
                    {
                        if (awaitedState != null)
                        {
                            object s = awaitedState;
                            awaitedState = null;
                            if (GetAwaitedResult(ref awaitState, s, curContinuation) is Exception awaitException)
                            {
                                throw awaitException;
                            }
                        }
                        else
                        {
                            currentFrames = 1;

                            Continuation? nextContinuation = curContinuation.Next;
                            asyncDispatcherInfo.NextContinuation = nextContinuation;

                            RuntimeAsyncInstrumentationHelpers.SyncPointCheck(ref asyncDispatcherInfo, flags, curContinuation);

                            Debug.Assert(awaitState.CurrentThread != null);
                            if (curContinuation.TryGetExecutionContext(out ExecutionContext? execContext))
                            {
                                RestoreExecutionContext(awaitState.CurrentThread, execContext);
                            }

                            ref byte resultLoc = ref nextContinuation != null ? ref nextContinuation.GetResultStorageOrNull() : ref GetResultStorage();

                            RuntimeAsyncInstrumentationHelpers.ResumeRuntimeAsyncMethod(ref asyncDispatcherInfo, flags, curContinuation);
                            Continuation? newContinuation = RuntimeAsyncInstrumentationHelpers.ResumeContinuation(ref asyncDispatcherInfo, flags, curContinuation, ref resultLoc);

                            if (newContinuation != null)
                            {
                                if ((newContinuation.Flags & ContinuationFlags.FinishSuspensionInDispatcher) != 0)
                                {
                                    FinishSuspensionInDispatcher(newContinuation, awaitState.CurrentThread!);
                                }
                                if (newContinuation.Next != nextContinuation)
                                {
                                    newContinuation.Next = nextContinuation;
                                }

                                RuntimeAsyncInstrumentationHelpers.AwaitSuspendedRuntimeAsyncContext(ref asyncDispatcherInfo, flags, curContinuation, newContinuation, awaitState.SentinelContinuation!.Next);
                                InstrumentedHandleSuspended(flags, ref awaitState);

                                contexts.Pop(awaitState.CurrentThread!);
                                awaitState.Pop();
                                refDispatcherInfo = asyncDispatcherInfo.Next;
                                return;
                            }

                            RuntimeAsyncInstrumentationHelpers.CompleteRuntimeAsyncMethod(ref asyncDispatcherInfo, flags, curContinuation);

                            // The frame completed, so stop keeping its continuation (and everything it captured) alive.
                            m_stateObject = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        uint unwindedFrames = currentFrames;
                        Continuation? handlerContinuation = UnwindToPossibleHandler(asyncDispatcherInfo.NextContinuation, ex, ref unwindedFrames);
                        if (handlerContinuation == null)
                        {
                            RuntimeAsyncInstrumentationHelpers.UnwindRuntimeAsyncMethodUnhandledException(ref asyncDispatcherInfo, flags, ex, curContinuation, unwindedFrames);

                            // Tail of AsyncTaskMethodBuilderT.SetException
                            m_stateObject = null;
                            if (m_action is not null)
                            {
                                ReleaseValueTaskSourceContinuation(ref awaitState);
                            }

                            bool successfullySet = TrySetRuntimeAsyncException(ex);

                            contexts.Pop(awaitState.CurrentThread!);
                            awaitState.Pop();
                            refDispatcherInfo = asyncDispatcherInfo.Next;

                            if (!successfullySet)
                            {
                                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted);
                            }

                            return;
                        }

                        RuntimeAsyncInstrumentationHelpers.UnwindRuntimeAsyncMethodHandledException(ref asyncDispatcherInfo, flags, curContinuation, unwindedFrames);

                        m_stateObject = null;
                        handlerContinuation.SetException(ex);
                        asyncDispatcherInfo.NextContinuation = handlerContinuation;
                    }

                    if (asyncDispatcherInfo.NextContinuation == null)
                    {
                        RuntimeAsyncInstrumentationHelpers.CompleteRuntimeAsyncContext(ref asyncDispatcherInfo, flags);

                        m_stateObject = null;
                        if (m_action is not null)
                        {
                            ReleaseValueTaskSourceContinuation(ref awaitState);
                        }

                        bool successfullySet = TrySetRuntimeAsyncResult();

                        contexts.Pop(awaitState.CurrentThread!);
                        awaitState.Pop();
                        refDispatcherInfo = asyncDispatcherInfo.Next;

                        if (!successfullySet)
                        {
                            ThrowHelper.ThrowInvalidOperationException(ExceptionResource.TaskT_TransitionToFinal_AlreadyCompleted);
                        }

                        return;
                    }

                    if (QueueContinuationFollowUpActionIfNecessary(asyncDispatcherInfo.NextContinuation, awaitState.CurrentThread!))
                    {
                        RuntimeAsyncInstrumentationHelpers.QueueSuspendedRuntimeAsyncContext(ref asyncDispatcherInfo, flags, asyncDispatcherInfo.NextContinuation);

                        contexts.Pop(awaitState.CurrentThread!);
                        awaitState.Pop();
                        refDispatcherInfo = asyncDispatcherInfo.Next;
                        return;
                    }

                    flags = AsyncInstrumentation.LoadFlags();
                }
            }

            private ref byte GetResultStorage() => ref Unsafe.As<T?, byte>(ref m_result);

            private static unsafe Continuation? UnwindToPossibleHandler(Continuation? continuation, Exception ex, ref uint unwindedFrames)
            {
                while (true)
                {
                    if (continuation != null && continuation.ResumeInfo != null && continuation.ResumeInfo->DiagnosticIP != null)
                    {
#if !NATIVEAOT
                        AddContinuationToExInternal(continuation.ResumeInfo->DiagnosticIP, ex);
#else
                        IntPtr ip = (IntPtr)continuation.ResumeInfo->DiagnosticIP;
                        System.Exception.AppendExceptionStackFrame(ex, ip, 0);
#endif
                    }
                    if (continuation == null || continuation.HasException())
                        return continuation;

                    continuation = continuation.Next;
                    unwindedFrames++;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool QueueContinuationFollowUpActionIfNecessary(Continuation continuation, Thread currentThread)
            {
                ContinuationFlags flags = continuation.Flags;

                // Fast path for the common case of a continuation that may run on any thread pool thread
                // when no synchronization context or task is current, such that it can be run inline.
                if ((flags & ContinuationFlags.ContinueOnThreadPool) != 0)
                {
                    if (currentThread._synchronizationContext is null && Task.t_currentTask is null)
                    {
                        return false;
                    }
                }
                else if ((flags & (ContinuationFlags.ContinueOnCapturedSynchronizationContext | ContinuationFlags.ContinueOnCapturedTaskScheduler)) == 0)
                {
                    return false;
                }

                return QueueContinuationFollowUpActionIfNecessarySlow(continuation);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private bool QueueContinuationFollowUpActionIfNecessarySlow(Continuation continuation)
            {
                if ((continuation.Flags & ContinuationFlags.ContinueOnThreadPool) != 0)
                {
                    SynchronizationContext? ctx = Thread.CurrentThreadAssumedInitialized._synchronizationContext;
                    if (ctx == null || ctx.GetType() == typeof(SynchronizationContext))
                    {
                        TaskScheduler? sched = GetCurrentTaskScheduler();
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
                    TaskSchedulerAwaitTaskContinuation.RunOrScheduleAction(s_dispatchCallback, this, sched, capturedContext: null, allowInlining: true);

                    return true;
                }

                return false;
            }

            private static readonly SendOrPostCallback s_postCallback = static state =>
            {
                Debug.Assert(state is RuntimeAsyncTask<T>);
                ((RuntimeAsyncTask<T>)state).DispatchContinuations();
            };
        }

        private static void InstrumentedFinalizeRuntimeAsyncTask<T>(RuntimeAsyncTask<T> task, ref RuntimeAsyncAwaitState state, AsyncInstrumentation.Flags flags)
        {
            if (AsyncInstrumentation.IsEnabled.CreateAsyncContext(flags))
            {
                if (AsyncInstrumentation.IsEnabled.AsyncProfiler(flags))
                {
                    Continuation? nextContinuation = state.SentinelContinuation!.Next;
                    if (nextContinuation != null)
                    {
                        AsyncProfiler.CreateAsyncContext.Create(task, nextContinuation);
                    }
                }

                if (AsyncInstrumentation.IsEnabled.AsyncDebugger(flags))
                {
                    task.NotifyDebuggerOfRuntimeAsyncState();
                    AsyncDebugger.CreateAsyncContext(task);
                }
            }

            task.InstrumentedHandleSuspended(flags, ref state);
            return;
        }

        private static unsafe void FinalizeRuntimeAsyncTask<T>(ref RuntimeAsyncAwaitState state, RuntimeAsyncTask<T> task)
        {
            // We restore back to the leaf context to call OnCompleted, so we
            // need to push/pop contexts around that as well.
            AsyncContexts contexts = new AsyncContexts(state.CurrentThread!);

            if (AsyncInstrumentation.IsActive && AsyncInstrumentation.LoadFlags(out AsyncInstrumentation.Flags flags))
            {
                if (AsyncInstrumentation.IsEnabled.AsyncProfiler(flags) || AsyncInstrumentation.IsEnabled.AsyncDebugger(flags))
                {
                    InstrumentedFinalizeRuntimeAsyncTask(task, ref state, flags);
                    contexts.Pop(state.CurrentThread!);
                    return;
                }
            }

            if (!task.TryHandleSuspendedOnTask(ref state, state.StackState))
            {
                task.HandleSuspended(ref state);
            }

            contexts.Pop(state.CurrentThread!);
        }

        // Change return type to RuntimeAsyncTask<T?> -- no benefit since this is used for Task returning thunks only
#pragma warning disable CA1859
        // When a Task-returning thunk gets a continuation result
        // it calls here to make a Task that awaits on the current async state.
        [MethodImpl(MethodImplOptions.NoInlining)] // keeps the suspended path out of inlined thunks
        private static Task<T?> CreateRuntimeAsyncTask<T>(ref RuntimeAsyncAwaitState state)
        {
            RuntimeAsyncTask<T?> result = new();
            FinalizeRuntimeAsyncTask(ref state, result);
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // keeps the suspended path out of inlined thunks
        private static Task CreateRuntimeAsyncTask(ref RuntimeAsyncAwaitState state)
        {
            RuntimeAsyncTask<VoidTaskResult> result = new();
            FinalizeRuntimeAsyncTask(ref state, result);
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // keeps the suspended path out of inlined thunks
        private static ValueTask<T?> CreateRuntimeAsyncValueTask<T>(ref RuntimeAsyncAwaitState state)
        {
            // We only come to these methods in the expensive case (already
            // suspended), so ValueTask optimization here is not relevant.
            RuntimeAsyncTask<T?> result = new();
            FinalizeRuntimeAsyncTask(ref state, result);
            return new ValueTask<T?>(result);
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // keeps the suspended path out of inlined thunks
        private static ValueTask CreateRuntimeAsyncValueTask(ref RuntimeAsyncAwaitState state)
        {
            RuntimeAsyncTask<VoidTaskResult> result = new();
            FinalizeRuntimeAsyncTask(ref state, result);
            return new ValueTask(result);
        }

        // Used instead of CreateRuntimeAsyncValueTask by thunks for methods attributed with
        // [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))], mirroring that builder's pooling.
        [MethodImpl(MethodImplOptions.NoInlining)] // keeps the suspended path out of inlined thunks
        private static ValueTask<T?> CreatePooledRuntimeAsyncValueTask<T>(ref RuntimeAsyncAwaitState state)
        {
            if ((AsyncInstrumentation.IsActive && AsyncInstrumentation.LoadFlags(out _)) || Task.s_asyncDebuggingEnabled)
            {
                // Diagnostics track the operation by its Task identity, so don't reuse it.
                return CreateRuntimeAsyncValueTask<T>(ref state);
            }

            PooledRuntimeAsyncValueTaskSource<T?> source = PooledRuntimeAsyncValueTaskSource<T?>.RentFromCache();
            short token = source.Version;
            FinalizeRuntimeAsyncTask(ref state, source.Task);
            return new ValueTask<T?>(source, token);
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // keeps the suspended path out of inlined thunks
        private static ValueTask CreatePooledRuntimeAsyncValueTask(ref RuntimeAsyncAwaitState state)
        {
            if ((AsyncInstrumentation.IsActive && AsyncInstrumentation.LoadFlags(out _)) || Task.s_asyncDebuggingEnabled)
            {
                return CreateRuntimeAsyncValueTask(ref state);
            }

            PooledRuntimeAsyncValueTaskSource<VoidTaskResult> source = PooledRuntimeAsyncValueTaskSource<VoidTaskResult>.RentFromCache();
            short token = source.Version;
            FinalizeRuntimeAsyncTask(ref state, source.Task);
            return new ValueTask(source, token);
        }

        // A RuntimeAsyncTask that's never exposed or completed as a Task. Its Task-ness is used only to dispatch
        // continuations; its completion is instead reported to the owning pooled value task source.
        private sealed class PooledRuntimeAsyncTask<T> : RuntimeAsyncTask<T>
        {
            internal readonly PooledRuntimeAsyncValueTaskSource<T> _source;

            internal PooledRuntimeAsyncTask(PooledRuntimeAsyncValueTaskSource<T> source) => _source = source;

            internal override bool TrySetRuntimeAsyncResult()
            {
                T result = m_result!;
                m_result = default;
                _source.SetResult(result);
                return true;
            }

            internal override bool TrySetRuntimeAsyncException(Exception ex)
            {
                m_result = default;
                _source.SetException(ex);
                return true;
            }
        }

        // The IValueTaskSource backing a ValueTask returned from a pooling runtime async method. It can't itself be
        // the RuntimeAsyncTask, as ValueTask treats any Task it wraps as a Task. It's reused (along with its task)
        // once its result has been consumed.
        private sealed class PooledRuntimeAsyncValueTaskSource<T> : IValueTaskSource<T>, IValueTaskSource
        {
            // Kept free of static initializers so that accessing it doesn't require class initialization checks.
            [ThreadStatic]
            private static PooledRuntimeAsyncValueTaskSource<T>? t_tlsCache;

            internal readonly PooledRuntimeAsyncTask<T> Task;

            private ManualResetValueTaskSourceCore<T> _valueTaskSource;

            private PooledRuntimeAsyncValueTaskSource() => Task = new PooledRuntimeAsyncTask<T>(this);

            public short Version => _valueTaskSource.Version;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static PooledRuntimeAsyncValueTaskSource<T> RentFromCache()
            {
                PooledRuntimeAsyncValueTaskSource<T>? source = t_tlsCache;
                if (source is not null)
                {
                    t_tlsCache = null;
                }
                else
                {
                    ref PooledRuntimeAsyncValueTaskSource<T>? slot = ref PerCoreCacheSlot;
                    if (slot is null ||
                        (source = Interlocked.Exchange(ref slot, null)) is null)
                    {
                        source = new PooledRuntimeAsyncValueTaskSource<T>();
                    }
                }

                return source;
            }

            private void ReturnToCache()
            {
                _valueTaskSource.Reset();

                if (t_tlsCache is null)
                {
                    t_tlsCache = this;
                }
                else
                {
                    ref PooledRuntimeAsyncValueTaskSource<T>? slot = ref PerCoreCacheSlot;
                    if (slot is null)
                    {
                        Volatile.Write(ref slot, this);
                    }
                }
            }

            private static ref PooledRuntimeAsyncValueTaskSource<T>? PerCoreCacheSlot
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    int i = (int)((uint)Thread.GetCurrentProcessorId() % (uint)Environment.ProcessorCount);
                    return ref Unsafe.As<object?, PooledRuntimeAsyncValueTaskSource<T>?>(ref PerCoreCache.s_cache[i].Object);
                }
            }

            private static class PerCoreCache
            {
                // Per-core cache of sources, with each element padded to a cache line to minimize false sharing.
                internal static readonly PaddedReference[] s_cache = new PaddedReference[Environment.ProcessorCount];
            }

            internal void SetResult(T result) => _valueTaskSource.SetResult(result);

            internal void SetException(Exception ex) => _valueTaskSource.SetException(ex);

            public ValueTaskSourceStatus GetStatus(short token) => _valueTaskSource.GetStatus(token);

            public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
                _valueTaskSource.OnCompleted(continuation, state, token, flags);

            T IValueTaskSource<T>.GetResult(short token)
            {
                try
                {
                    return _valueTaskSource.GetResult(token);
                }
                finally
                {
                    ReturnToCache();
                }
            }

            void IValueTaskSource.GetResult(short token)
            {
                try
                {
                    _valueTaskSource.GetResult(token);
                }
                finally
                {
                    ReturnToCache();
                }
            }
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
        private static void RestoreExecutionContext(Thread thread, ExecutionContext? previousExecCtx)
        {
            ExecutionContext? currentExecCtx = thread._executionContext;
            if (previousExecCtx != currentExecCtx && previousExecCtx != ExecutionContext.DefaultFlowSuppressed)
            {
                ExecutionContext.RestoreChangedContextToThread(thread, previousExecCtx, currentExecCtx);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CaptureContexts(out Thread thread, out ExecutionContext? execCtx, out SynchronizationContext? syncCtx)
        {
            Thread curThread = Thread.CurrentThreadAssumedInitialized;
            thread = curThread;
            // Here we get the execution context for synchronous restoring,
            // not for flowing across suspension to potentially another thread.
            // Therefore we do not need to worry about IsFlowSuppressed
            execCtx = curThread._executionContext;
            syncCtx = curThread._synchronizationContext;
        }

        // Restore contexts onto thread. If "resumed" then this is not the first starting call for the async method.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RestoreContexts(bool resumed, Thread thread, ExecutionContext? previousExecCtx, SynchronizationContext? previousSyncCtx)
        {
            if (!resumed)
            {
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

        // Equivalent to TaskScheduler.InternalCurrent, but avoids the call when no task is current (the common case).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TaskScheduler? GetCurrentTaskScheduler() => Task.t_currentTask is null ? null : TaskScheduler.InternalCurrent;

        private static void CaptureContinuationContext(ref object? continuationContext, ref ContinuationFlags flags)
        {
            SynchronizationContext? syncCtx = Thread.CurrentThreadAssumedInitialized._synchronizationContext;
            if (syncCtx != null && syncCtx.GetType() != typeof(SynchronizationContext))
            {
                flags |= ContinuationFlags.ContinueOnCapturedSynchronizationContext;
                continuationContext = syncCtx;
                return;
            }

            TaskScheduler? sched = GetCurrentTaskScheduler();
            if (sched != null && sched != TaskScheduler.Default)
            {
                flags |= ContinuationFlags.ContinueOnCapturedTaskScheduler;
                continuationContext = sched;
                return;
            }

            flags |= ContinuationFlags.ContinueOnThreadPool;
        }

        // Same as above, but only captures flags
        private static void CaptureContinuationContextFlags(ref ContinuationFlags flags, Thread currentThread)
        {
            SynchronizationContext? syncCtx = currentThread._synchronizationContext;
            if (syncCtx != null && syncCtx.GetType() != typeof(SynchronizationContext))
            {
                flags |= ContinuationFlags.ContinueOnCapturedSynchronizationContext;
                return;
            }

            TaskScheduler? sched = GetCurrentTaskScheduler();
            if (sched != null && sched != TaskScheduler.Default)
            {
                flags |= ContinuationFlags.ContinueOnCapturedTaskScheduler;
                return;
            }

            flags |= ContinuationFlags.ContinueOnThreadPool;
        }

        // Restore the contexts that an inlined async frame captured when it logically returned to
        // its caller, after that frame was resumed inside its own body.
        //
        // Used by the JIT when inlining runtime async calls. The JIT emits the check of whether
        // the frame was resumed at all and calls this when it was; everything the async
        // infrastructure would otherwise have done at that frame boundary happens here.
        //
        // Unlike the synchronous restore at the end of a method, the ExecutionContext restore runs
        // only after a resumption, so it must target the thread we were resumed on rather than the
        // one whose contexts were captured on entry.
        //
        // The continuation context check determines whether resuming the caller's continuation here
        // would be dispatched inline. It mirrors the "can inline" conditions in
        // RuntimeAsyncTask.QueueAwaitContinuationIfNecessary; the two must be kept in sync.
        //
        // 'flags' must contain only ContinuationFlags.AllContinuationFlags bits.
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.Async)]
        private static void RestoreInlinedFrameContexts(ExecutionContext? previousExecCtx, object? continuationContext, ContinuationFlags flags)
        {
            Debug.Assert((flags & ~ContinuationFlags.AllContinuationFlags) == 0);

            // We are inside a runtime async chain, so the thread has already been cached. Use it
            // instead of Thread.CurrentThreadAssumedInitialized to keep this to one TLS lookup.
            ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
            Thread? currentThread = state.CurrentThread;
            Debug.Assert(currentThread != null);

            RestoreExecutionContext(currentThread, previousExecCtx);

            if ((flags & ContinuationFlags.ContinueOnThreadPool) != 0)
            {
                SynchronizationContext? syncCtx = currentThread._synchronizationContext;
                if (syncCtx is null || syncCtx.GetType() == typeof(SynchronizationContext))
                {
                    TaskScheduler? sched = GetCurrentTaskScheduler();
                    if (sched is null || sched == TaskScheduler.Default)
                    {
                        return;
                    }
                }
            }
            else if ((flags & ContinuationFlags.ContinueOnCapturedSynchronizationContext) != 0)
            {
                Debug.Assert(continuationContext is SynchronizationContext);
                if (continuationContext == currentThread._synchronizationContext)
                {
                    return;
                }
            }
            else if ((flags & ContinuationFlags.ContinueOnCapturedTaskScheduler) != 0)
            {
                Debug.Assert(continuationContext is TaskScheduler);
                if (continuationContext == TaskScheduler.InternalCurrent)
                {
                    return;
                }
            }
            else
            {
                // No continuation context was captured, so there is nothing to switch to.
                return;
            }

            TailAwait();
            SwitchToContinuationContext(ref state, continuationContext, flags);
        }

        // Suspend and resume in the specified continuation context.
        //
        // Suspending on an already completed task makes the dispatcher re-dispatch the continuation
        // immediately. Because that dispatch happens with canInline: false, it always posts or
        // schedules onto the requested context rather than running inline here, which is what we
        // want -- we only get here when we are known to be on the wrong context.
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.Async)]
        private static unsafe void SwitchToContinuationContext(ref RuntimeAsyncAwaitState state, object? continuationContext, ContinuationFlags flags)
        {
            Continuation? sentinelContinuation = state.SentinelContinuation ??= new Continuation();

            RuntimeAsyncStackState* stackState = state.StackState;
            stackState->Notifier = Task.CompletedTask;
            stackState->AwaitedContinuationContext = EncodeAwaitedContinuationContext(continuationContext, flags);
            stackState->NotifierKind = SuspensionNotifierKind.AwaitedTask;

            AsyncSuspend(sentinelContinuation);
        }

        // Capture the contexts an inlined async frame hands to its caller when it logically
        // returns during a suspension, i.e. what the caller's continuation would have captured
        // had the callee's frame been physically present.
        //
        // No-ops when the frame has already resumed: in that case the caller's continuation
        // already exists and keeps the values it captured when the frame first suspended.
        //
        // The suspension walks the inlined frames outward, and a frame having resumed implies
        // its caller has too, so these can be emitted as a straight line: once one frame has
        // resumed, this and every subsequent capture for the frames outside it no-op.
        //
        // Which of the three variants the JIT emits follows how the caller awaited the frame.
        // Each assigns the whole flags value rather than adding to it, since the per-depth
        // storage is shared and may hold what an unrelated suspension point left there.
        //
        // Capture with the caller's continuation context, for a frame whose caller awaited it in
        // a way that has to come back to the context it was on.
        private static void CaptureInlinedFrameTransitionWithContinuationContext(bool resumed,
                                                          ref object? continuationContext,
                                                          ref ContinuationFlags flags,
                                                          ref ExecutionContext? execContext)
        {
            if (resumed)
            {
                return;
            }

            flags = default;
            CaptureContinuationContext(ref continuationContext, ref flags);
            execContext = CaptureExecutionContext();
        }

        // Capture for a frame whose caller awaited it in a way that captures no continuation
        // context at all, as a custom awaiter does, so only the ExecutionContext has to be
        // restored when the frame logically returns.
        private static void CaptureInlinedFrameTransitionNoContinuationContext(bool resumed,
                                                          ref ContinuationFlags flags,
                                                          ref ExecutionContext? execContext)
        {
            if (resumed)
            {
                return;
            }

            flags = default;
            execContext = CaptureExecutionContext();
        }

        // Capture for a frame whose caller awaited it with ConfigureAwait(false), which asks to
        // continue off any captured context, so the frame gets back to the thread pool rather
        // than to a context it recorded.
        private static void CaptureInlinedFrameTransitionContinueOnThreadPool(bool resumed,
                                                          ref ContinuationFlags flags,
                                                          ref ExecutionContext? execContext)
        {
            if (resumed)
            {
                return;
            }

            flags = ContinuationFlags.ContinueOnThreadPool;
            execContext = CaptureExecutionContext();
        }

        // Finish suspension in the common case of a custom await or for a ConfigureAwait(false) task await:
        // - Capture current ExecutionContext into the continuation
        // - Restore ExecutionContext and SynchronizationContext to the current Thread object
        private static void FinishSuspensionNoContinuationContext(ref ExecutionContext? execCtx, bool resumed, ExecutionContext? previousExecCtx, SynchronizationContext? previousSyncCtx)
        {
            Thread thread = Thread.CurrentThreadAssumedInitialized;

            ExecutionContext? threadExecCtx = thread._executionContext;

            // Commonly when we reuse a continuation we have already the right
            // context saved, or we are saving null and it is already null.
            if (threadExecCtx != execCtx)
            {
                if (threadExecCtx != null && threadExecCtx.InstanceIsFlowSuppressed)
                {
                    execCtx = ExecutionContext.DefaultFlowSuppressed;
                }
                else
                {
                    execCtx = threadExecCtx;
                }
            }
            else
            {
                Debug.Assert(threadExecCtx == null || !threadExecCtx.InstanceIsFlowSuppressed || execCtx == ExecutionContext.DefaultFlowSuppressed);
            }

            if (!resumed)
            {
                if (previousSyncCtx != thread._synchronizationContext)
                {
                    thread._synchronizationContext = previousSyncCtx;
                }

                if (previousExecCtx != threadExecCtx)
                {
                    thread._executionContext = previousExecCtx;
                }
            }
        }

        // Finish suspension in the common case of a standard task await:
        // - Record continuation context to determine where to continue on resumption
        // - Capture current ExecutionContext into the continuation
        // - Restore ExecutionContext and SynchronizationContext to the current Thread object
        private static void FinishSuspensionWithContinuationContext(ref object continuationContext, ref ContinuationFlags flags, ref ExecutionContext? execCtx, bool resumed, ExecutionContext? previousExecCtx, SynchronizationContext? previousSyncCtx)
        {
            Thread thread = Thread.CurrentThreadAssumedInitialized;
            SynchronizationContext? threadSyncCtx = thread._synchronizationContext;
            if (threadSyncCtx != null && threadSyncCtx.GetType() != typeof(SynchronizationContext))
            {
                flags |= ContinuationFlags.ContinueOnCapturedSynchronizationContext;
                if (continuationContext != threadSyncCtx)
                {
                    continuationContext = threadSyncCtx;
                }
            }
            else
            {
                TaskScheduler? sched = GetCurrentTaskScheduler();
                if (sched != null && sched != TaskScheduler.Default)
                {
                    flags |= ContinuationFlags.ContinueOnCapturedTaskScheduler;
                    if (continuationContext != sched)
                    {
                        continuationContext = sched;
                    }
                }
                else
                {
                    flags |= ContinuationFlags.ContinueOnThreadPool;
                }
            }

            ExecutionContext? threadExecCtx = thread._executionContext;
            // Commonly when we reuse a continuation we have already the right
            // context saved, or we are saving null and it is already null.
            if (threadExecCtx != execCtx)
            {
                if (threadExecCtx != null && threadExecCtx.InstanceIsFlowSuppressed)
                {
                    execCtx = ExecutionContext.DefaultFlowSuppressed;
                }
                else
                {
                    execCtx = threadExecCtx;
                }
            }
            else
            {
                Debug.Assert(threadExecCtx == null || !threadExecCtx.InstanceIsFlowSuppressed || execCtx == ExecutionContext.DefaultFlowSuppressed);
            }

            if (!resumed)
            {
                if (previousSyncCtx != threadSyncCtx)
                {
                    thread._synchronizationContext = previousSyncCtx;
                }

                if (previousExecCtx != threadExecCtx)
                {
                    thread._executionContext = previousExecCtx;
                }
            }
        }

        // Finish the suspension of a frame that the dispatcher resumed and that left this to the
        // dispatcher (see ContinuationFlags.FinishSuspensionInDispatcher). This does what
        // FinishSuspension{No,With}ContinuationContext would have done for the resumed frame;
        // the frame returned straight to the dispatcher, so the thread's contexts are still the
        // ones it suspended with.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FinishSuspensionInDispatcher(Continuation continuation, Thread thread)
        {
            Debug.Assert((continuation.Flags & ContinuationFlags.FinishSuspensionInDispatcher) != 0);

            ref object? continuationContext = ref continuation.GetContinuationContextRefOrNull();
            if (!Unsafe.IsNullRef(ref continuationContext))
            {
                CaptureContinuationContextForDispatcher(continuation, ref continuationContext, thread);
            }

            ExecutionContext? threadExecCtx = thread._executionContext;
            ref ExecutionContext? execCtx = ref continuation.GetExecutionContextRef();
            if (threadExecCtx != execCtx)
            {
                execCtx = threadExecCtx != null && threadExecCtx.InstanceIsFlowSuppressed ? ExecutionContext.DefaultFlowSuppressed : threadExecCtx;
            }
        }

        private static void CaptureContinuationContextForDispatcher(Continuation continuation, ref object? continuationContext, Thread thread)
        {
            SynchronizationContext? threadSyncCtx = thread._synchronizationContext;
            if (threadSyncCtx != null && threadSyncCtx.GetType() != typeof(SynchronizationContext))
            {
                continuation.Flags |= ContinuationFlags.ContinueOnCapturedSynchronizationContext;
                if (continuationContext != threadSyncCtx)
                {
                    continuationContext = threadSyncCtx;
                }
            }
            else
            {
                TaskScheduler? sched = GetCurrentTaskScheduler();
                if (sched != null && sched != TaskScheduler.Default)
                {
                    continuation.Flags |= ContinuationFlags.ContinueOnCapturedTaskScheduler;
                    if (continuationContext != sched)
                    {
                        continuationContext = sched;
                    }
                }
                else
                {
                    continuation.Flags |= ContinuationFlags.ContinueOnThreadPool;
                }
            }
        }

        // Instrumentation helpers called from InstrumentedDispatchContinuations.
        // These methods should not throw - exceptions would break the dispatch loop.
        internal static class RuntimeAsyncInstrumentationHelpers
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void SyncPointCheck(ref AsyncDispatcherInfo info, AsyncInstrumentation.Flags flags, Continuation curContinuation)
            {
                if (AsyncInstrumentation.IsEnabled.AsyncProfiler(flags))
                {
                    info.AsyncProfilerInfo.CurrentContinuation = curContinuation;
                    AsyncProfiler.SyncPoint.Check(ref info.AsyncProfilerInfo);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ResumeRuntimeAsyncContext(Task task, ref AsyncDispatcherInfo info, AsyncInstrumentation.Flags flags, Task? awaitedTask)
            {
                info.CurrentTask = task;
                AsyncProfiler.InitInfo(ref info.AsyncProfilerInfo);

                info.AsyncProfilerInfo.DispatcherId = (ulong)task.Id;

                if (AsyncInstrumentation.IsEnabled.ResumeAsyncContext(flags))
                {
                    if (AsyncInstrumentation.IsEnabled.AsyncProfiler(flags))
                    {
                        AsyncProfiler.ResumeAsyncContext.Resume(ref info);
                    }

                    if (AsyncInstrumentation.IsEnabled.AsyncDebugger(flags))
                    {
                        AsyncDebugger.ResumeAsyncContext(task, awaitedTask);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void QueueSuspendedRuntimeAsyncContext(ref AsyncDispatcherInfo info, AsyncInstrumentation.Flags flags, Continuation nextContinuation)
            {
                if (AsyncInstrumentation.IsEnabled.SuspendAsyncContext(flags))
                {
                    if (AsyncInstrumentation.IsEnabled.AsyncProfiler(flags))
                    {
                        AsyncProfiler.SuspendAsyncContext.Suspend(ref info, nextContinuation);
                    }

                    if (AsyncInstrumentation.IsEnabled.AsyncDebugger(flags))
                    {
                        AsyncDebugger.SuspendAsyncContext();
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void AwaitSuspendedRuntimeAsyncContext(ref AsyncDispatcherInfo info, AsyncInstrumentation.Flags flags, Continuation curContinuation, Continuation newContinuation, Continuation? nextContinuation)
            {
                if (AsyncInstrumentation.IsEnabled.SuspendAsyncContext(flags))
                {
                    if (AsyncInstrumentation.IsEnabled.AsyncProfiler(flags))
                    {
                        AsyncProfiler.SuspendAsyncContext.Suspend(ref info, nextContinuation ?? newContinuation);
                    }

                    if (AsyncInstrumentation.IsEnabled.AsyncDebugger(flags))
                    {
                        AsyncDebugger.SuspendAsyncContext(curContinuation, newContinuation);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void CompleteRuntimeAsyncContext(ref AsyncDispatcherInfo info, AsyncInstrumentation.Flags flags)
            {
                if (AsyncInstrumentation.IsEnabled.CompleteAsyncContext(flags))
                {
                    if (AsyncInstrumentation.IsEnabled.AsyncProfiler(flags))
                    {
                        AsyncProfiler.CompleteAsyncContext.Complete(ref info);
                    }

                    if (AsyncInstrumentation.IsEnabled.AsyncDebugger(flags))
                    {
                        AsyncDebugger.CompleteAsyncContext(info.CurrentTask);
                    }
                }
            }

            public static void UnwindRuntimeAsyncMethodUnhandledException(ref AsyncDispatcherInfo info, AsyncInstrumentation.Flags flags, Exception ex, Continuation curContinuation, uint unwindedFrames)
            {
                if (AsyncInstrumentation.IsEnabled.AsyncProfiler(flags))
                {
                    AsyncProfiler.ContinuationWrapper.UnwindIndex(ref info.AsyncProfilerInfo, unwindedFrames);
                }

                if (AsyncInstrumentation.IsEnabled.UnwindAsyncException(flags))
                {
                    if (AsyncInstrumentation.IsEnabled.AsyncProfiler(flags))
                    {
                        AsyncProfiler.AsyncMethodException.Unhandled(ref info, unwindedFrames);
                    }

                    if (AsyncInstrumentation.IsEnabled.AsyncDebugger(flags))
                    {
                        AsyncDebugger.AsyncMethodUnhandledException(info.CurrentTask, ex, curContinuation);
                    }
                }
            }

            public static void UnwindRuntimeAsyncMethodHandledException(ref AsyncDispatcherInfo info, AsyncInstrumentation.Flags flags, Continuation curContinuation, uint unwindedFrames)
            {
                if (AsyncInstrumentation.IsEnabled.AsyncProfiler(flags))
                {
                    AsyncProfiler.ContinuationWrapper.UnwindIndex(ref info.AsyncProfilerInfo, unwindedFrames);
                }

                if (AsyncInstrumentation.IsEnabled.UnwindAsyncException(flags))
                {
                    if (AsyncInstrumentation.IsEnabled.AsyncProfiler(flags))
                    {
                        AsyncProfiler.AsyncMethodException.Handled(ref info, unwindedFrames);
                    }

                    if (AsyncInstrumentation.IsEnabled.AsyncDebugger(flags))
                    {
                        AsyncDebugger.AsyncMethodHandledException(curContinuation, unwindedFrames);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ResumeRuntimeAsyncMethod(ref AsyncDispatcherInfo info, AsyncInstrumentation.Flags flags, Continuation curContinuation)
            {
                if (AsyncInstrumentation.IsEnabled.ResumeAsyncMethod(flags))
                {
                    if (AsyncInstrumentation.IsEnabled.AsyncProfiler(flags))
                    {
                        AsyncProfiler.ResumeAsyncMethod.Resume(ref info);
                    }

                    if (AsyncInstrumentation.IsEnabled.AsyncDebugger(flags))
                    {
                        AsyncDebugger.ResumeAsyncMethod(ref info, curContinuation);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void CompleteRuntimeAsyncMethod(ref AsyncDispatcherInfo info, AsyncInstrumentation.Flags flags, Continuation curContinuation)
            {
                if (AsyncInstrumentation.IsEnabled.AsyncProfiler(flags))
                {
                    if (AsyncInstrumentation.IsEnabled.CompleteAsyncMethod(flags))
                    {
                        AsyncProfiler.CompleteAsyncMethod.Complete(ref info);
                    }

                    AsyncProfiler.ContinuationWrapper.IncrementIndex(ref info.AsyncProfilerInfo);
                }

                if (AsyncInstrumentation.IsEnabled.AsyncDebugger(flags) && AsyncInstrumentation.IsEnabled.CompleteAsyncMethod(flags))
                {
                    AsyncDebugger.CompleteAsyncMethod(curContinuation);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Continuation? ResumeContinuation(ref AsyncDispatcherInfo info, AsyncInstrumentation.Flags flags, Continuation curContinuation, ref byte resultLoc)
            {
                if (AsyncInstrumentation.IsEnabled.AsyncProfiler(flags))
                {
                    return AsyncProfiler.ContinuationWrapper.Dispatch(ref info, curContinuation, ref resultLoc);
                }

                unsafe
                {
                    return curContinuation.ResumeInfo->Resume(curContinuation, ref resultLoc);
                }
            }
        }

        internal static class AsyncDebugger
        {
            public static void CreateAsyncContext(Task task)
            {
                Task.AddToActiveTasks(task);
                TplEventSource.Log.TraceOperationBegin(task.Id, "System.Runtime.CompilerServices.AsyncHelpers+RuntimeAsyncTask", 0);
            }

            public static void ResumeAsyncContext(Task task, Task? awaitedTask)
            {
                OutputTaskWaitEnd(task, awaitedTask is SuppressedAwaitedTask suppressed ? suppressed.AwaitedTask : awaitedTask);
                TplEventSource.Log.TraceSynchronousWorkBegin(task.Id, CausalitySynchronousWork.Execution);
            }

            public static void SuspendAsyncContext(Continuation curContinuation, Continuation newContinuation)
            {
                Task.ReplaceOrAddRuntimeAsyncContinuationTimestamp(curContinuation, newContinuation);
                TplEventSource.Log.TraceSynchronousWorkEnd(CausalitySynchronousWork.Execution);
            }

            public static void SuspendAsyncContext()
            {
                TplEventSource.Log.TraceSynchronousWorkEnd(CausalitySynchronousWork.Execution);
            }

            public static void CompleteAsyncContext(Task? task)
            {
                if (task != null)
                {
                    Task.RemoveRuntimeAsyncTask(task);
                    TplEventSource.Log.TraceOperationEnd(task.Id, AsyncCausalityStatus.Completed);
                    TplEventSource.Log.TraceSynchronousWorkEnd(CausalitySynchronousWork.Execution);
                }
            }

            public static void AsyncMethodUnhandledException(Task? task, Exception ex, Continuation curContinuation)
            {
                if (task != null)
                {
                    Task.RemoveRuntimeAsyncTask(task, curContinuation);
                    TplEventSource.Log.TraceOperationEnd(task.Id, ex is OperationCanceledException ? AsyncCausalityStatus.Canceled : AsyncCausalityStatus.Error);
                    TplEventSource.Log.TraceSynchronousWorkEnd(CausalitySynchronousWork.Execution);
                }
            }

            public static void AsyncMethodHandledException(Continuation curContinuation, uint unwindedFrames)
            {
                Task.RemoveRuntimeAsyncContinuationChainTimestamps(curContinuation, unwindedFrames);
            }

            public static void ResumeAsyncMethod(ref AsyncDispatcherInfo info, Continuation curContinuation)
            {
                if (info.CurrentTask != null)
                {
                    Task.UpdateRuntimeAsyncTaskTimestamp(info.CurrentTask, curContinuation);
                }
            }

            public static void CompleteAsyncMethod(Continuation curContinuation)
            {
                Task.RemoveRuntimeAsyncContinuationTimestamp(curContinuation);
            }

            public static void HandleSuspended(Task task, Continuation? nextContinuation, Task? awaitedTask)
            {
                if (nextContinuation != null)
                {
                    Task.TryAddRuntimeAsyncContinuationChainTimestamps(nextContinuation);
                }

                OutputTaskWaitBegin(task, awaitedTask);
            }

            private static void OutputTaskWaitBegin(Task task, Task? awaitedTask)
            {
                if (awaitedTask != null)
                {
                    TplEventSource log = TplEventSource.Log;
                    if (log.IsEnabled(EventLevel.Informational, TplEventSource.Keywords.TaskTransfer | TplEventSource.Keywords.Tasks))
                    {
                        log.TaskWaitBegin(
                            task.m_taskScheduler?.Id ?? TaskScheduler.Default.Id,
                            task.Id,
                            awaitedTask.Id,
                            TplEventSource.TaskWaitBehavior.Asynchronous,
                            task.Id);
                    }
                }
            }

            public static void HandleSuspendedFailed(Task task, Continuation? nextContinuation, Task? awaitedTask)
            {
                OutputTaskWaitEnd(task, awaitedTask);

                if (nextContinuation != null)
                {
                    Task.RemoveRuntimeAsyncTask(task, nextContinuation);
                }
                else
                {
                    Task.RemoveRuntimeAsyncTask(task);
                }
            }

            private static void OutputTaskWaitEnd(Task task, Task? awaitedTask)
            {
                if (awaitedTask != null)
                {
                    TplEventSource log = TplEventSource.Log;
                    if (log.IsEnabled(EventLevel.Verbose, TplEventSource.Keywords.Tasks))
                    {
                        log.TaskWaitEnd(
                            task.m_taskScheduler?.Id ?? TaskScheduler.Default.Id,
                            task.Id,
                            awaitedTask.Id);
                    }
                }
            }
        }
    }
}
