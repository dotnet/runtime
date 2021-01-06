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

        public extern int ManagedThreadId
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
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

        [DllImport(RuntimeHelpers.QCall)]
        private static extern unsafe void StartInternal(ThreadHandle t, int stackSize, int priority, char* pThreadName);

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
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void SleepInternal(int millisecondsTimeout);

        public static void Sleep(int millisecondsTimeout) => SleepInternal(millisecondsTimeout);

        [DllImport(RuntimeHelpers.QCall)]
        internal static extern void UninterruptibleSleep0();

        /// <summary>
        /// Wait for a length of time proportional to 'iterations'.  Each iteration is should
        /// only take a few machine instructions.  Calling this API is preferable to coding
        /// a explicit busy loop because the hardware can be informed that it is busy waiting.
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void SpinWaitInternal(int iterations);

        public static void SpinWait(int iterations) => SpinWaitInternal(iterations);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern Interop.BOOL YieldInternal();

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
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void InformThreadNameChange(ThreadHandle t, string? name, int len);

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
            get => IsBackgroundNative();
            set
            {
                SetBackgroundNative(value);
                if (!value)
                {
                    _mayNeedResetForThreadPool = true;
                }
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool IsBackgroundNative();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void SetBackgroundNative(bool isBackground);

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

        /// <summary>Returns the operating system identifier for the current thread.</summary>
        internal static ulong CurrentOSThreadId => GetCurrentOSThreadId();

        [DllImport(RuntimeHelpers.QCall)]
        private static extern ulong GetCurrentOSThreadId();

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
            if ((state == System.Threading.ApartmentState.Unknown) && (retState == System.Threading.ApartmentState.MTA))
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
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern void DisableComObjectEagerCleanup();
#else // !FEATURE_COMINTEROP
        public void DisableComObjectEagerCleanup()
        {
        }
#endif // FEATURE_COMINTEROP

        /// <summary>
        /// Interrupts a thread that is inside a Wait(), Sleep() or Join().  If that
        /// thread is not currently blocked in that manner, it will be interrupted
        /// when it next begins to block.
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern void Interrupt();

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

        private static int s_optimalMaxSpinWaitsPerSpinIteration;

        [DllImport(RuntimeHelpers.QCall)]
        private static extern int GetOptimalMaxSpinWaitsPerSpinIterationInternal();

        /// <summary>
        /// Max value to be passed into <see cref="SpinWait(int)"/> for optimal delaying. This value is normalized to be
        /// appropriate for the processor.
        /// </summary>
        internal static int OptimalMaxSpinWaitsPerSpinIteration
        {
            get
            {
                int optimalMaxSpinWaitsPerSpinIteration = s_optimalMaxSpinWaitsPerSpinIteration;
                return optimalMaxSpinWaitsPerSpinIteration != 0 ? optimalMaxSpinWaitsPerSpinIteration : CalculateOptimalMaxSpinWaitsPerSpinIteration();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int CalculateOptimalMaxSpinWaitsPerSpinIteration()
        {
            // This is done lazily because the first call to the function below in the process triggers a measurement that
            // takes a nontrivial amount of time if the measurement has not already been done in the backgorund.
            // See Thread::InitializeYieldProcessorNormalized(), which describes and calculates this value.
            s_optimalMaxSpinWaitsPerSpinIteration = GetOptimalMaxSpinWaitsPerSpinIterationInternal();
            Debug.Assert(s_optimalMaxSpinWaitsPerSpinIteration > 0);
            return s_optimalMaxSpinWaitsPerSpinIteration;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int GetCurrentProcessorNumber();

        // Cached processor id could be used as a hint for which per-core stripe of data to access to avoid sharing.
        // It is periodically refreshed to trail the actual thread core affinity.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCurrentProcessorId()
        {
            if (s_isProcessorNumberReallyFast)
                return GetCurrentProcessorNumber();

            return ProcessorIdCache.GetCurrentProcessorId();
        }

        // a speed check will determine refresh rate of the cache and will report if caching is not advisable.
        // we will record that in a readonly static so that it could become a JIT constant and bypass caching entirely.
        private static readonly bool s_isProcessorNumberReallyFast = ProcessorIdCache.ProcessorNumberSpeedCheck();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ResetThreadPoolThread()
        {
            Debug.Assert(this == CurrentThread);
            Debug.Assert(IsThreadPoolThread);

            if (!ThreadPool.UsePortableThreadPool)
            {
                // Currently implemented in unmanaged method Thread::InternalReset and
                // called internally from the ThreadPool in NotifyWorkItemComplete.
                return;
            }

            if (_mayNeedResetForThreadPool)
            {
                ResetThreadPoolThreadSlow();
            }
        }
    }
}
