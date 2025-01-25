// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

namespace System.Threading
{
    internal readonly struct ThreadHandle
    {
        private readonly IntPtr _ptr;

        internal ThreadHandle(IntPtr pThread)
        {
            _ptr = pThread;
        }
    }

    public sealed partial class Thread
    {
        /*=========================================================================
        ** Data accessed from managed code that needs to be defined in
        ** ThreadBaseObject to maintain alignment between the two classes.
        ** DON'T CHANGE THESE UNLESS YOU MODIFY ThreadBaseObject in vm\object.h
        =========================================================================*/
        internal ExecutionContext? _executionContext; // this call context follows the logical thread
        internal SynchronizationContext? _synchronizationContext; // maintained separately from ExecutionContext

        private string? _name;
        private StartHelper? _startHelper;

        /*=========================================================================
        ** The base implementation of Thread is all native.  The following fields
        ** should never be used in the C# code.  They are here to define the proper
        ** space so the thread object may be allocated.  DON'T CHANGE THESE UNLESS
        ** YOU MODIFY ThreadBaseObject in vm\object.h
        =========================================================================*/
#pragma warning disable CA1823, 169 // These fields are not used from managed.
        // IntPtrs need to be together, and before ints, because IntPtrs are 64-bit
        // fields on 64-bit platforms, where they will be sorted together.

        private IntPtr _DONT_USE_InternalThread; // Pointer
        private int _priority; // INT32

        // The following field is required for interop with the VS Debugger
        // Prior to making any changes to this field, please reach out to the VS Debugger
        // team to make sure that your changes are not going to prevent the debugger
        // from working.
        private int _managedThreadId; // INT32
#pragma warning restore CA1823, 169

        // This is used for a quick check on thread pool threads after running a work item to determine if the name, background
        // state, or priority were changed by the work item, and if so to reset it. Other threads may also change some of those,
        // but those types of changes may race with the reset anyway, so this field doesn't need to be synchronized.
        private bool _mayNeedResetForThreadPool;

        // Set in unmanaged and read in managed code.
        private bool _isDead;
        private bool _isThreadPool;

        private Thread() { }

        public int ManagedThreadId
        {
            [Intrinsic]
            get => _managedThreadId;
        }

        /// <summary>Returns handle for interop with EE. The handle is guaranteed to be non-null.</summary>
        internal ThreadHandle GetNativeHandle()
        {
            IntPtr thread = _DONT_USE_InternalThread;

            // This should never happen under normal circumstances.
            if (thread == IntPtr.Zero)
            {
                throw new ThreadStateException(SR.Argument_InvalidHandle);
            }

            return new ThreadHandle(thread);
        }

