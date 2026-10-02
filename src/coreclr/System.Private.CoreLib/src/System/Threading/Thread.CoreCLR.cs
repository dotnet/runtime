// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

#if TARGET_UNIX || TARGET_BROWSER || TARGET_WASI
        internal WaitSubsystem.ThreadWaitInfo? _waitInfo;
#if FEATURE_MULTITHREADING
        private volatile ManualResetEvent? _joinEvent;
#endif
#endif

        private IntPtr _DONT_USE_InternalThread;
        private int _priority;
        private int _managedThreadId; // Debugger depends on the exact name of this field.

        // This is used for a quick check on thread pool threads after running a work item to determine if the name, background
        // state, or priority were changed by the work item, and if so to reset it. Other threads may also change some of those,
        // but those types of changes may race with the reset anyway, so this field doesn't need to be synchronized.
        private bool _mayNeedResetForThreadPool;

        // This is set in two places:
        // For threads started with Thread.Start: Set in managed code as the thread is exiting.
        // For external threads that attach to the runtime: Set in unmanaged code as part of thread detach.
        // This is only read in managed code.
        private bool _isDead;
        private bool _isThreadPool;

        private Thread() { }

        internal static Exception GetQCallSpecialException(nint status)
        {
            Exception? exception = null;
            GetQCallSpecialException(status, ObjectHandleOnStack.Create(ref exception));
            Debug.Assert(exception is not null);
            return exception;
        }

        [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_GetQCallSpecialException")]
        private static partial void GetQCallSpecialException(nint status, ObjectHandleOnStack exception);

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

        partial void StartCore();

        /// <summary>Clean up the thread when it goes away.</summary>
        ~Thread() => InternalFinalize(); // Delegate to the unmanaged portion.

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void InternalFinalize();

        partial void ThreadNameChanged(string? value);

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

                return GetIsBackgroundCore();
            }
            set
            {
                if (_isDead)
                {
                    throw new ThreadStateException(SR.ThreadState_Dead_State);
                }

                SetIsBackgroundCore(value);
            }
        }

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
                if (value is not (
                    ThreadPriority.Lowest or
                    ThreadPriority.BelowNormal or
                    ThreadPriority.Normal or
                    ThreadPriority.AboveNormal or
                    ThreadPriority.Highest))
                {
                    throw new ArgumentOutOfRangeException(paramName: null, message: SR.Argument_InvalidFlag);
                }

                SetPriorityCore(value);
            }
        }

        /// <summary>
        /// Return the thread state as a consistent set of bits.  This is more
        /// general then IsAlive or IsBackground.
        /// </summary>
        public ThreadState ThreadState
        {
            get
            {
                if (_isDead)
                {
                    return ThreadState.Stopped;
                }

                return GetThreadStateCore();
            }
        }

        [SuppressGCTransition]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_GetThreadState")]
        private static partial int GetThreadState(ThreadHandle t);

        internal unsafe void SetWaitSleepJoinState()
        {
            // This method is called when the thread is about to enter a wait, sleep, or join state.
            // It sets the state in the native layer to indicate that the thread is waiting.
            NativeThread* nativeThread = GetNativeThreadForCurrentThread();
            Interlocked.Or(ref nativeThread->m_State, NativeThread.ThreadState.TS_WaitSleepJoin);
        }

        internal unsafe void ClearWaitSleepJoinState()
        {
            // This method is called when the thread is no longer in a wait, sleep, or join state.
            // It clears the state in the native layer to indicate that the thread is no longer waiting.
            NativeThread* nativeThread = GetNativeThreadForCurrentThread();
            Interlocked.And(ref nativeThread->m_State, ~NativeThread.ThreadState.TS_WaitSleepJoin);
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

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool CatchAtSafePoint();

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_PollGC")]
        private static partial void PollGCInternal();

        // GC Suspension is done by simply dropping into native code via p/invoke, and we reuse the p/invoke
        // mechanism for suspension. On all architectures we should have the actual stub used for the check be implemented
        // as a small assembly stub which checks the global g_TrapReturningThreads flag and tail-call to this helper
        private static void PollGC()
        {
            if (CatchAtSafePoint())
            {
                PollGCWorker();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void PollGCWorker() => PollGCInternal();
        }

#if TARGET_UNIX || TARGET_BROWSER || TARGET_WASI
        internal WaitSubsystem.ThreadWaitInfo WaitInfo
        {
            get
            {
                return Volatile.Read(ref _waitInfo) ?? AllocateWaitInfo();

                WaitSubsystem.ThreadWaitInfo AllocateWaitInfo()
                {
                    Interlocked.CompareExchange(ref _waitInfo, new WaitSubsystem.ThreadWaitInfo(this), null!);
                    return _waitInfo;
                }
            }
        }
#endif

        private void OnThreadExited()
        {
            // Consider this managed thread as dead.
            // The unmanaged thread is still alive, but will die soon, after cleaning up some state.
            // We set _isDead = true before calling _waitInfo?.OnThreadExiting() and SetJoinHandle()
            // so that any threads waiting on this thread to end will correctly see that it is stopped
            // when we set the join handle.
            _isDead = true;
#if TARGET_UNIX || TARGET_BROWSER || TARGET_WASI
            // Inform the wait subsystem that the thread is exiting. For instance, this would abandon any mutexes locked by
            // the thread.
            _waitInfo?.OnThreadExiting();
            SetJoinHandle();
#endif
        }

        [UnmanagedCallersOnly]
        private static unsafe void OnThreadExited(Thread* pThread, Exception* pException)
        {
            try
            {
                pThread->OnThreadExited();
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_ReentrantWaitAny")]
        internal static unsafe partial int ReentrantWaitAny([MarshalAs(UnmanagedType.Bool)] bool alertable, int timeout, int count, IntPtr* handles);

        [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ThreadNative_CheckForPendingInterrupt")]
        internal static partial void CheckForPendingInterrupt();

        private unsafe NativeThread* GetNativeThreadForCurrentThread()
        {
            Debug.Assert(this == CurrentThread);
            return (NativeThread*)_DONT_USE_InternalThread;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeThread
        {
            public ThreadState m_State;

            internal enum ThreadState
            {
                TS_WaitSleepJoin = 0x02000000, // Thread is waiting, sleeping or joining
            }
        }
    }
}
