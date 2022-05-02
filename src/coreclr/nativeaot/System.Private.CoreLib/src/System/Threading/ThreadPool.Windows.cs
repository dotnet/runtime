// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    //
    // Windows-specific implementation of ThreadPool
    //
    [UnsupportedOSPlatform("browser")]
    public sealed class RegisteredWaitHandle : MarshalByRefObject
    {
        private readonly Lock _lock;
        private SafeWaitHandle _waitHandle;
        private readonly _ThreadPoolWaitOrTimerCallback _callbackHelper;
        private readonly uint _millisecondsTimeout;
        private bool _repeating;
        private bool _unregistering;

        // Handle to this object to keep it alive
        private GCHandle _gcHandle;

        // Pointer to the TP_WAIT structure
        private IntPtr _tpWait;

        internal unsafe RegisteredWaitHandle(SafeWaitHandle waitHandle, _ThreadPoolWaitOrTimerCallback callbackHelper,
            uint millisecondsTimeout, bool repeating)
        {
            _lock = new Lock();

            // Protect the handle from closing while we are waiting on it (VSWhidbey 285642)
            waitHandle.DangerousAddRef();
            _waitHandle = waitHandle;

            _callbackHelper = callbackHelper;
            _millisecondsTimeout = millisecondsTimeout;
            _repeating = repeating;

            // Allocate _gcHandle and _tpWait as the last step and make sure they are never leaked
            _gcHandle = GCHandle.Alloc(this);

            _tpWait = Interop.Kernel32.CreateThreadpoolWait(&RegisteredWaitCallback, (IntPtr)_gcHandle, IntPtr.Zero);

            if (_tpWait == IntPtr.Zero)
            {
                _gcHandle.Free();
                throw new OutOfMemoryException();
            }
        }

        [UnmanagedCallersOnly]
        internal static void RegisteredWaitCallback(IntPtr instance, IntPtr context, IntPtr wait, uint waitResult)
        {
            var wrapper = ThreadPoolCallbackWrapper.Enter();
            GCHandle handle = (GCHandle)context;
            RegisteredWaitHandle registeredWaitHandle = (RegisteredWaitHandle)handle.Target!;
            Debug.Assert((handle == registeredWaitHandle._gcHandle) && (wait == registeredWaitHandle._tpWait));

            bool timedOut = (waitResult == (uint)Interop.Kernel32.WAIT_TIMEOUT);
            registeredWaitHandle.PerformCallback(timedOut);
            ThreadPool.IncrementCompletedWorkItemCount();
            wrapper.Exit();
        }

        private void PerformCallback(bool timedOut)
        {
            bool lockAcquired;
            var spinner = new SpinWait();

            // Prevent the race condition with Unregister and the previous PerformCallback call, which may still be
            // holding the _lock.
            while (!(lockAcquired = _lock.TryAcquire(0)) && !Volatile.Read(ref _unregistering))
            {
                spinner.SpinOnce();
            }

            // If another thread is running Unregister, no need to restart the timer or clean up
            if (lockAcquired)
            {
                try
                {
                    if (!_unregistering)
                    {
                        if (_repeating)
                        {
                            // Allow this wait to fire again. Restart the timer before executing the callback.
                            RestartWait();
                        }
                        else
                        {
                            // This wait will not be fired again. Free the GC handle to allow the GC to collect this object.
                            Debug.Assert(_gcHandle.IsAllocated);
                            _gcHandle.Free();
                        }
                    }
                }
                finally
                {
                    _lock.Release();
                }
            }

            _ThreadPoolWaitOrTimerCallback.PerformWaitOrTimerCallback(_callbackHelper, timedOut);
        }

        internal unsafe void RestartWait()
        {
            long timeout;
            long* pTimeout = null;  // Null indicates infinite timeout

            if (_millisecondsTimeout != Timeout.UnsignedInfinite)
            {
                timeout = -10000L * _millisecondsTimeout;
                pTimeout = &timeout;
            }

            // We can use DangerousGetHandle because of DangerousAddRef in the constructor
            Interop.Kernel32.SetThreadpoolWait(_tpWait, _waitHandle.DangerousGetHandle(), (IntPtr)pTimeout);
        }

        public bool Unregister(WaitHandle waitObject)
        {
            // Hold the lock during the synchronous part of Unregister (as in CoreCLR)
            using (LockHolder.Hold(_lock))
            {
                if (!_unregistering)
                {
                    // Ensure callbacks will not call SetThreadpoolWait anymore
                    _unregistering = true;

                    // Cease queueing more callbacks
                    Interop.Kernel32.SetThreadpoolWait(_tpWait, IntPtr.Zero, IntPtr.Zero);

                    // Should we wait for callbacks synchronously? Note that we treat the zero handle as the asynchronous case.
                    SafeWaitHandle? safeWaitHandle = waitObject?.SafeWaitHandle;
                    bool blocking = ((safeWaitHandle != null) && (safeWaitHandle.DangerousGetHandle() == new IntPtr(-1)));

                    if (blocking)
                    {
                        FinishUnregistering();
                    }
                    else
                    {
                        // Wait for callbacks and dispose resources asynchronously
                        ThreadPool.QueueUserWorkItem(FinishUnregisteringAsync, safeWaitHandle);
                    }

                    return true;
                }
            }
            return false;
        }

        private void FinishUnregistering()
        {
            Debug.Assert(_unregistering);

            // Wait for outstanding wait callbacks to complete
            Interop.Kernel32.WaitForThreadpoolWaitCallbacks(_tpWait, false);

            // Now it is safe to dispose resources
            Interop.Kernel32.CloseThreadpoolWait(_tpWait);
            _tpWait = IntPtr.Zero;

            if (_gcHandle.IsAllocated)
            {
                _gcHandle.Free();
            }

            Debug.Assert(_waitHandle != null);
            _waitHandle.DangerousRelease();
            _waitHandle = null;

            GC.SuppressFinalize(this);
        }

        private void FinishUnregisteringAsync(object? waitObject)
        {
            FinishUnregistering();

            // Signal the provided wait object
            SafeWaitHandle? safeWaitHandle = (SafeWaitHandle?)waitObject;

            if ((safeWaitHandle != null) && !safeWaitHandle.IsInvalid)
            {
                Interop.Kernel32.SetEvent(safeWaitHandle);
            }
        }

        ~RegisteredWaitHandle()
        {
            // If _gcHandle is allocated, it points to this object, so this object must not be collected by the GC
            Debug.Assert(!_gcHandle.IsAllocated);

            // If this object gets resurrected and another thread calls Unregister, that creates a race condition.
            // Do not block the finalizer thread. If another thread is running Unregister, it will clean up for us.
            // The _lock may be null in case of OOM in the constructor.
            if ((_lock != null) && _lock.TryAcquire(0))
            {
                try
                {
                    if (!_unregistering)
                    {
                        _unregistering = true;

                        if (_tpWait != IntPtr.Zero)
                        {
                            // There must be no in-flight callbacks; just dispose resources
                            Interop.Kernel32.CloseThreadpoolWait(_tpWait);
                            _tpWait = IntPtr.Zero;
                        }

                        if (_waitHandle != null)
                        {
                            _waitHandle.DangerousRelease();
                            _waitHandle = null;
                        }
                    }
                }
                finally
                {
                    _lock.Release();
                }
            }
        }
    }

    public static partial class ThreadPool
    {
        internal const bool IsWorkerTrackingEnabledInConfig = false;

        // Indicates whether the thread pool should yield the thread from the dispatch loop to the runtime periodically so that
        // the runtime may use the thread for processing other work.
        //
        // Windows thread pool threads need to yield back to the thread pool periodically, otherwise those threads may be
        // considered to be doing long-running work and change thread pool heuristics, such as slowing or halting thread
        // injection.
        internal static bool YieldFromDispatchLoop => true;

        /// <summary>
        /// The maximum number of threads in the default thread pool on Windows 10 as computed by
        /// TppComputeDefaultMaxThreads(TppMaxGlobalPool).
        /// </summary>
        /// <remarks>
        /// Note that Windows 8 and 8.1 used a different value: Math.Max(4 * Environment.ProcessorCount, 512).
        /// </remarks>
        private static readonly int MaxThreadCount = Math.Max(8 * Environment.ProcessorCount, 768);

        private static IntPtr s_work;

        private class ThreadCountHolder
        {
            internal ThreadCountHolder() => Interlocked.Increment(ref s_threadCount);
            ~ThreadCountHolder() => Interlocked.Decrement(ref s_threadCount);
        }

        [ThreadStatic]
        private static ThreadCountHolder t_threadCountHolder;
        private static int s_threadCount;

        [StructLayout(LayoutKind.Sequential)]
        private struct WorkingThreadCounter
        {
            private readonly Internal.PaddingFor32 pad1;

            public volatile int Count;

            private readonly Internal.PaddingFor32 pad2;
        }

        // The number of threads executing work items in the Dispatch method
        private static WorkingThreadCounter s_workingThreadCounter;

        private static readonly ThreadInt64PersistentCounter s_completedWorkItemCounter = new ThreadInt64PersistentCounter();

        [ThreadStatic]
        private static object? t_completionCountObject;

        internal static void InitializeForThreadPoolThread() => t_threadCountHolder = new ThreadCountHolder();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void IncrementCompletedWorkItemCount() => ThreadInt64PersistentCounter.Increment(GetOrCreateThreadLocalCompletionCountObject());

        internal static object GetOrCreateThreadLocalCompletionCountObject() =>
            t_completionCountObject ?? CreateThreadLocalCompletionCountObject();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static object CreateThreadLocalCompletionCountObject()
        {
            Debug.Assert(t_completionCountObject == null);

            object threadLocalCompletionCountObject = s_completedWorkItemCounter.CreateThreadLocalCountObject();
            t_completionCountObject = threadLocalCompletionCountObject;
            return threadLocalCompletionCountObject;
        }

        public static bool SetMaxThreads(int workerThreads, int completionPortThreads)
        {
            // Not supported at present
            return false;
        }

        public static void GetMaxThreads(out int workerThreads, out int completionPortThreads)
        {
            // Note that worker threads and completion port threads share the same thread pool.
            // The total number of threads cannot exceed MaxThreadCount.
            workerThreads = MaxThreadCount;
            completionPortThreads = MaxThreadCount;
        }

        public static bool SetMinThreads(int workerThreads, int completionPortThreads)
        {
            // Not supported at present
            return false;
        }

        public static void GetMinThreads(out int workerThreads, out int completionPortThreads)
        {
            workerThreads = 0;
            completionPortThreads = 0;
        }

        public static void GetAvailableThreads(out int workerThreads, out int completionPortThreads)
        {
            // Make sure we return a non-negative value if thread pool defaults are changed
            int availableThreads = Math.Max(MaxThreadCount - s_workingThreadCounter.Count, 0);

            workerThreads = availableThreads;
            completionPortThreads = availableThreads;
        }

        /// <summary>
        /// Gets the number of thread pool threads that currently exist.
        /// </summary>
        /// <remarks>
        /// For a thread pool implementation that may have different types of threads, the count includes all types.
        /// </remarks>
        public static int ThreadCount => s_threadCount;

        /// <summary>
        /// Gets the number of work items that have been processed so far.
        /// </summary>
        /// <remarks>
        /// For a thread pool implementation that may have different types of work items, the count includes all types.
        /// </remarks>
        public static long CompletedWorkItemCount => s_completedWorkItemCounter.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void NotifyWorkItemProgress() => IncrementCompletedWorkItemCount();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool NotifyWorkItemComplete(object? threadLocalCompletionCountObject, int currentTimeMs)
        {
            ThreadInt64PersistentCounter.Increment(threadLocalCompletionCountObject);
            return true;
        }

        internal static bool NotifyThreadBlocked() { return false; }
        internal static void NotifyThreadUnblocked() { }

        [UnmanagedCallersOnly]
        private static void DispatchCallback(IntPtr instance, IntPtr context, IntPtr work)
        {
            var wrapper = ThreadPoolCallbackWrapper.Enter();
            Debug.Assert(s_work == work);
            Interlocked.Increment(ref s_workingThreadCounter.Count);
            ThreadPoolWorkQueue.Dispatch();
            Interlocked.Decrement(ref s_workingThreadCounter.Count);
            // We reset the thread after executing each callback
            wrapper.Exit(resetThread: false);
        }

        internal static unsafe void RequestWorkerThread()
        {
            if (s_work == IntPtr.Zero)
            {
                IntPtr work = Interop.Kernel32.CreateThreadpoolWork(&DispatchCallback, IntPtr.Zero, IntPtr.Zero);
                if (work == IntPtr.Zero)
                    throw new OutOfMemoryException();

                if (Interlocked.CompareExchange(ref s_work, work, IntPtr.Zero) != IntPtr.Zero)
                    Interop.Kernel32.CloseThreadpoolWork(work);
            }

            Interop.Kernel32.SubmitThreadpoolWork(s_work);
        }

        private static RegisteredWaitHandle RegisterWaitForSingleObject(
             WaitHandle waitObject,
             WaitOrTimerCallback callBack,
             object state,
             uint millisecondsTimeOutInterval,
             bool executeOnlyOnce,
             bool flowExecutionContext)
        {
            if (waitObject == null)
                throw new ArgumentNullException(nameof(waitObject));

            if (callBack == null)
                throw new ArgumentNullException(nameof(callBack));

            var callbackHelper = new _ThreadPoolWaitOrTimerCallback(callBack, state, flowExecutionContext);
            var registeredWaitHandle = new RegisteredWaitHandle(waitObject.SafeWaitHandle, callbackHelper, millisecondsTimeOutInterval, !executeOnlyOnce);

            registeredWaitHandle.RestartWait();
            return registeredWaitHandle;
        }

        private static unsafe void NativeOverlappedCallback(nint overlappedPtr) =>
            _IOCompletionCallback.PerformSingleIOCompletionCallback(0, 0, (NativeOverlapped*)overlappedPtr);

        [CLSCompliant(false)]
        [SupportedOSPlatform("windows")]
        public static unsafe bool UnsafeQueueNativeOverlapped(NativeOverlapped* overlapped)
        {
            if (overlapped == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.overlapped);
            }

            // OS doesn't signal handle, so do it here (CoreCLR does this assignment in ThreadPoolNative::CorPostQueuedCompletionStatus)
            overlapped->InternalLow = (IntPtr)0;
            // Both types of callbacks are executed on the same thread pool
            return UnsafeQueueUserWorkItem(NativeOverlappedCallback, (nint)overlapped, preferLocal: false);
        }

        [Obsolete("ThreadPool.BindHandle(IntPtr) has been deprecated. Use ThreadPool.BindHandle(SafeHandle) instead.")]
        [SupportedOSPlatform("windows")]
        public static bool BindHandle(IntPtr osHandle)
        {
            throw new PlatformNotSupportedException(SR.Arg_PlatformNotSupported); // Replaced by ThreadPoolBoundHandle.BindHandle
        }

        [SupportedOSPlatform("windows")]
        public static bool BindHandle(SafeHandle osHandle)
        {
            throw new PlatformNotSupportedException(SR.Arg_PlatformNotSupported); // Replaced by ThreadPoolBoundHandle.BindHandle
        }
    }
}
