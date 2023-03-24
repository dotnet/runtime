// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

// PR-Comment: This implementation comes from Thread.CoreCLR.cs (src\coreclr\System.Private.CoreLib\src\System\Threading\Thread.CoreCLR.cs)

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

        public extern int ManagedThreadId
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        private extern bool IsAlivePortableCore
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /// <summary>
        /// Return whether or not this thread is a background thread.  Background
        /// threads do not affect when the Execution Engine shuts down.
        /// </summary>
        private bool IsBackgroundPortableCore
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

        /// <summary>Returns true if the thread is a threadpool thread.</summary>
        private extern bool IsThreadPoolThreadPortableCore
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
            [MethodImpl(MethodImplOptions.InternalCall)]
            set;
        }

        /// <summary>Returns the priority of the thread.</summary>
        private ThreadPriority PriorityPortableCore
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

        /// <summary>
        /// Return the thread state as a consistent set of bits.  This is more
        /// general then IsAlive or IsBackground.
        /// </summary>
        private ThreadState ThreadStatePortableCore => (ThreadState)GetThreadStateNative();


        /// <summary>
        /// Max value to be passed into <see cref="SpinWait(int)"/> for optimal delaying. This value is normalized to be
        /// appropriate for the processor.
        /// </summary>
        private static int OptimalMaxSpinWaitsPerSpinIterationPortableCore
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /// <summary>Returns handle for interop with EE. The handle is guaranteed to be non-null.</summary>
        private ThreadHandle GetNativeHandleCore()
        {
            IntPtr thread = _DONT_USE_InternalThread;

            // This should never happen under normal circumstances.
            if (thread == IntPtr.Zero)
            {
                throw new ArgumentException(null, SR.Argument_InvalidHandle);
            }

            return new ThreadHandle(thread);
        }

        // private unsafe void StartCore() - PR-Comment: Don't know if this rename it's appropriate
        private unsafe void StartCLRCore()
        {
            lock (this)
            {
                fixed (char* pThreadName = _name)
                {
                    StartInternal(GetNativeHandle(), _startHelper?._maxStackSize ?? 0, _priority_portableCore, pThreadName);
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
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void SleepInternal(int millisecondsTimeout);

        /// <summary>
        /// Wait for a length of time proportional to 'iterations'.  Each iteration is should
        /// only take a few machine instructions.  Calling this API is preferable to coding
        /// a explicit busy loop because the hardware can be informed that it is busy waiting.
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void SpinWaitInternalPortableCore(int iterations);

        private static void SpinWaitPortableCore(int iterations) => SpinWaitInternal(iterations);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_YieldThread")]
        private static partial Interop.BOOL YieldInternal();

        private static bool YieldPortableCore() => YieldInternal() != Interop.BOOL.FALSE;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Thread InitializeCurrentThreadPortableCore() => t_currentThread = GetCurrentThreadNative();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern Thread GetCurrentThreadNative();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void InitializePortableCore();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void InternalFinalize();

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_InformThreadNameChange", StringMarshalling = StringMarshalling.Utf16)]
        private static partial void InformThreadNameChange(ThreadHandle t, string? name, int len);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool IsBackgroundNative();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void SetBackgroundNative(bool isBackground);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern int GetPriorityNative();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void SetPriorityNative(int priority);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_GetCurrentOSThreadId")]
        private static partial ulong GetCurrentOSThreadId();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern int GetThreadStateNative();

        private ApartmentState GetApartmentStatePortableCore() =>
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
        private bool SetApartmentStateUncheckedPortableCore(ApartmentState state, bool throwOnError)
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
        private static bool SetApartmentStateUncheckedPortableCore(ApartmentState state, bool throwOnError)
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

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void InterruptPortableCore();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool JoinPortableCore(int millisecondsTimeout);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetThreadPoolThreadCore()
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
