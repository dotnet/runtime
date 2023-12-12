// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime
{
    internal sealed class BypassReadyToRunAttribute : Attribute {}
}


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
        [MethodImpl(MethodImplOptions.NoInlining)]
        [BypassReadyToRun]
        public static void AwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion
        {
            if (RuntimeAsyncViaJitGeneratedStateMachines)
            {
                ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
                Continuation? sentinelContinuation = state.SentinelContinuation;
                if (sentinelContinuation == null)
                    state.SentinelContinuation = sentinelContinuation = new Continuation();

                state.Notifier = awaiter;
                SuspendAsync2(sentinelContinuation);
                return;
            }
            else
            {
                StackCrawlMark stackMark = StackCrawlMark.LookForMe;

                // Create resumption delegate, wrapping task, and create tasklets to represent each stack frame on the stack.
                // RuntimeTaskSuspender.DoSetupAndCapture() works like a POSIX fork call in that calls to it will return a
                // true for the initial call to DoSetupAndCapture, but once the thread is resumed,
                // it will resume with a return value of null.
                if (RuntimeHelpers.DoSetupAndCapture(ref stackMark))
                {
                    // If we reach here, the only way that we actually run follow on code is for the continuation to actually run,
                    // and return from DoSetupAndCapture with a false return value
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
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void UnsafeAwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion
        {
            if (RuntimeAsyncViaJitGeneratedStateMachines)
            {
                ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
                Continuation? sentinelContinuation = state.SentinelContinuation;
                if (sentinelContinuation == null)
                    state.SentinelContinuation = sentinelContinuation = new Continuation();

                state.Notifier = awaiter;
                SuspendAsync2(sentinelContinuation);
                return;
            }
            else
            {
                StackCrawlMark stackMark = StackCrawlMark.LookForMe;

                // Create resumption delegate, wrapping task, and create tasklets to represent each stack frame on the stack.
                // RuntimeTaskSuspender.DoSetupAndCapture() works like a POSIX fork call in that calls to it will return a
                // true for the initial call to DoSetupAndCapture, but once the thread is resumed,
                // it will resume with a return value of false.
                if (RuntimeHelpers.DoSetupAndCapture(ref stackMark))
                {
                    // If we reach here, the only way that we actually run follow on code is for the continuation to actually run,
                    // and return from DoSetupAndCapture with a false return value
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

        internal class RuntimeAsyncMaintainedData
        {
            public Exception? _exception;
            public INotifyCompletion? _awaiter;
            public int _suspendActive;
            public bool _initialTaskEntry = true;
            public bool _completed;
            public byte _dummy;

            public unsafe Tasklet* _nextTasklet;

            public RuntimeAsyncReturnValue _retValue;
            public virtual ref byte GetReturnPointer() { return ref _dummy; }

            public Task? _task;

            public virtual Task GetTask()
            {
                return CompletionTaskReturnVoid();
            }

            public unsafe bool ResumptionFunc()
            {
                // Suspension has finished and we are resuming
                _suspendActive = 0;

                // Once we perform a resumption we no longer need to worry about handling the ultimate return data from the run of Tasklets
                _initialTaskEntry = false;

                int collectiveStackAllocsPerformed = 0;

                AsyncDataFrame dataFrame = new AsyncDataFrame(this);
                PushAsyncData(ref dataFrame);
                try
                {
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

                return _suspendActive!= 0;
            }

            protected sealed class TempAwaitable : ICriticalNotifyCompletion
            {
                public INotifyCompletion? _awaiter;

                public bool IsCompleted => false;

                public void OnCompleted(Action action)
                {
                    _awaiter!.OnCompleted(action);
                }

                public TempAwaitable GetAwaiter() { return this; }

                public void UnsafeOnCompleted(Action action)
                {
                    if (_awaiter is ICriticalNotifyCompletion criticalNotification)
                    {
                        criticalNotification.UnsafeOnCompleted(action);
                    }
                    else
                    {
                        _awaiter!.OnCompleted(action);
                    }
                }

                public void GetResult() {}
            }

            private async Task CompletionTaskReturnVoid()
            {
                TempAwaitable awaitableObject = new TempAwaitable();
                bool keepProcessing = true;
                while (keepProcessing)
                {
                    awaitableObject._awaiter = _awaiter!;
                    await awaitableObject;
                    keepProcessing = ResumptionFunc();
                }
            }
        }

        // These are all implemented by the same assembly helper that will setup the tasklet in its new home on the stack
        // and then tail-call into it. We will need a different entrypoint name for each type of register based return that can happen
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object ResumeTaskletReferenceReturn(Tasklet* pTasklet, ref RuntimeAsyncReturnValue retValue);
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe IntPtr ResumeTaskletIntegerRegisterReturn(Tasklet* pTasklet, ref RuntimeAsyncReturnValue retValue);

        internal sealed class RuntimeAsyncMaintainedData<T> : RuntimeAsyncMaintainedData
        {
            private T? _returnData;
            public override ref byte GetReturnPointer()
            {
                return ref Unsafe.As<T, byte>(ref _returnData!);
            }

            public override Task GetTask()
            {
                return CompletionTask();
            }

            private async Task<T?> CompletionTask()
            {
                TempAwaitable awaitableObject = new TempAwaitable();
                bool keepProcessing = true;
                while (keepProcessing)
                {
                    awaitableObject._awaiter = _awaiter!;
                    await awaitableObject;
                    keepProcessing = ResumptionFunc();
                }

                switch (_retValue._returnType)
                {
                    case TaskletReturnType.Integer:
                        return Unsafe.As<IntPtr, T>(ref _retValue._ptr);
                    case TaskletReturnType.ObjectReference:
                        return Unsafe.As<object, T>(ref _retValue!._obj!)!;
                    default:
                        // Other possibiilities not yet implemented
                        throw new NotImplementedException();
                }
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
            public int minGeneration;
            public Tasklet* pTaskletPrevInStack;

            public int GetMaxStackNeeded() { return pStackDataInfo->StackRequirement; }
        }

        internal static unsafe void PushAsyncData(ref AsyncDataFrame asyncData)
        {
            asyncData._next = t_asyncData;
            t_asyncData = Unsafe.AsPointer(ref asyncData);
        }

        internal static unsafe void PopAsyncData(ref AsyncDataFrame asyncData)
        {
            t_asyncData = asyncData._next;
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

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeSuspension_RegisterTasklet")]
        private static unsafe partial void RegisterTasklet(Tasklet* tasklet);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void UnwindToFunctionWithAsyncFrame(Tasklet* topTasklet, nint framesToUnwind);

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        private static unsafe bool DoSetupAndCapture(ref StackCrawlMark stackMark)
        {
            ref AsyncDataFrame asyncFrame = ref GetCurrentAsyncDataFrame();

            asyncFrame._maintainedData ??= asyncFrame._createRuntimeMaintainedData!();

            RuntimeAsyncMaintainedData maintainedData = asyncFrame._maintainedData;

            Tasklet* lastTasklet = null;
            Tasklet* nextTaskletInStack = CaptureCurrentStackIntoTasklets(new StackCrawlMarkHandle(ref stackMark), ref maintainedData.GetReturnPointer(), maintainedData._initialTaskEntry, t_asyncData, out lastTasklet, out var framesCaptured);
            if (nextTaskletInStack == null)
                throw new OutOfMemoryException();

            if (maintainedData._nextTasklet == null)
            {
                RegisterTasklet(lastTasklet);
            }
            else
            {
                maintainedData._nextTasklet->pTaskletPrevInStack = lastTasklet;
                lastTasklet->pTaskletNextInStack = maintainedData._nextTasklet;

                // we are suspending, so change the age of tasklets from -1 (active) to 0 (very young)
                // NOTE: this is only needed as long as we allow cross-frame byrefs as
                // that forces us to make age of active tasklets undefined (see comment in PlatformIndependentRestore)
                // without such byrefs old tasklets could stay old as nothing could change locals of a captured frame.
                for (Tasklet* current = maintainedData._nextTasklet; current != null; current = current->pTaskletNextInStack)
                {
                    if (current->minGeneration == -1)
                    {
                        current->minGeneration = 0;
                    }
                }
            }

            maintainedData._nextTasklet = nextTaskletInStack;
            maintainedData._retValue = default(RuntimeAsyncReturnValue);

            maintainedData._suspendActive = framesCaptured;

            return true;
        }
#endif
    }
}
