// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
**
**
** Purpose: Class for creating and managing a threadpool
**
**
=============================================================================*/

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    //
    // This type is necessary because VS 2010's debugger looks for a method named _ThreadPoolWaitCallbacck.PerformWaitCallback
    // on the stack to determine if a thread is a ThreadPool thread or not.  We have a better way to do this for .NET 4.5, but
    // still need to maintain compatibility with VS 2010.  When compat with VS 2010 is no longer an issue, this type may be
    // removed.
    //
    internal static class _ThreadPoolWaitCallback
    {
        internal static bool PerformWaitCallback() => ThreadPoolWorkQueue.Dispatch();
    }

    public sealed partial class RegisteredWaitHandle : MarshalByRefObject
    {
        private IntPtr _nativeRegisteredWaitHandle = InvalidHandleValue;
        private bool _releaseHandle;

        private static bool IsValidHandle(IntPtr handle) => handle != InvalidHandleValue && handle != IntPtr.Zero;

        internal void SetNativeRegisteredWaitHandle(IntPtr nativeRegisteredWaitHandle)
        {
            Debug.Assert(!ThreadPool.UsePortableThreadPool);
            Debug.Assert(IsValidHandle(nativeRegisteredWaitHandle));
            Debug.Assert(!IsValidHandle(_nativeRegisteredWaitHandle));

            _nativeRegisteredWaitHandle = nativeRegisteredWaitHandle;
        }

        internal void OnBeforeRegister()
        {
            if (ThreadPool.UsePortableThreadPool)
            {
                GC.SuppressFinalize(this);
                return;
            }

            Handle.DangerousAddRef(ref _releaseHandle);
        }

        /// <summary>
        /// Unregisters this wait handle registration from the wait threads.
        /// </summary>
        /// <param name="waitObject">The event to signal when the handle is unregistered.</param>
        /// <returns>If the handle was successfully marked to be removed and the provided wait handle was set as the user provided event.</returns>
        /// <remarks>
        /// This method will only return true on the first call.
        /// Passing in a wait handle with a value of -1 will result in a blocking wait, where Unregister will not return until the full unregistration is completed.
        /// </remarks>
        public bool Unregister(WaitHandle waitObject)
        {
            if (ThreadPool.UsePortableThreadPool)
            {
                return UnregisterPortable(waitObject);
            }

            s_callbackLock.Acquire();
            try
            {
                if (!IsValidHandle(_nativeRegisteredWaitHandle) ||
                    !UnregisterWaitNative(_nativeRegisteredWaitHandle, waitObject?.SafeWaitHandle))
                {
                    return false;
                }
                _nativeRegisteredWaitHandle = InvalidHandleValue;

                if (_releaseHandle)
                {
                    Handle.DangerousRelease();
                    _releaseHandle = false;
                }
            }
            finally
            {
                s_callbackLock.Release();
            }

            GC.SuppressFinalize(this);
            return true;
        }

        ~RegisteredWaitHandle()
        {
            if (ThreadPool.UsePortableThreadPool)
            {
                return;
            }

            s_callbackLock.Acquire();
            try
            {
                if (!IsValidHandle(_nativeRegisteredWaitHandle))
                {
                    return;
                }

                WaitHandleCleanupNative(_nativeRegisteredWaitHandle);
                _nativeRegisteredWaitHandle = InvalidHandleValue;

                if (_releaseHandle)
                {
                    Handle.DangerousRelease();
                    _releaseHandle = false;
                }
            }
            finally
            {
                s_callbackLock.Release();
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void WaitHandleCleanupNative(IntPtr handle);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool UnregisterWaitNative(IntPtr handle, SafeHandle? waitObject);
    }

    internal sealed partial class CompleteWaitThreadPoolWorkItem : IThreadPoolWorkItem
    {
        void IThreadPoolWorkItem.Execute() => CompleteWait();

        // Entry point from unmanaged code
        private void CompleteWait()
        {
            Debug.Assert(ThreadPool.UsePortableThreadPool);
            PortableThreadPool.CompleteWait(_registeredWaitHandle, _timedOut);
        }
    }

    internal sealed class UnmanagedThreadPoolWorkItem : IThreadPoolWorkItem
    {
        private readonly IntPtr _callback;
        private readonly IntPtr _state;

        public UnmanagedThreadPoolWorkItem(IntPtr callback, IntPtr state)
        {
            _callback = callback;
            _state = state;
        }

        void IThreadPoolWorkItem.Execute() => ExecuteUnmanagedThreadPoolWorkItem(_callback, _state);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void ExecuteUnmanagedThreadPoolWorkItem(IntPtr callback, IntPtr state);
    }

    public static partial class ThreadPool
    {
        // SOS's ThreadPool command depends on this name
        internal static readonly bool UsePortableThreadPool = InitializeConfigAndDetermineUsePortableThreadPool();

        // Time-senstiive work items are those that may need to run ahead of normal work items at least periodically. For a
        // runtime that does not support time-sensitive work items on the managed side, the thread pool yields the thread to the
        // runtime periodically (by exiting the dispatch loop) so that the runtime may use that thread for processing
        // any time-sensitive work. For a runtime that supports time-sensitive work items on the managed side, the thread pool
        // does not yield the thread and instead processes time-sensitive work items queued by specific APIs periodically.
        internal static bool SupportsTimeSensitiveWorkItems => UsePortableThreadPool;

        // This needs to be initialized after UsePortableThreadPool above, as it may depend on UsePortableThreadPool and the
        // config initialization
        internal static readonly bool EnableWorkerTracking = GetEnableWorkerTracking();

        private static unsafe bool InitializeConfigAndDetermineUsePortableThreadPool()
        {
            bool usePortableThreadPool = false;
            int configVariableIndex = 0;
            while (true)
            {
                int nextConfigVariableIndex =
                    GetNextConfigUInt32Value(
                        configVariableIndex,
                        out uint configValue,
                        out bool isBoolean,
                        out char* appContextConfigNameUnsafe);
                if (nextConfigVariableIndex < 0)
                {
                    break;
                }

                Debug.Assert(nextConfigVariableIndex > configVariableIndex);
                configVariableIndex = nextConfigVariableIndex;

                if (appContextConfigNameUnsafe == null)
                {
                    // Special case for UsePortableThreadPool, which doesn't go into the AppContext
                    Debug.Assert(configValue != 0);
                    Debug.Assert(isBoolean);
                    usePortableThreadPool = true;
                    continue;
                }

                var appContextConfigName = new string(appContextConfigNameUnsafe);
                if (isBoolean)
                {
                    AppContext.SetSwitch(appContextConfigName, configValue != 0);
                }
                else
                {
                    AppContext.SetData(appContextConfigName, configValue);
                }
            }

            return usePortableThreadPool;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe int GetNextConfigUInt32Value(
            int configVariableIndex,
            out uint configValue,
            out bool isBoolean,
            out char* appContextConfigName);

        private static bool GetEnableWorkerTracking() =>
            UsePortableThreadPool
                ? AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.EnableWorkerTracking", false)
                : GetEnableWorkerTrackingNative();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool CanSetMinIOCompletionThreads(int ioCompletionThreads);

        internal static void SetMinIOCompletionThreads(int ioCompletionThreads)
        {
            Debug.Assert(UsePortableThreadPool);
            Debug.Assert(ioCompletionThreads >= 0);

            bool success = SetMinThreadsNative(1, ioCompletionThreads); // worker thread count is ignored
            Debug.Assert(success);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool CanSetMaxIOCompletionThreads(int ioCompletionThreads);

        internal static void SetMaxIOCompletionThreads(int ioCompletionThreads)
        {
            Debug.Assert(UsePortableThreadPool);
            Debug.Assert(ioCompletionThreads > 0);

            bool success = SetMaxThreadsNative(1, ioCompletionThreads); // worker thread count is ignored
            Debug.Assert(success);
        }

        public static bool SetMaxThreads(int workerThreads, int completionPortThreads)
        {
            if (UsePortableThreadPool)
            {
                return PortableThreadPool.ThreadPoolInstance.SetMaxThreads(workerThreads, completionPortThreads);
            }

            return
                workerThreads >= 0 &&
                completionPortThreads >= 0 &&
                SetMaxThreadsNative(workerThreads, completionPortThreads);
        }

        public static void GetMaxThreads(out int workerThreads, out int completionPortThreads)
        {
            GetMaxThreadsNative(out workerThreads, out completionPortThreads);

            if (UsePortableThreadPool)
            {
                workerThreads = PortableThreadPool.ThreadPoolInstance.GetMaxThreads();
            }
        }

        public static bool SetMinThreads(int workerThreads, int completionPortThreads)
        {
            if (UsePortableThreadPool)
            {
                return PortableThreadPool.ThreadPoolInstance.SetMinThreads(workerThreads, completionPortThreads);
            }

            return
                workerThreads >= 0 &&
                completionPortThreads >= 0 &&
                SetMinThreadsNative(workerThreads, completionPortThreads);
        }

        public static void GetMinThreads(out int workerThreads, out int completionPortThreads)
        {
            GetMinThreadsNative(out workerThreads, out completionPortThreads);

            if (UsePortableThreadPool)
            {
                workerThreads = PortableThreadPool.ThreadPoolInstance.GetMinThreads();
            }
        }

        public static void GetAvailableThreads(out int workerThreads, out int completionPortThreads)
        {
            GetAvailableThreadsNative(out workerThreads, out completionPortThreads);

            if (UsePortableThreadPool)
            {
                workerThreads = PortableThreadPool.ThreadPoolInstance.GetAvailableThreads();
            }
        }

        /// <summary>
        /// Gets the number of thread pool threads that currently exist.
        /// </summary>
        /// <remarks>
        /// For a thread pool implementation that may have different types of threads, the count includes all types.
        /// </remarks>
        public static int ThreadCount =>
            (UsePortableThreadPool ? PortableThreadPool.ThreadPoolInstance.ThreadCount : 0) + GetThreadCount();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GetThreadCount();

        /// <summary>
        /// Gets the number of work items that have been processed so far.
        /// </summary>
        /// <remarks>
        /// For a thread pool implementation that may have different types of work items, the count includes all types.
        /// </remarks>
        public static long CompletedWorkItemCount
        {
            get
            {
                long count = GetCompletedWorkItemCount();
                if (UsePortableThreadPool)
                {
                    count += PortableThreadPool.ThreadPoolInstance.CompletedWorkItemCount;
                }
                return count;
            }
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern long GetCompletedWorkItemCount();

        private static long PendingUnmanagedWorkItemCount => UsePortableThreadPool ? 0 : GetPendingUnmanagedWorkItemCount();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern long GetPendingUnmanagedWorkItemCount();

        private static RegisteredWaitHandle RegisterWaitForSingleObject(
             WaitHandle? waitObject,
             WaitOrTimerCallback? callBack,
             object? state,
             uint millisecondsTimeOutInterval,
             bool executeOnlyOnce,
             bool flowExecutionContext)
        {

            if (waitObject == null)
                throw new ArgumentNullException(nameof(waitObject));

            if (callBack == null)
                throw new ArgumentNullException(nameof(callBack));

            RegisteredWaitHandle registeredWaitHandle = new RegisteredWaitHandle(
                waitObject,
                new _ThreadPoolWaitOrTimerCallback(callBack, state, flowExecutionContext),
                (int)millisecondsTimeOutInterval,
                !executeOnlyOnce);

            registeredWaitHandle.OnBeforeRegister();

            if (UsePortableThreadPool)
            {
                PortableThreadPool.ThreadPoolInstance.RegisterWaitHandle(registeredWaitHandle);
            }
            else
            {
                IntPtr nativeRegisteredWaitHandle =
                    RegisterWaitForSingleObjectNative(
                        waitObject,
                        registeredWaitHandle.Callback,
                        (uint)registeredWaitHandle.TimeoutDurationMs,
                        !registeredWaitHandle.Repeating,
                        registeredWaitHandle);
                registeredWaitHandle.SetNativeRegisteredWaitHandle(nativeRegisteredWaitHandle);
            }

            return registeredWaitHandle;
        }

        internal static void UnsafeQueueWaitCompletion(CompleteWaitThreadPoolWorkItem completeWaitWorkItem)
        {
            Debug.Assert(UsePortableThreadPool);

#if TARGET_WINDOWS // the IO completion thread pool is currently only available on Windows
            QueueWaitCompletionNative(completeWaitWorkItem);
#else
            UnsafeQueueUserWorkItemInternal(completeWaitWorkItem, preferLocal: false);
#endif
        }

#if TARGET_WINDOWS // the IO completion thread pool is currently only available on Windows
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void QueueWaitCompletionNative(CompleteWaitThreadPoolWorkItem completeWaitWorkItem);
#endif

        internal static void RequestWorkerThread()
        {
            if (UsePortableThreadPool)
            {
                PortableThreadPool.ThreadPoolInstance.RequestWorker();
                return;
            }

            RequestWorkerThreadNative();
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern Interop.BOOL RequestWorkerThreadNative();

        // Entry point from unmanaged code
        private static void EnsureGateThreadRunning()
        {
            Debug.Assert(UsePortableThreadPool);
            PortableThreadPool.EnsureGateThreadRunning();
        }

        /// <summary>
        /// Called from the gate thread periodically to perform runtime-specific gate activities
        /// </summary>
        /// <param name="cpuUtilization">CPU utilization as a percentage since the last call</param>
        /// <returns>True if the runtime still needs to perform gate activities, false otherwise</returns>
        internal static bool PerformRuntimeSpecificGateActivities(int cpuUtilization)
        {
            Debug.Assert(UsePortableThreadPool);
            return PerformRuntimeSpecificGateActivitiesNative(cpuUtilization) != Interop.BOOL.FALSE;
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern Interop.BOOL PerformRuntimeSpecificGateActivitiesNative(int cpuUtilization);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe bool PostQueuedCompletionStatus(NativeOverlapped* overlapped);

        [CLSCompliant(false)]
        public static unsafe bool UnsafeQueueNativeOverlapped(NativeOverlapped* overlapped) =>
            PostQueuedCompletionStatus(overlapped);

        // Entry point from unmanaged code
        private static void UnsafeQueueUnmanagedWorkItem(IntPtr callback, IntPtr state)
        {
            Debug.Assert(SupportsTimeSensitiveWorkItems);
            UnsafeQueueTimeSensitiveWorkItemInternal(new UnmanagedThreadPoolWorkItem(callback, state));
        }

        // Native methods:

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool SetMinThreadsNative(int workerThreads, int completionPortThreads);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool SetMaxThreadsNative(int workerThreads, int completionPortThreads);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void GetMinThreadsNative(out int workerThreads, out int completionPortThreads);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void GetMaxThreadsNative(out int workerThreads, out int completionPortThreads);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void GetAvailableThreadsNative(out int workerThreads, out int completionPortThreads);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool NotifyWorkItemComplete(object? threadLocalCompletionCountObject, int currentTimeMs)
        {
            if (UsePortableThreadPool)
            {
                return
                    PortableThreadPool.ThreadPoolInstance.NotifyWorkItemComplete(
                        threadLocalCompletionCountObject,
                        currentTimeMs);
            }

            return NotifyWorkItemCompleteNative();
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool NotifyWorkItemCompleteNative();

        internal static void ReportThreadStatus(bool isWorking)
        {
            if (UsePortableThreadPool)
            {
                PortableThreadPool.ThreadPoolInstance.ReportThreadStatus(isWorking);
                return;
            }

            ReportThreadStatusNative(isWorking);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void ReportThreadStatusNative(bool isWorking);

        internal static void NotifyWorkItemProgress()
        {
            if (UsePortableThreadPool)
            {
                PortableThreadPool.ThreadPoolInstance.NotifyWorkItemProgress();
                return;
            }

            NotifyWorkItemProgressNative();
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void NotifyWorkItemProgressNative();

        internal static bool NotifyThreadBlocked() =>
            UsePortableThreadPool && PortableThreadPool.ThreadPoolInstance.NotifyThreadBlocked();

        internal static void NotifyThreadUnblocked()
        {
            Debug.Assert(UsePortableThreadPool);
            PortableThreadPool.ThreadPoolInstance.NotifyThreadUnblocked();
        }

        internal static object? GetOrCreateThreadLocalCompletionCountObject() =>
            UsePortableThreadPool ? PortableThreadPool.ThreadPoolInstance.GetOrCreateThreadLocalCompletionCountObject() : null;

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool GetEnableWorkerTrackingNative();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr RegisterWaitForSingleObjectNative(
             WaitHandle waitHandle,
             object state,
             uint timeOutInterval,
             bool executeOnlyOnce,
             RegisteredWaitHandle registeredWaitHandle
             );

        [Obsolete("ThreadPool.BindHandle(IntPtr) has been deprecated.  Please use ThreadPool.BindHandle(SafeHandle) instead.", false)]
        public static bool BindHandle(IntPtr osHandle)
        {
            return BindIOCompletionCallbackNative(osHandle);
        }

        [SupportedOSPlatform("windows")]
        public static bool BindHandle(SafeHandle osHandle)
        {
            if (osHandle == null)
                throw new ArgumentNullException(nameof(osHandle));

            bool ret = false;
            bool mustReleaseSafeHandle = false;
            try
            {
                osHandle.DangerousAddRef(ref mustReleaseSafeHandle);
                ret = BindIOCompletionCallbackNative(osHandle.DangerousGetHandle());
            }
            finally
            {
                if (mustReleaseSafeHandle)
                    osHandle.DangerousRelease();
            }
            return ret;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool BindIOCompletionCallbackNative(IntPtr fileHandle);
    }
}
