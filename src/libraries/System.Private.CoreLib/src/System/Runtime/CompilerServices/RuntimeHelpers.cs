// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    public static partial class RuntimeHelpers
    {
        // The special dll name to be used for DllImport of QCalls
        internal const string QCall = "QCall";

        public delegate void TryCode(object? userData);

        public delegate void CleanupCode(object? userData, bool exceptionThrown);

        /// <summary>
        /// Slices the specified array using the specified range.
        /// </summary>
        public static T[] GetSubArray<T>(T[] array, Range range)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            (int offset, int length) = range.GetOffsetAndLength(array.Length);

            if (length == 0)
            {
                return Array.Empty<T>();
            }

            T[] dest = new T[length];

            // Due to array variance, it's possible that the incoming array is
            // actually of type U[], where U:T; or that an int[] <-> uint[] or
            // similar cast has occurred. In any case, since it's always legal
            // to reinterpret U as T in this scenario (but not necessarily the
            // other way around), we can use Buffer.Memmove here.

            Buffer.Memmove(
                ref MemoryMarshal.GetArrayDataReference(dest),
                ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), offset),
                (uint)length);

            return dest;
        }

        [Obsolete(Obsoletions.ConstrainedExecutionRegionMessage, DiagnosticId = Obsoletions.ConstrainedExecutionRegionDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void ExecuteCodeWithGuaranteedCleanup(TryCode code, CleanupCode backoutCode, object? userData)
        {
            ArgumentNullException.ThrowIfNull(code);
            ArgumentNullException.ThrowIfNull(backoutCode);

            bool exceptionThrown = true;

            try
            {
                code(userData);
                exceptionThrown = false;
            }
            finally
            {
                backoutCode(userData, exceptionThrown);
            }
        }

        [Obsolete(Obsoletions.ConstrainedExecutionRegionMessage, DiagnosticId = Obsoletions.ConstrainedExecutionRegionDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void PrepareContractedDelegate(Delegate d)
        {
        }

        [Obsolete(Obsoletions.ConstrainedExecutionRegionMessage, DiagnosticId = Obsoletions.ConstrainedExecutionRegionDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void ProbeForSufficientStack()
        {
        }

        [Obsolete(Obsoletions.ConstrainedExecutionRegionMessage, DiagnosticId = Obsoletions.ConstrainedExecutionRegionDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void PrepareConstrainedRegions()
        {
        }

        [Obsolete(Obsoletions.ConstrainedExecutionRegionMessage, DiagnosticId = Obsoletions.ConstrainedExecutionRegionDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void PrepareConstrainedRegionsNoOP()
        {
        }

        internal static bool IsPrimitiveType(this CorElementType et)
            // COR_ELEMENT_TYPE_I1,I2,I4,I8,U1,U2,U4,U8,R4,R8,I,U,CHAR,BOOLEAN
            => ((1 << (int)et) & 0b_0011_0000_0000_0011_1111_1111_1100) != 0;

        /// <summary>Provide a fast way to access constant data stored in a module as a ReadOnlySpan{T}</summary>
        /// <param name="fldHandle">A field handle that specifies the location of the data to be referred to by the ReadOnlySpan{T}. The Rva of the field must be aligned on a natural boundary of type T</param>
        /// <returns>A ReadOnlySpan{T} of the data stored in the field</returns>
        /// <exception cref="ArgumentException"><paramref name="fldHandle"/> does not refer to a field which is an Rva, is misaligned, or T is of an invalid type.</exception>
        /// <remarks>This method is intended for compiler use rather than use directly in code. T must be one of byte, sbyte, bool, char, short, ushort, int, uint, long, ulong, float, or double.</remarks>
        [Intrinsic]
        public static unsafe ReadOnlySpan<T> CreateSpan<T>(RuntimeFieldHandle fldHandle) => new ReadOnlySpan<T>(GetSpanDataFrom(fldHandle, typeof(T).TypeHandle, out int length), length);


        // The following intrinsics return true if input is a compile-time constant
        // Feel free to add more overloads on demand
#pragma warning disable IDE0060
        [Intrinsic]
        internal static bool IsKnownConstant(Type? t) => false;

        [Intrinsic]
        internal static bool IsKnownConstant(string? t) => false;

        [Intrinsic]
        internal static bool IsKnownConstant(char t) => false;

        [Intrinsic]
        internal static bool IsKnownConstant(int t) => false;
#pragma warning restore IDE0060

#if !NATIVEAOT
        [Intrinsic]
        public static void AwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion
        {
            if (RuntimeAsyncViaJitGeneratedStateMachines)
            {
                ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
                object? sentinelContinuation = state.SentinelContinuation;
                if (sentinelContinuation == null)
                    state.SentinelContinuation = sentinelContinuation = default(Continuation);

                state.Notifier = awaiter;
                SuspendAsync2(sentinelContinuation);
                return;
            }
            else
            {
                StackCrawlMark stackMark = StackCrawlMark.LookForMe;

                // Create resumption delegate, wrapping task, and create tasklets to represent each stack frame on the stack.
                // RuntimeTaskSuspender.GetOrCreateResumptionDelegate() works like a POSIX fork call in that calls to it will return a
                // delegate if they are the initial call to GetOrCreateResumptionDelegate, but once the thread is resumed,
                // it will resume with a return value of null.
                Action? resumption = RuntimeHelpers.GetOrCreateResumptionDelegate(ref stackMark);
                if (resumption != null)
                {
                    // If we reach here, the only way that we actually run follow on code is for the continuation to actually run,
                    // and return from GetOrCreateResumptionDelegate with a null return value.
                    ref AsyncDataFrame asyncFrame = ref GetCurrentAsyncDataFrame();
                    RuntimeAsyncMaintainedData maintainedData = asyncFrame._maintainedData!;
                    maintainedData._awaiter = awaiter;
                    // This function must be called from the same function that has the stackmark in it.
                    unsafe { UnwindToFunctionWithAsyncFrame(maintainedData._nextTasklet, maintainedData._suspendActive); }
                }
            }
        }

        // Marked intrinsic since for JIT state machines this needs to be
        // recognizes as an async2 call.
        [Intrinsic]
        public static void UnsafeAwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion
        {
            if (RuntimeAsyncViaJitGeneratedStateMachines)
            {
                ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
                object? sentinelContinuation = state.SentinelContinuation;
                if (sentinelContinuation == null)
                    state.SentinelContinuation = sentinelContinuation = default(Continuation);

                state.Notifier = awaiter;
                SuspendAsync2(sentinelContinuation);
                return;
            }
            else
            {
                StackCrawlMark stackMark = StackCrawlMark.LookForMe;

                // Create resumption delegate, wrapping task, and create tasklets to represent each stack frame on the stack.
                // RuntimeTaskSuspender.GetOrCreateResumptionDelegate() works like a POSIX fork call in that calls to it will return a
                // delegate if they are the initial call to GetOrCreateResumptionDelegate, but once the thread is resumed,
                // it will resume with a return value of null.
                Action? resumption = RuntimeHelpers.GetOrCreateResumptionDelegate(ref stackMark);
                if (resumption != null)
                {
                    // If we reach here, the only way that we actually run follow on code is for the continuation to actually run,
                    // and return from GetOrCreateResumptionDelegate with a null return value.
                    ref AsyncDataFrame asyncFrame = ref GetCurrentAsyncDataFrame();
                    RuntimeAsyncMaintainedData maintainedData = asyncFrame._maintainedData!;
                    maintainedData._awaiter = awaiter;
                    // This function must be called from the same function that has the stackmark in it.
                    unsafe { UnwindToFunctionWithAsyncFrame(maintainedData._nextTasklet, maintainedData._suspendActive); }
                }
            }
        }

        // Could probably be a static readonly bool computing the environment
        // variable, but recognizing it as an intrinsic avoids having to deal
        // with having a static constructor that could interfere with perf
        // measurements.
        private static bool RuntimeAsyncViaJitGeneratedStateMachines
        {
            [Intrinsic]
            get => false;
        }

        [ThreadStatic]
        private static unsafe void* t_asyncData;

        internal struct RuntimeAsyncReturnValue
        {
            public RuntimeAsyncReturnValue(object? obj)
            {
                _obj = obj;
                _ptr = IntPtr.Zero;
                _returnType = TaskletReturnType.ObjectReference;
            }
            public RuntimeAsyncReturnValue(IntPtr ptr)
            {
                _obj = null;
                _ptr = ptr;
                _returnType = TaskletReturnType.Integer;
            }
            public object? _obj;
            public IntPtr _ptr;
            public TaskletReturnType _returnType;
        }

        internal abstract unsafe class RuntimeAsyncMaintainedData
        {
            public Action? _resumption;
            public Exception? _exception;
            public INotifyCompletion? _awaiter;
            public int _suspendActive;
            public bool _initialTaskEntry = true;
            public bool _completed;
            public byte _dummy;

            public ExecutionContext? _executionCtx;
            public SynchronizationContext? _syncCtx;


            public Tasklet* _nextTasklet;
            public Tasklet* _oldTaskletNext;

            public RuntimeAsyncReturnValue _retValue;
            public virtual ref byte GetReturnPointer() { return ref _dummy; }

            public Task? _task;
            public abstract Task GetTask();
        }


        // These are all implemented by the same assembly helper that will setup the tasklet in its new home on the stack
        // and then tail-call into it. We will need a different entrypoint name for each type of register based return that can happen
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object ResumeTaskletReferenceReturn(Tasklet* pTasklet, ref RuntimeAsyncReturnValue retValue);
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe IntPtr ResumeTaskletIntegerRegisterReturn(Tasklet* pTasklet, ref RuntimeAsyncReturnValue retValue);

        internal sealed class RuntimeAsyncMaintainedData<T> : RuntimeAsyncMaintainedData, ICriticalNotifyCompletion
        {
            public RuntimeAsyncMaintainedData()
            {
                _task = CompletionTask();
                _resumption = ResumptionFunc;
            }

            public unsafe void ResumptionFunc()
            {
                // Suspension has finished and we are resuming
                _suspendActive = 0;

                // Once we perform a resumption we no longer need to worry about handling the ultimate return data from the run of Tasklets
                _initialTaskEntry = false;

                int collectiveStackAllocsPerformed = 0;

                try
                {
                    AsyncDataFrame dataFrame = new AsyncDataFrame(this);
                    PushAsyncData(ref dataFrame);
                    try
                    {
                        // PushAsyncData will fill in the value of _previousExecutionCtx and _previousSyncCtx
                        if (_syncCtx != dataFrame._previousSyncCtx)
                        {
                            // TODO: Possibly we should marshal to the right sync context? I dunno. We'll figure this out
                            dataFrame._currentThread._synchronizationContext = _syncCtx;
                        }
                        if (_executionCtx != dataFrame._previousExecutionCtx)
                        {
                            ExecutionContext.RestoreChangedContextToThread(dataFrame._currentThread, _executionCtx, dataFrame._previousExecutionCtx);
                        }

                        while (_nextTasklet != null)
                        {
                            Tasklet* pCurTasklet = _nextTasklet;
                            int maxStackNeeded = pCurTasklet->GetMaxStackNeeded();
                            _nextTasklet = pCurTasklet->pTaskletNextInStack;
                            if (maxStackNeeded > collectiveStackAllocsPerformed)
                            {
#pragma warning disable CA2014
                                // This won't stack overflow unless MaxStackNeeded is actually too high, as the extra allocation is controlled by collectiveStackAllocsPerformed
                                // TODO This is doing terrible things with the ABI, so we may need to be more careful here
                                int stackToAlloc = maxStackNeeded - collectiveStackAllocsPerformed;
                                byte* pStackAlloc = stackalloc byte[stackToAlloc];
                                collectiveStackAllocsPerformed += stackToAlloc;
#pragma warning restore CA2014
                                // The optimizer does nothing with variable sized StackAlloc KeepStackAllocAlive(pStackAlloc);
                            }

                            try
                            {
                                switch (pCurTasklet->taskletReturnType)
                                {
                                    case TaskletReturnType.ObjectReference:
                                        _retValue = new RuntimeAsyncReturnValue(ResumeTaskletReferenceReturn(pCurTasklet, ref _retValue));
                                        break;
                                    case TaskletReturnType.Integer:
                                        _retValue = new RuntimeAsyncReturnValue(ResumeTaskletIntegerRegisterReturn(pCurTasklet, ref _retValue));
                                        break;
                                    case TaskletReturnType.ByReference:
                                        throw new NotImplementedException(); // This will be awkward (but not impossible) to implement. Hold off for now
                                }
                            }
                            finally
                            {
                                DeleteTasklet(pCurTasklet);
                            }

                            if (_suspendActive != 0)
                            {
                                // TODO: we should capture the sync and execution contexts at this point
                                break;
                            }
                        }
                    }
                    finally
                    {
                        PopAsyncData(ref dataFrame);
                    }
                }
                catch (Exception e)
                {
                    SetException(e);
                }

                if (_suspendActive != 0)
                {
                    // We suspended again. Just register to return to this function, and return
                    _awaiter!.OnCompleted(_resumption!);
                    return;
                }
                // We are actually done. Set the final result
                SetResultDone();
            }

            private Action? _taskResumer;
            private T? _returnData;
            public override ref byte GetReturnPointer()
            {
                return ref Unsafe.As<T, byte>(ref _returnData!);
            }

            public RuntimeAsyncMaintainedData<T> GetAwaiter()
            {
                return this;
            }

            public bool IsCompleted => _completed;

            public override Task GetTask()
            {
                _awaiter!.OnCompleted(_resumption!);
                return _task!;
            }

            public void SetException(Exception exception)
            {
                _exception = exception;
                _completed = true;
                _taskResumer!();
            }

            public void SetResultDone()
            {
                switch (_retValue._returnType)
                {
                    case TaskletReturnType.Integer:
                        _returnData = Unsafe.As<IntPtr, T>(ref _retValue._ptr);
                        Thread.MemoryBarrier();
                        break;
                    case TaskletReturnType.ObjectReference:
                        _returnData = Unsafe.As<object, T>(ref _retValue!._obj!)!;
                        break;
                    default:
                        // Other possibiilities not yet implemented
                        throw new NotImplementedException();
                }
                _completed = true;
                _taskResumer!();
            }

            public void OnCompleted(Action resumer) { throw new NotSupportedException(); }
            public void UnsafeOnCompleted(Action resumer) { _taskResumer = resumer; }
            public T? GetResult() { if (_exception != null) throw _exception; return _returnData; }

            private async Task<T?> CompletionTask()
            {
                return await this;
            }
        }

        internal unsafe struct AsyncDataFrame
        {
            public AsyncDataFrame(RuntimeAsyncMaintainedData maintainedData)
            {
                _maintainedData = maintainedData;
                _crawlMark = StackCrawlMark.LookForMe;
                _next = null;
                _createRuntimeMaintainedData = null;
            }

            public AsyncDataFrame(Func<RuntimeAsyncMaintainedData> getMaintainedData)
            {
                _maintainedData = null;
                _crawlMark = StackCrawlMark.LookForMe;
                _next = null;
                _createRuntimeMaintainedData = getMaintainedData;
            }

            public RuntimeAsyncMaintainedData? _maintainedData;
            public StackCrawlMark _crawlMark;
            public void* _next;
            public Func<RuntimeAsyncMaintainedData>? _createRuntimeMaintainedData;
            public Thread _currentThread = Thread.CurrentThread;

            // Store current ExecutionContext and SynchronizationContext as "previousXxx".
            // This allows us to restore them and undo any Context changes made in stateMachine.MoveNext
            // so that they won't "leak" out of the first await.
            public ExecutionContext? _previousExecutionCtx;
            public SynchronizationContext? _previousSyncCtx;
        }

        internal enum TaskletReturnType
        {
            // These return types are OS/architecture specific. For instance, Arm64 supports returning structs in a register pair
            Integer,
            ObjectReference,
            ByReference
        }

        internal struct StackDataInfo
        {
            public int StackRequirement;
            // And native has a bunch of other fields that managed does not use.
        }
        internal unsafe struct Tasklet
        {
            public Tasklet* pTaskletNextInStack;
            public Tasklet* pTaskletNextInLiveList;
            public Tasklet* pTaskletPrevInLiveList;
            public byte* pStackData;
            public IntPtr restoreIPAddress;
            public StackDataInfo* pStackDataInfo;
            public TaskletReturnType taskletReturnType;

            public int GetMaxStackNeeded() { return pStackDataInfo->StackRequirement; }
        }

        internal static unsafe void PushAsyncData(ref AsyncDataFrame asyncData)
        {
            asyncData._next = t_asyncData;
            asyncData._previousExecutionCtx = asyncData._currentThread._executionContext;
            asyncData._previousSyncCtx = asyncData._currentThread._synchronizationContext;

            t_asyncData = Unsafe.AsPointer(ref asyncData);
        }

        internal static unsafe void PopAsyncData(ref AsyncDataFrame asyncData)
        {
            t_asyncData = Unsafe.AsRef<AsyncDataFrame>(t_asyncData)._next;

            // The common case is that these have not changed, so avoid the cost of a write barrier if not needed.
            if (asyncData._previousSyncCtx != asyncData._currentThread._synchronizationContext)
            {
                // Restore changed SynchronizationContext back to previous
                asyncData._currentThread._synchronizationContext = asyncData._previousSyncCtx;
            }

            ExecutionContext? currentExecutionCtx = asyncData._currentThread._executionContext;
            if (asyncData._previousExecutionCtx != currentExecutionCtx)
            {
                ExecutionContext.RestoreChangedContextToThread(asyncData._currentThread, asyncData._previousExecutionCtx, currentExecutionCtx);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static unsafe bool HasCurrentAsyncDataFrame()
        {
            return t_asyncData != null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe ref AsyncDataFrame GetCurrentAsyncDataFrame()
        {
            return ref Unsafe.AsRef<AsyncDataFrame>(t_asyncData);
        }

        // Capture stack into a series of tasklets (one per stack frame)
        // These tasklets hold the stack data for a particular frame, as well as the contents of the saved registers as needed by that frame, GC data for reporting the frame, and data for restoring the frame.
        // To make this work.
        // 1. All addresses of locals are to be used as byrefs
        // 2. Frame pointers are to be reported as byrefs
        // 3. Return values are to be returned by reference in all cases where the return value is not a simple object return or return of a simple value in the return value register (this makes the resumption function reasonable to write. Notably, floating point, and ref return will be returned by reference as well as generalized struct return, and return which would normally involve multiple return value registers)
        // 4. There are to be no refs to the outermost caller function exceptn for the valuetype return address (methods which begin on an instance valuetype will have the thunk box the valuetype and the runtime async method on the boxed instance)
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeSuspension_CaptureTasklets")]
        private static unsafe partial Tasklet* CaptureCurrentStackIntoTasklets(StackCrawlMarkHandle stackMarkTop, ref byte returnValueHandle, [MarshalAs(UnmanagedType.U1)] bool useReturnValueHandle, void* taskAsyncData, out Tasklet* lastTasklet, out int framesCaptured);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeSuspension_DeleteTasklet")]
        private static unsafe partial void DeleteTasklet(Tasklet* tasklet);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void UnwindToFunctionWithAsyncFrame(Tasklet* topTasklet, nint framesToUnwind);

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        private static unsafe Action? GetOrCreateResumptionDelegate(ref StackCrawlMark stackMark)
        {
            ref AsyncDataFrame asyncFrame = ref GetCurrentAsyncDataFrame();

            asyncFrame._maintainedData ??= asyncFrame._createRuntimeMaintainedData!();

            RuntimeAsyncMaintainedData maintainedData = asyncFrame._maintainedData;

            Tasklet* lastTasklet = null;
            Tasklet* nextTaskletInStack = CaptureCurrentStackIntoTasklets(new StackCrawlMarkHandle(ref stackMark), ref maintainedData.GetReturnPointer(), maintainedData._initialTaskEntry, t_asyncData, out lastTasklet, out var framesCaptured);
            if (nextTaskletInStack == null)
                throw new OutOfMemoryException();

            maintainedData._oldTaskletNext = maintainedData._nextTasklet;
            lastTasklet->pTaskletNextInStack = maintainedData._nextTasklet;
            maintainedData._nextTasklet = nextTaskletInStack;
            maintainedData._retValue = default(RuntimeAsyncReturnValue);

            maintainedData._suspendActive = framesCaptured;

            maintainedData._executionCtx = asyncFrame._currentThread._executionContext;
            maintainedData._syncCtx = asyncFrame._currentThread._synchronizationContext;


            return maintainedData._resumption;
        }
#endif
    }
}