        private unsafe void StartCore()
        {
            lock (this)
            {
                fixed (char* pThreadName = _name)
                {
                    StartInternal(GetNativeHandle(), _startHelper?._maxStackSize ?? 0, _priority, _isThreadPool ? Interop.BOOL.TRUE : Interop.BOOL.FALSE, pThreadName);
                }
            }
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_Start")]
        private static unsafe partial void StartInternal(ThreadHandle t, int stackSize, int priority, Interop.BOOL isThreadPool, char* pThreadName);

        // Called from the runtime
        private void StartCallback()
        {
            StartHelper? startHelper = _startHelper;
            Debug.Assert(startHelper != null);
            _startHelper = null;

            startHelper.Run();
        }

        /// <summary>
        /// Suspends the current thread for timeout milliseconds. If timeout == 0,
        /// forces the thread to give up the remainder of its timeslice.  If timeout
        /// == Timeout.Infinite, no timeout will occur.
        /// </summary>
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_Sleep")]
        private static partial void SleepInternal(int millisecondsTimeout);

        // Max iterations to be done in SpinWait without switching GC modes.
        private const int SpinWaitCoopThreshold = 1024;

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_SpinWait")]
        [SuppressGCTransition]
        private static partial void SpinWaitInternal(int iterations);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_SpinWait")]
        private static partial void LongSpinWaitInternal(int iterations);

        [MethodImpl(MethodImplOptions.NoInlining)] // Slow path method. Make sure that the caller frame does not pay for PInvoke overhead.
        private static void LongSpinWait(int iterations) => LongSpinWaitInternal(iterations);

        /// <summary>
        /// Wait for a length of time proportional to 'iterations'.  Each iteration is should
        /// only take a few machine instructions.  Calling this API is preferable to coding
        /// a explicit busy loop because the hardware can be informed that it is busy waiting.
        /// </summary>
        public static void SpinWait(int iterations)
        {
            if (iterations < SpinWaitCoopThreshold)
            {
                SpinWaitInternal(iterations);
            }
            else
            {
                LongSpinWait(iterations);
            }
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_YieldThread")]
        private static partial Interop.BOOL YieldInternal();

        public static bool Yield() => YieldInternal() != Interop.BOOL.FALSE;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Thread InitializeCurrentThread()
        {
            Thread? thread = null;
            GetCurrentThread(ObjectHandleOnStack.Create(ref thread));
            return t_currentThread = thread!;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_GetCurrentThread")]
        private static partial void GetCurrentThread(ObjectHandleOnStack thread);

        private void Initialize()
        {
            Thread _this = this;
            Initialize(ObjectHandleOnStack.Create(ref _this));
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_Initialize")]
        private static partial void Initialize(ObjectHandleOnStack thread);

        /// <summary>Clean up the thread when it goes away.</summary>
        ~Thread() => InternalFinalize(); // Delegate to the unmanaged portion.

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void InternalFinalize();

        private void ThreadNameChanged(string? value)
        {
            InformThreadNameChange(GetNativeHandle(), value, value?.Length ?? 0);
            GC.KeepAlive(this);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_InformThreadNameChange", StringMarshalling = StringMarshalling.Utf16)]
        private static partial void InformThreadNameChange(ThreadHandle t, string? name, int len);

        /// <summary>Returns true if the thread has been started and is not dead.</summary>
        public bool IsAlive => (ThreadState & (ThreadState.Unstarted | ThreadState.Stopped | ThreadState.Aborted)) == 0;

        /// <summary>
        /// Return whether or not this thread is a background thread.  Background
        /// threads do not affect when the Execution Engine shuts down.
        /// </summary>
        public bool IsBackground
        {
            get
            {
                if (_isDead)
                {
                    throw new ThreadStateException(SR.ThreadState_Dead_State);
                }

                Interop.BOOL res = GetIsBackground(GetNativeHandle());
                GC.KeepAlive(this);
                return res != Interop.BOOL.FALSE;
            }
            set
            {
                if (_isDead)
                {
                    throw new ThreadStateException(SR.ThreadState_Dead_State);
                }

                SetIsBackground(GetNativeHandle(), value ? Interop.BOOL.TRUE : Interop.BOOL.FALSE);
                GC.KeepAlive(this);
                if (!value)
                {
                    _mayNeedResetForThreadPool = true;
                }
            }
        }

        [SuppressGCTransition]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_GetIsBackground")]
        private static partial Interop.BOOL GetIsBackground(ThreadHandle t);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_SetIsBackground")]
        private static partial void SetIsBackground(ThreadHandle t, Interop.BOOL value);

        /// <summary>Returns true if the thread is a threadpool thread.</summary>
        public bool IsThreadPoolThread
        {
            get
            {
                if (_isDead)
                {
                    throw new ThreadStateException(SR.ThreadState_Dead_State);
                }

                return _isThreadPool;
            }
            internal set
            {
                Debug.Assert(value);
                Debug.Assert(!_isDead);
                Debug.Assert(((ThreadState & ThreadState.Unstarted) != 0)
#if TARGET_WINDOWS
                    || ThreadPool.UseWindowsThreadPool
#endif
                );
                _isThreadPool = value;
            }
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_SetPriority")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial void SetPriority(ObjectHandleOnStack thread, int priority);

        /// <summary>Returns the priority of the thread.</summary>
        public ThreadPriority Priority
        {
            get
            {
                if (_isDead)
                {
                    throw new ThreadStateException(SR.ThreadState_Dead_Priority);
                }
                return (ThreadPriority)_priority;
            }
            set
            {
                Thread _this = this;
                SetPriority(ObjectHandleOnStack.Create(ref _this), (int)value);
                _mayNeedResetForThreadPool = true;
            }
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_GetCurrentOSThreadId")]
        private static partial ulong GetCurrentOSThreadId();

        /// <summary>
        /// Return the thread state as a consistent set of bits.  This is more
        /// general then IsAlive or IsBackground.
        /// </summary>
        public ThreadState ThreadState
        {
            get
            {
                var state = (ThreadState)GetThreadState(GetNativeHandle());
                GC.KeepAlive(this);
                return state;
            }
        }

        [SuppressGCTransition]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_GetThreadState")]
        private static partial int GetThreadState(ThreadHandle t);

        /// <summary>
        /// An unstarted thread can be marked to indicate that it will host a
        /// single-threaded or multi-threaded apartment.
        /// </summary>
#if FEATURE_COMINTEROP_APARTMENT_SUPPORT
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_GetApartmentState")]
        private static partial int GetApartmentState(ObjectHandleOnStack t);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_SetApartmentState")]
        private static partial int SetApartmentState(ObjectHandleOnStack t, int state);

        public ApartmentState GetApartmentState()
        {
            Thread _this = this;
            return (ApartmentState)GetApartmentState(ObjectHandleOnStack.Create(ref _this));
        }

        private bool SetApartmentStateUnchecked(ApartmentState state, bool throwOnError)
        {
            ApartmentState retState;
            lock (this) // This lock is only needed when the this is not the current thread.
            {
                Thread _this = this;
                retState = (ApartmentState)SetApartmentState(ObjectHandleOnStack.Create(ref _this), (int)state);
            }

            // Special case where we pass in Unknown and get back MTA.
            //  Once we CoUninitialize the thread, the OS will still
            //  report the thread as implicitly in the MTA if any
            //  other thread in the process is CoInitialized.
            if ((state == ApartmentState.Unknown) && (retState == ApartmentState.MTA))
            {
                return true;
            }

            if (retState != state)
            {
                if (throwOnError)
                {
                    string msg = SR.Format(SR.Thread_ApartmentState_ChangeFailed, retState);
                    throw new InvalidOperationException(msg);
                }

                return false;
            }

            return true;
        }

#else // FEATURE_COMINTEROP_APARTMENT_SUPPORT
        public ApartmentState GetApartmentState() => ApartmentState.Unknown;

        private static bool SetApartmentStateUnchecked(ApartmentState state, bool throwOnError)
        {
            if (state != ApartmentState.Unknown)
            {
                if (throwOnError)
                {
                    throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
                }

                return false;
            }

            return true;
        }
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

#if FEATURE_COMINTEROP
        public void DisableComObjectEagerCleanup()
        {
            DisableComObjectEagerCleanup(GetNativeHandle());
            GC.KeepAlive(this);
        }

        [SuppressGCTransition]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_DisableComObjectEagerCleanup")]
        private static partial void DisableComObjectEagerCleanup(ThreadHandle t);
#else // !FEATURE_COMINTEROP
        public void DisableComObjectEagerCleanup() { }
#endif // FEATURE_COMINTEROP

        /// <summary>
        /// Interrupts a thread that is inside a Wait(), Sleep() or Join().  If that
        /// thread is not currently blocked in that manner, it will be interrupted
        /// when it next begins to block.
        /// </summary>
        public void Interrupt()
        {
            Interrupt(GetNativeHandle());
            GC.KeepAlive(this);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_Interrupt")]
        private static partial void Interrupt(ThreadHandle t);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_Join")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool Join(ObjectHandleOnStack thread, int millisecondsTimeout);

        /// <summary>
        /// Waits for the thread to die or for timeout milliseconds to elapse.
        /// </summary>
        /// <returns>
        /// Returns true if the thread died, or false if the wait timed out. If
        /// -1 is given as the parameter, no timeout will occur.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">if timeout &lt; -1 (Timeout.Infinite)</exception>
        /// <exception cref="ThreadInterruptedException">if the thread is interrupted while waiting</exception>
        /// <exception cref="ThreadStateException">if the thread has not been started yet</exception>
        public bool Join(int millisecondsTimeout)
        {
            // Validate the timeout
            if (millisecondsTimeout < 0 && millisecondsTimeout != Timeout.Infinite)
            {
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            }

            Thread _this = this;
            return Join(ObjectHandleOnStack.Create(ref _this), millisecondsTimeout);
        }

        /// <summary>
        /// Max value to be passed into <see cref="SpinWait(int)"/> for optimal delaying. This value is normalized to be
        /// appropriate for the processor.
        /// </summary>
        internal static int OptimalMaxSpinWaitsPerSpinIteration
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        private static class DirectOnThreadLocalData
        {
            // Special Thread Static variable which is always allocated at the address of the Thread variable in the ThreadLocalData of the current thread
            [ThreadStatic]
            public static IntPtr pNativeThread;
        }

        /// <summary>
        /// Get the ThreadStaticBase used for this threads TLS data. This ends up being a pointer to the pNativeThread field on the ThreadLocalData,
        /// which is at a well known offset from the start of the ThreadLocalData
        /// </summary>
        ///
        /// <remarks>
        /// We use BypassReadyToRunAttribute to ensure that this method is not compiled using ReadyToRun. This avoids an issue where we might
        /// fail to use the JIT_GetNonGCThreadStaticBaseOptimized2 JIT helpers to access the field, which would result in a stack overflow, as accessing
        /// this field would recursively call this method.
        /// </remarks>
        [System.Runtime.BypassReadyToRunAttribute]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe StaticsHelpers.ThreadLocalData* GetThreadStaticsBase()
        {
            return (StaticsHelpers.ThreadLocalData*)(((byte*)Unsafe.AsPointer(ref DirectOnThreadLocalData.pNativeThread)) - sizeof(StaticsHelpers.ThreadLocalData));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ResetFinalizerThread()
        {
            Debug.Assert(this == CurrentThread);

            if (_mayNeedResetForThreadPool)
            {
                ResetFinalizerThreadSlow();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ResetFinalizerThreadSlow()
        {
            Debug.Assert(this == CurrentThread);
            Debug.Assert(_mayNeedResetForThreadPool);

            _mayNeedResetForThreadPool = false;

            const string FinalizerThreadName = ".NET Finalizer";

            if (Name != FinalizerThreadName)
            {
                Name = FinalizerThreadName;
            }

            if (!IsBackground)
            {
                IsBackground = true;
            }

            if (Priority != ThreadPriority.Highest)
            {
                Priority = ThreadPriority.Highest;
            }
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_PollGC")]
        private static partial void ThreadNative_PollGC();

        // GC Suspension is done by simply dropping into native code via p/invoke, and we reuse the p/invoke
        // mechanism for suspension. On all architectures we should have the actual stub used for the check be implemented
        // as a small assembly stub which checks the global g_TrapReturningThreads flag and tail-call to this helper
        private static unsafe void PollGC()
        {
            NativeThreadState catchAtSafePoint = ((NativeThreadClass*)Thread.DirectOnThreadLocalData.pNativeThread)->m_State & NativeThreadState.TS_CatchAtSafePoint;
            if (catchAtSafePoint != NativeThreadState.None)
            {
                ThreadNative_PollGC();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeThreadClass
        {
            public NativeThreadState m_State;
        }

        private enum NativeThreadState
        {
            None                      = 0,
            TS_AbortRequested         = 0x00000001,    // Abort the thread
            TS_DebugSuspendPending    = 0x00000008,    // Is the debugger suspending threads?
            TS_GCOnTransitions        = 0x00000010,    // Force a GC on stub transitions (GCStress only)

        // We require (and assert) that the following bits are less than 0x100.
            TS_CatchAtSafePoint = (TS_AbortRequested | TS_DebugSuspendPending | TS_GCOnTransitions),
        };
    }
}
