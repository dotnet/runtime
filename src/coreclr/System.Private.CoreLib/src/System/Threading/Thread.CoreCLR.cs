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
                throw new ArgumentException(null, SR.Argument_InvalidHandle);
            }

            return new ThreadHandle(thread);
        }

        private unsafe void StartCore()
        {
            lock (this)
            {
                fixed (char* pThreadName = _name)
                {
                    StartInternal(GetNativeHandle(), _startHelper?._maxStackSize ?? 0, _priority, pThreadName);
                }
            }
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_Start")]
        private static unsafe partial void StartInternal(ThreadHandle t, int stackSize, int priority, char* pThreadName);

        // Called from the runtime
        private void StartCallback()
        {
            StartHelper? startHelper = _startHelper;
            Debug.Assert(startHelper != null);
            _startHelper = null;

            startHelper.Run();
        }

        // Invoked by VM. Helper method to get a logical thread ID for StringBuilder (for
        // correctness) and for FileStream's async code path (for perf, to avoid creating
        // a Thread instance).
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr InternalGetCurrentThread();

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
        private static Thread InitializeCurrentThread() => t_currentThread = GetCurrentThreadNative();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern Thread GetCurrentThreadNative();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void Initialize();

        /// <summary>Clean up the thread when it goes away.</summary>
        ~Thread() => InternalFinalize(); // Delegate to the unmanaged portion.

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void InternalFinalize();

        partial void ThreadNameChanged(string? value)
        {
            InformThreadNameChange(GetNativeHandle(), value, value?.Length ?? 0);
            GC.KeepAlive(this);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_InformThreadNameChange", StringMarshalling = StringMarshalling.Utf16)]
        private static partial void InformThreadNameChange(ThreadHandle t, string? name, int len);

        /// <summary>Returns true if the thread has been started and is not dead.</summary>
        public extern bool IsAlive
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /// <summary>
        /// Return whether or not this thread is a background thread.  Background
        /// threads do not affect when the Execution Engine shuts down.
        /// </summary>
        public bool IsBackground
        {
            get => GetIsBackground();
            set
            {
                SetIsBackground(GetNativeHandle(), value ? Interop.BOOL.TRUE : Interop.BOOL.FALSE);
                GC.KeepAlive(this);
                if (!value)
                {
                    _mayNeedResetForThreadPool = true;
                }
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool GetIsBackground();

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_SetIsBackground")]
        private static partial void SetIsBackground(ThreadHandle t, Interop.BOOL value);

        /// <summary>Returns true if the thread is a threadpool thread.</summary>
        public extern bool IsThreadPoolThread
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
            [MethodImpl(MethodImplOptions.InternalCall)]
            internal set;
        }

        /// <summary>Returns the priority of the thread.</summary>
        public ThreadPriority Priority
        {
            get => (ThreadPriority)GetPriorityNative();
            set
            {
                SetPriorityNative((int)value);
                if (value != ThreadPriority.Normal)
                {
                    _mayNeedResetForThreadPool = true;
                }
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern int GetPriorityNative();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void SetPriorityNative(int priority);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_GetCurrentOSThreadId")]
        private static partial ulong GetCurrentOSThreadId();

        /// <summary>
        /// Return the thread state as a consistent set of bits.  This is more
        /// general then IsAlive or IsBackground.
        /// </summary>
        public ThreadState ThreadState => (ThreadState)GetThreadStateNative();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern int GetThreadStateNative();

        public ApartmentState GetApartmentState() =>
#if FEATURE_COMINTEROP_APARTMENT_SUPPORT
            (ApartmentState)GetApartmentStateNative();
#else // !FEATURE_COMINTEROP_APARTMENT_SUPPORT
            ApartmentState.Unknown;
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

        /// <summary>
        /// An unstarted thread can be marked to indicate that it will host a
        /// single-threaded or multi-threaded apartment.
        /// </summary>
#if FEATURE_COMINTEROP_APARTMENT_SUPPORT
        private bool SetApartmentStateUnchecked(ApartmentState state, bool throwOnError)
        {
            ApartmentState retState = (ApartmentState)SetApartmentStateNative((int)state);

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

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern int GetApartmentStateNative();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern int SetApartmentStateNative(int state);
#else // FEATURE_COMINTEROP_APARTMENT_SUPPORT
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

        /// <summary>
        /// Waits for the thread to die or for timeout milliseconds to elapse.
        /// </summary>
        /// <returns>
        /// Returns true if the thread died, or false if the wait timed out. If
        /// -1 is given as the parameter, no timeout will occur.
        /// </returns>
        /// <exception cref="ArgumentException">if timeout &lt; -1 (Timeout.Infinite)</exception>
        /// <exception cref="ThreadInterruptedException">if the thread is interrupted while waiting</exception>
        /// <exception cref="ThreadStateException">if the thread has not been started yet</exception>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern bool Join(int millisecondsTimeout);

        /// <summary>
        /// Max value to be passed into <see cref="SpinWait(int)"/> for optimal delaying. This value is normalized to be
        /// appropriate for the processor.
        /// </summary>
        internal static int OptimalMaxSpinWaitsPerSpinIteration
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ResetThreadPoolThread()
        {
            Debug.Assert(this == CurrentThread);
            Debug.Assert(IsThreadPoolThread);

            if (_mayNeedResetForThreadPool)
            {
                ResetThreadPoolThreadSlow();
            }
        }
    }
}
