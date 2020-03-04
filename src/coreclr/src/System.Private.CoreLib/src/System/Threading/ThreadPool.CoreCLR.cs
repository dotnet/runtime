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

    public static partial class ThreadPool
    {
        // Time in ms for which ThreadPoolWorkQueue.Dispatch keeps executing work items before returning to the OS
        private const uint DispatchQuantum = 30;

        internal static readonly bool UsePortableThreadPool = InitializeConfigAndDetermineUsePortableThreadPool();

        internal static readonly bool EnableWorkerTracking = GetEnableWorkerTracking();

        internal static bool KeepDispatching(int startTickCount)
        {
            if (UsePortableThreadPool)
            {
                return true;
            }

            // Note: this function may incorrectly return false due to TickCount overflow
            // if work item execution took around a multiple of 2^32 milliseconds (~49.7 days),
            // which is improbable.
            return (uint)(Environment.TickCount - startTickCount) < DispatchQuantum;
        }

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

        public static bool SetMaxThreads(int workerThreads, int completionPortThreads)
        {
            if (workerThreads < 0 || completionPortThreads < 0)
            {
                return false;
            }

            if (UsePortableThreadPool && !PortableThreadPool.ThreadPoolInstance.SetMaxThreads(workerThreads))
            {
                return false;
            }

            return SetMaxThreadsNative(workerThreads, completionPortThreads);
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
            if (workerThreads < 0 || completionPortThreads < 0)
            {
                return false;
            }

            if (UsePortableThreadPool && !PortableThreadPool.ThreadPoolInstance.SetMinThreads(workerThreads))
            {
                return false;
            }

            return SetMinThreadsNative(workerThreads, completionPortThreads);
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

        private static void RegisterWaitForSingleObjectCore(WaitHandle waitObject, RegisteredWaitHandle registeredWaitHandle)
        {
            registeredWaitHandle.OnBeforeRegister();

            if (UsePortableThreadPool)
            {
                PortableThreadPool.ThreadPoolInstance.RegisterWaitHandle(registeredWaitHandle);
                return;
            }

            IntPtr nativeRegisteredWaitHandle =
                RegisterWaitForSingleObjectNative(
                    waitObject,
                    registeredWaitHandle.Callback,
                    (uint)registeredWaitHandle.TimeoutDurationMs,
                    !registeredWaitHandle.Repeating,
                    registeredWaitHandle);
            registeredWaitHandle.SetNativeRegisteredWaitHandle(nativeRegisteredWaitHandle);
        }

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

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe bool PostQueuedCompletionStatus(NativeOverlapped* overlapped);

        [CLSCompliant(false)]
        public static unsafe bool UnsafeQueueNativeOverlapped(NativeOverlapped* overlapped) =>
            PostQueuedCompletionStatus(overlapped);

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

        internal static bool NotifyWorkItemComplete()
        {
            if (UsePortableThreadPool)
            {
                return PortableThreadPool.ThreadPoolInstance.NotifyWorkItemComplete();
            }

            return NotifyWorkItemCompleteNative();
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool NotifyWorkItemCompleteNative();

        internal static void ReportThreadStatus(bool isWorking)
        {
            if (UsePortableThreadPool)
            {
                // TODO: PortableThreadPool - Implement worker tracking
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
                PortableThreadPool.ThreadPoolInstance.NotifyWorkItemComplete();
                return;
            }

            NotifyWorkItemProgressNative();
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void NotifyWorkItemProgressNative();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool GetEnableWorkerTracking();

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
