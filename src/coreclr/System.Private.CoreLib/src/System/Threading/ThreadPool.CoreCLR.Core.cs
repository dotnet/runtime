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
    internal sealed partial class CompleteWaitThreadPoolWorkItem : IThreadPoolWorkItem
    {
        void IThreadPoolWorkItem.Execute() => CompleteWait();

        // Entry point from unmanaged code
        private void CompleteWait()
        {
            PortableThreadPool.CompleteWait(_registeredWaitHandle, _timedOut);
        }
    }

    public static partial class ThreadPool
    {
        private static bool EnsureConfigInitializedCore()
        {
            return s_initialized;
        }

        private static readonly bool s_initialized = InitializeConfig();

        // Indicates whether the thread pool should yield the thread from the dispatch loop to the runtime periodically so that
        // the runtime may use the thread for processing other work
        internal static bool YieldFromDispatchLoop => false;

        private static readonly bool IsWorkerTrackingEnabledInConfig = GetEnableWorkerTracking();

        private static unsafe bool InitializeConfig()
        {
            int configVariableIndex = 1;
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

                Debug.Assert(appContextConfigNameUnsafe != null);

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

            return true;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe int GetNextConfigUInt32Value(
            int configVariableIndex,
            out uint configValue,
            out bool isBoolean,
            out char* appContextConfigName);

        private static bool GetEnableWorkerTracking() =>
            AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.EnableWorkerTracking", false);

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

        private static void InitializeForThreadPoolThreadPortableCore() { }

        [SupportedOSPlatform("windows")]
        private static unsafe bool UnsafeQueueNativeOverlappedPortableCore(NativeOverlapped* overlapped)
        {
            if (overlapped == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.overlapped);
            }

            // OS doesn't signal handle, so do it here
            overlapped->InternalLow = IntPtr.Zero;

            PortableThreadPool.ThreadPoolInstance.QueueNativeOverlapped(overlapped);
            return true;
        }

        [Obsolete("ThreadPool.BindHandle(IntPtr) has been deprecated. Use ThreadPool.BindHandle(SafeHandle) instead.")]
        [SupportedOSPlatform("windows")]
        private static bool BindHandlePortableCore(IntPtr osHandle)
        {
            PortableThreadPool.ThreadPoolInstance.RegisterForIOCompletionNotifications(osHandle);
            return true;
        }

        [SupportedOSPlatform("windows")]
        private static bool BindHandlePortableCore(SafeHandle osHandle)
        {
            ArgumentNullException.ThrowIfNull(osHandle);

            bool mustReleaseSafeHandle = false;
            try
            {
                osHandle.DangerousAddRef(ref mustReleaseSafeHandle);

                PortableThreadPool.ThreadPoolInstance.RegisterForIOCompletionNotifications(osHandle.DangerousGetHandle());
                return true;
            }
            finally
            {
                if (mustReleaseSafeHandle)
                    osHandle.DangerousRelease();
            }
        }

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

            PortableThreadPool.ThreadPoolInstance.RegisterWaitHandle(registeredWaitHandle);

            return registeredWaitHandle;
        }

        private static void ReportThreadStatusCore(bool isWorking)
        {
            PortableThreadPool.ThreadPoolInstance.ReportThreadStatus(isWorking);
        }
    }
}
