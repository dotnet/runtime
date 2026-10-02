// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    public sealed partial class Thread
    {
        private void Initialize()
        {
            Thread _this = this;
            Initialize(ObjectHandleOnStack.Create(ref _this));
        }

        [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_Initialize")]
        private static partial void Initialize(ObjectHandleOnStack thread);

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        [DebuggerStepThrough]
        internal static unsafe StaticsHelpers.ThreadLocalData* GetThreadStaticsBase()
        {
            return (StaticsHelpers.ThreadLocalData*)(((byte*)Unsafe.AsPointer(ref DirectOnThreadLocalData.pNativeThread)) - sizeof(StaticsHelpers.ThreadLocalData));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Thread InitializeCurrentThread()
        {
            Thread? thread = null;
            GetCurrentThread(ObjectHandleOnStack.Create(ref thread));
            return t_currentThread = thread!;
        }

        [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_GetCurrentThread")]
        private static partial void GetCurrentThread(ObjectHandleOnStack thread);

        [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_GetCurrentOSThreadId")]
        private static partial ulong GetCurrentOSThreadId();

        [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_YieldThread")]
        private static partial Interop.BOOL YieldInternal();

        public static bool Yield() => YieldInternal() != Interop.BOOL.FALSE;

        partial void StartCore()
        {
            lock (this)
            {
                unsafe
                {
                    fixed (char* pThreadName = _name)
                    {
                        Exception? exception = null;
                        if (StartInternal(GetNativeHandle(), _startHelper?._maxStackSize ?? 0, _priority, _isThreadPool ? Interop.BOOL.TRUE : Interop.BOOL.FALSE, pThreadName, ObjectHandleOnStack.Create(ref exception)) == Interop.BOOL.FALSE)
                        {
                            throw new ThreadStartException(exception);
                        }
                    }
                }
            }
        }

        [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_Start")]
        private static unsafe partial Interop.BOOL StartInternal(ThreadHandle t, int stackSize, int priority, Interop.BOOL isThreadPool, char* pThreadName, ObjectHandleOnStack exception);

        [UnmanagedCallersOnly]
        private static unsafe void StartCallback(Thread* pThread)
        {
            StartHelper? startHelper = pThread->_startHelper;
            Debug.Assert(startHelper != null);
            pThread->_startHelper = null;

            startHelper.Run();

            // When this thread is about to exit, inform any subsystems that need to know.
            // For external threads that have been attached to the runtime, we'll call this
            // after the thread has been detached as it won't come through this path.
            pThread->OnThreadExited();
        }

        partial void ThreadNameChanged(string? value)
        {
            InformThreadNameChange(GetNativeHandle(), value, value?.Length ?? 0);
            GC.KeepAlive(this);
        }

        [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_InformThreadNameChange", StringMarshalling = StringMarshalling.Utf16)]
        private static partial void InformThreadNameChange(ThreadHandle t, string? name, int len);

        [SuppressGCTransition]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_GetIsBackground")]
        private static partial Interop.BOOL GetIsBackground(ThreadHandle t);

        [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_SetIsBackground")]
        private static partial void SetIsBackground(ThreadHandle t, Interop.BOOL value);

        private bool GetIsBackgroundCore()
        {
            Interop.BOOL res = GetIsBackground(GetNativeHandle());
            GC.KeepAlive(this);
            return res != Interop.BOOL.FALSE;
        }

        private void SetIsBackgroundCore(bool value)
        {
            SetIsBackground(GetNativeHandle(), value ? Interop.BOOL.TRUE : Interop.BOOL.FALSE);
            GC.KeepAlive(this);
            if (!value)
            {
                _mayNeedResetForThreadPool = true;
            }
        }

        private void SetPriorityCore(ThreadPriority value)
        {
            Thread _this = this;
            SetPriority(ObjectHandleOnStack.Create(ref _this), (int)value);
            _mayNeedResetForThreadPool = true;
        }

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

        [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_SetPriority")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial void SetPriority(ObjectHandleOnStack thread, int priority);

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

        private ThreadState GetThreadStateCore()
        {
            ThreadState state = (ThreadState)GetThreadState(GetNativeHandle());
            GC.KeepAlive(this);
            return state;
        }

        /// <summary>
        /// An unstarted thread can be marked to indicate that it will host a
        /// single-threaded or multi-threaded apartment.
        /// </summary>
#if FEATURE_COMINTEROP_APARTMENT_SUPPORT
        [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_GetApartmentState")]
        private static partial int GetApartmentState(ObjectHandleOnStack t);

        [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
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

        internal static bool ReentrantWaitsEnabled =>
            CurrentThread.GetApartmentState() == ApartmentState.STA;

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

        internal const bool ReentrantWaitsEnabled = false;
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
#if TARGET_UNIX || TARGET_BROWSER || TARGET_WASI
            WaitSubsystem.Interrupt(this);
#else
            Interrupt(GetNativeHandle());
            GC.KeepAlive(this);
#endif
        }

#if TARGET_WINDOWS
        [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_Interrupt")]
        private static partial void Interrupt(ThreadHandle t);

        [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_GetOSHandle")]
        private static partial SafeWaitHandle GetOSHandle(ThreadHandle t);

        private SafeWaitHandle GetJoinHandle()
        {
            SafeWaitHandle handle = GetOSHandle(GetNativeHandle());
            GC.KeepAlive(this);
            return handle;
        }
#else
        private SafeWaitHandle GetJoinHandle()
        {
            ManualResetEvent newEvent = new ManualResetEvent(false);
            ManualResetEvent joinEvent = Interlocked.CompareExchange(ref _joinEvent, newEvent, null) ?? newEvent;
            return joinEvent.SafeWaitHandle;
        }
#endif
    }
}
