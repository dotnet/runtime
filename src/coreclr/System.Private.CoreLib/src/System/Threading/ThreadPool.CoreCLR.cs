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
            GC.SuppressFinalize(this);
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
            return UnregisterPortable(waitObject);
        }

        ~RegisteredWaitHandle()
        {
            return;
        }
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

        unsafe void IThreadPoolWorkItem.Execute() => ((delegate* unmanaged<IntPtr, int>)_callback)(_state);
    }

    public static partial class ThreadPool
    {
        private static readonly byte UsePortableThreadPoolConfigValues = InitializeConfigAndDetermineUsePortableThreadPool();

        // Indicates whether the thread pool should yield the thread from the dispatch loop to the runtime periodically so that
        // the runtime may use the thread for processing other work
        internal static bool YieldFromDispatchLoop => false;

        private static readonly bool IsWorkerTrackingEnabledInConfig = GetEnableWorkerTracking();

        private static unsafe byte InitializeConfigAndDetermineUsePortableThreadPool()
        {
            byte usePortableThreadPoolConfigValues = 0;
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
                    // Special case for UsePortableThreadPool and similar, which don't go into the AppContext
                    Debug.Assert(configValue != 0);
                    Debug.Assert(!isBoolean);
                    usePortableThreadPoolConfigValues = (byte)configValue;
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

            return usePortableThreadPoolConfigValues;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe int GetNextConfigUInt32Value(
            int configVariableIndex,
            out uint configValue,
            out bool isBoolean,
            out char* appContextConfigName);

        private static bool GetEnableWorkerTracking() =>
            AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.EnableWorkerTracking", false);

        public static bool SetMaxThreads(int workerThreads, int completionPortThreads)
        {
            return PortableThreadPool.ThreadPoolInstance.SetMaxThreads(workerThreads, completionPortThreads);
        }

        public static void GetMaxThreads(out int workerThreads, out int completionPortThreads)
        {
            PortableThreadPool.ThreadPoolInstance.GetMaxThreads(out workerThreads, out completionPortThreads);
        }

        public static bool SetMinThreads(int workerThreads, int completionPortThreads)
        {
            return PortableThreadPool.ThreadPoolInstance.SetMinThreads(workerThreads, completionPortThreads);
        }

        public static void GetMinThreads(out int workerThreads, out int completionPortThreads)
        {
            PortableThreadPool.ThreadPoolInstance.GetMinThreads(out workerThreads, out completionPortThreads);
        }

        public static void GetAvailableThreads(out int workerThreads, out int completionPortThreads)
        {
            PortableThreadPool.ThreadPoolInstance.GetAvailableThreads(out workerThreads, out completionPortThreads);
        }

        /// <summary>
        /// Gets the number of thread pool threads that currently exist.
        /// </summary>
        /// <remarks>
        /// For a thread pool implementation that may have different types of threads, the count includes all types.
        /// </remarks>
        public static int ThreadCount
        {
            get
            {
                return PortableThreadPool.ThreadPoolInstance.ThreadCount;
            }
        }

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
                return PortableThreadPool.ThreadPoolInstance.CompletedWorkItemCount;
            }
        }

        private static long PendingUnmanagedWorkItemCount => 0;

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern long GetPendingUnmanagedWorkItemCount();

        private static RegisteredWaitHandle RegisterWaitForSingleObject(
             WaitHandle waitObject,
             WaitOrTimerCallback callBack,
             object? state,
             uint millisecondsTimeOutInterval,
             bool executeOnlyOnce,
             bool flowExecutionContext)
        {
            ArgumentNullException.ThrowIfNull(waitObject);
            ArgumentNullException.ThrowIfNull(callBack);

            RegisteredWaitHandle registeredWaitHandle = new RegisteredWaitHandle(
                waitObject,
                new _ThreadPoolWaitOrTimerCallback(callBack, state, flowExecutionContext),
                (int)millisecondsTimeOutInterval,
                !executeOnlyOnce);

            registeredWaitHandle.OnBeforeRegister();

            PortableThreadPool.ThreadPoolInstance.RegisterWaitHandle(registeredWaitHandle);

            return registeredWaitHandle;
        }

        internal static void RequestWorkerThread()
        {
            PortableThreadPool.ThreadPoolInstance.RequestWorker();
        }

        /// <summary>
        /// Called from the gate thread periodically to perform runtime-specific gate activities
        /// </summary>
        /// <param name="cpuUtilization">CPU utilization as a percentage since the last call</param>
        /// <returns>True if the runtime still needs to perform gate activities, false otherwise</returns>
        internal static bool PerformRuntimeSpecificGateActivities(int cpuUtilization)
        {
            return false;
        }

        // Entry point from unmanaged code
        private static void UnsafeQueueUnmanagedWorkItem(IntPtr callback, IntPtr state)
        {
            UnsafeQueueHighPriorityWorkItemInternal(new UnmanagedThreadPoolWorkItem(callback, state));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool NotifyWorkItemComplete(object? threadLocalCompletionCountObject, int currentTimeMs)
        {
            return
                PortableThreadPool.ThreadPoolInstance.NotifyWorkItemComplete(
                    threadLocalCompletionCountObject,
                    currentTimeMs);
        }

        internal static void ReportThreadStatus(bool isWorking)
        {
            PortableThreadPool.ThreadPoolInstance.ReportThreadStatus(isWorking);
        }

        internal static void NotifyWorkItemProgress()
        {
            PortableThreadPool.ThreadPoolInstance.NotifyWorkItemProgress();
        }

        internal static bool NotifyThreadBlocked() => PortableThreadPool.ThreadPoolInstance.NotifyThreadBlocked();

        internal static void NotifyThreadUnblocked()
        {
            PortableThreadPool.ThreadPoolInstance.NotifyThreadUnblocked();
        }

        internal static object? GetOrCreateThreadLocalCompletionCountObject() =>
            PortableThreadPool.ThreadPoolInstance.GetOrCreateThreadLocalCompletionCountObject();
    }
}
