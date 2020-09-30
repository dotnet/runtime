// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
using System.Runtime.Versioning;

namespace System.Threading
{
    //
    // Portable implementation of ThreadPool
    //

    /// <summary>
    /// An object representing the registration of a <see cref="WaitHandle"/> via <see cref="ThreadPool.RegisterWaitForSingleObject"/>.
    /// </summary>
    [UnsupportedOSPlatform("browser")]
    public sealed class RegisteredWaitHandle : MarshalByRefObject
    {
        internal RegisteredWaitHandle(WaitHandle waitHandle, _ThreadPoolWaitOrTimerCallback callbackHelper,
            int millisecondsTimeout, bool repeating)
        {
            Handle = waitHandle;
            Callback = callbackHelper;
            TimeoutDurationMs = millisecondsTimeout;
            Repeating = repeating;
            RestartTimeout(Environment.TickCount);
        }

        ~RegisteredWaitHandle()
        {
            if (WaitThread != null)
            {
                Unregister(null);
            }
        }

        private static AutoResetEvent? s_cachedEvent;

        private static AutoResetEvent RentEvent() =>
            Interlocked.Exchange(ref s_cachedEvent, null) ??
            new AutoResetEvent(false);

        private static void ReturnEvent(AutoResetEvent resetEvent)
        {
            if (Interlocked.CompareExchange(ref s_cachedEvent, resetEvent, null) != null)
            {
                resetEvent.Dispose();
            }
        }

        /// <summary>
        /// The callback to execute when the wait on <see cref="Handle"/> either times out or completes.
        /// </summary>
        internal _ThreadPoolWaitOrTimerCallback Callback { get; }

        /// <summary>
        /// The <see cref="WaitHandle"/> that was registered.
        /// </summary>
        internal WaitHandle Handle { get; }

        /// <summary>
        /// The time this handle times out at in ms.
        /// </summary>
        internal int TimeoutTimeMs { get; private set; }

        private int TimeoutDurationMs { get; }

        internal bool IsInfiniteTimeout => TimeoutDurationMs == -1;

        internal void RestartTimeout(int currentTimeMs)
        {
            TimeoutTimeMs = currentTimeMs + TimeoutDurationMs;
        }

        /// <summary>
        /// Whether or not the wait is a repeating wait.
        /// </summary>
        internal bool Repeating { get; }

        /// <summary>
        /// The <see cref="WaitHandle"/> the user passed in via <see cref="Unregister(WaitHandle)"/>.
        /// </summary>
        private SafeWaitHandle? UserUnregisterWaitHandle { get; set; }

        private IntPtr UserUnregisterWaitHandleValue { get; set; }

        internal bool IsBlocking => UserUnregisterWaitHandleValue == (IntPtr)(-1);

        /// <summary>
        /// The <see cref="PortableThreadPool.WaitThread"/> this <see cref="RegisteredWaitHandle"/> was registered on.
        /// </summary>
        internal PortableThreadPool.WaitThread? WaitThread { get; set; }

        /// <summary>
        /// The number of callbacks that are currently queued on the Thread Pool or executing.
        /// </summary>
        private int _numRequestedCallbacks;

        private readonly LowLevelLock _callbackLock = new LowLevelLock();

        /// <summary>
        /// Notes if we need to signal the user's unregister event after all callbacks complete.
        /// </summary>
        private bool _signalAfterCallbacksComplete;

        private bool _unregisterCalled;

        private bool _unregistered;

        private AutoResetEvent? _callbacksComplete;

        private AutoResetEvent? _removed;

        /// <summary>
        /// Unregisters this wait handle registration from the wait threads.
        /// </summary>
        /// <param name="waitObject">The event to signal when the handle is unregistered.</param>
        /// <returns>If the handle was successfully marked to be removed and the provided wait handle was set as the user provided event.</returns>
        /// <remarks>
        /// This method will only return true on the first call.
        /// Passing in a wait handle with a value of -1 will result in a blocking wait, where Unregister will not return until the full unregistration is completed.
        /// </remarks>
        public bool Unregister(WaitHandle? waitObject)
        {
            GC.SuppressFinalize(this);
            _callbackLock.Acquire();
            bool needToRollBackRefCountOnException = false;
            try
            {
                if (_unregisterCalled)
                {
                    return false;
                }

                UserUnregisterWaitHandle = waitObject?.SafeWaitHandle;
                UserUnregisterWaitHandle?.DangerousAddRef(ref needToRollBackRefCountOnException);

                UserUnregisterWaitHandleValue = UserUnregisterWaitHandle?.DangerousGetHandle() ?? IntPtr.Zero;

                if (_unregistered)
                {
                    SignalUserWaitHandle();
                    return true;
                }

                if (IsBlocking)
                {
                    _callbacksComplete = RentEvent();
                }
                else
                {
                    _removed = RentEvent();
                }
                _unregisterCalled = true;
            }
            catch (Exception) // Rollback state on exception
            {
                if (_removed != null)
                {
                    ReturnEvent(_removed);
                    _removed = null;
                }
                else if (_callbacksComplete != null)
                {
                    ReturnEvent(_callbacksComplete);
                    _callbacksComplete = null;
                }

                UserUnregisterWaitHandleValue = IntPtr.Zero;

                if (needToRollBackRefCountOnException)
                {
                    UserUnregisterWaitHandle?.DangerousRelease();
                }

                UserUnregisterWaitHandle  = null;
                throw;
            }
            finally
            {
                _callbackLock.Release();
            }

            WaitThread!.UnregisterWait(this);
            return true;
        }

        /// <summary>
        /// Signal <see cref="UserUnregisterWaitHandle"/> if it has not been signaled yet and is a valid handle.
        /// </summary>
        private void SignalUserWaitHandle()
        {
            _callbackLock.VerifyIsLocked();
            SafeWaitHandle? handle = UserUnregisterWaitHandle;
            IntPtr handleValue = UserUnregisterWaitHandleValue;
            try
            {
                if (handleValue != IntPtr.Zero && handleValue != (IntPtr)(-1))
                {
                    Debug.Assert(handleValue == handle!.DangerousGetHandle());
                    EventWaitHandle.Set(handle);
                }
            }
            finally
            {
                handle?.DangerousRelease();
                _callbacksComplete?.Set();
                _unregistered = true;
            }
        }

        /// <summary>
        /// Perform the registered callback if the <see cref="UserUnregisterWaitHandle"/> has not been signaled.
        /// </summary>
        /// <param name="timedOut">Whether or not the wait timed out.</param>
        internal void PerformCallback(bool timedOut)
        {
#if DEBUG
            _callbackLock.Acquire();
            try
            {
                Debug.Assert(_numRequestedCallbacks != 0);
            }
            finally
            {
                _callbackLock.Release();
            }
#endif
            _ThreadPoolWaitOrTimerCallback.PerformWaitOrTimerCallback(Callback, timedOut);
            CompleteCallbackRequest();
        }

        /// <summary>
        /// Tell this handle that there is a callback queued on the thread pool for this handle.
        /// </summary>
        internal void RequestCallback()
        {
            _callbackLock.Acquire();
            try
            {
                _numRequestedCallbacks++;
            }
            finally
            {
                _callbackLock.Release();
            }
        }

        /// <summary>
        /// Called when the wait thread removes this handle registration. This will signal the user's event if there are no callbacks pending,
        /// or note that the user's event must be signaled when the callbacks complete.
        /// </summary>
        internal void OnRemoveWait()
        {
            _callbackLock.Acquire();
            try
            {
                _removed?.Set();
                if (_numRequestedCallbacks == 0)
                {
                    SignalUserWaitHandle();
                }
                else
                {
                    _signalAfterCallbacksComplete = true;
                }
            }
            finally
            {
                _callbackLock.Release();
            }
        }

        /// <summary>
        /// Reduces the number of callbacks requested. If there are no more callbacks and the user's handle is queued to be signaled, signal it.
        /// </summary>
        private void CompleteCallbackRequest()
        {
            _callbackLock.Acquire();
            try
            {
                --_numRequestedCallbacks;
                if (_numRequestedCallbacks == 0 && _signalAfterCallbacksComplete)
                {
                    SignalUserWaitHandle();
                }
            }
            finally
            {
                _callbackLock.Release();
            }
        }

        /// <summary>
        /// Wait for all queued callbacks and the full unregistration to complete.
        /// </summary>
        internal void WaitForCallbacks()
        {
            Debug.Assert(IsBlocking);
            Debug.Assert(_unregisterCalled); // Should only be called when the wait is unregistered by the user.

            _callbacksComplete!.WaitOne();
            ReturnEvent(_callbacksComplete);
            _callbacksComplete = null;
        }

        internal void WaitForRemoval()
        {
            Debug.Assert(!IsBlocking);
            Debug.Assert(_unregisterCalled); // Should only be called when the wait is unregistered by the user.

            _removed!.WaitOne();
            ReturnEvent(_removed);
            _removed = null;
        }
    }

    public static partial class ThreadPool
    {
        internal const bool EnableWorkerTracking = false;

        internal static void InitializeForThreadPoolThread() { }

        public static bool SetMaxThreads(int workerThreads, int completionPortThreads)
        {
            if (workerThreads < 0 || completionPortThreads < 0)
            {
                return false;
            }
            return PortableThreadPool.ThreadPoolInstance.SetMaxThreads(workerThreads);
        }

        public static void GetMaxThreads(out int workerThreads, out int completionPortThreads)
        {
            // Note that worker threads and completion port threads share the same thread pool.
            // The total number of threads cannot exceed MaxThreadCount.
            workerThreads = PortableThreadPool.ThreadPoolInstance.GetMaxThreads();
            completionPortThreads = 1;
        }

        public static bool SetMinThreads(int workerThreads, int completionPortThreads)
        {
            if (workerThreads < 0 || completionPortThreads < 0)
            {
                return false;
            }
            return PortableThreadPool.ThreadPoolInstance.SetMinThreads(workerThreads);
        }

        public static void GetMinThreads(out int workerThreads, out int completionPortThreads)
        {
            // All threads are pre-created at present
            workerThreads = PortableThreadPool.ThreadPoolInstance.GetMinThreads();
            completionPortThreads = 0;
        }

        public static void GetAvailableThreads(out int workerThreads, out int completionPortThreads)
        {
            workerThreads = PortableThreadPool.ThreadPoolInstance.GetAvailableThreads();
            completionPortThreads = 0;
        }

        /// <summary>
        /// Gets the number of thread pool threads that currently exist.
        /// </summary>
        /// <remarks>
        /// For a thread pool implementation that may have different types of threads, the count includes all types.
        /// </remarks>
        public static int ThreadCount => PortableThreadPool.ThreadPoolInstance.ThreadCount;

        /// <summary>
        /// Gets the number of work items that have been processed by the thread pool so far.
        /// </summary>
        /// <remarks>
        /// For a thread pool implementation that may have different types of work items, the count includes all types.
        /// </remarks>
        public static long CompletedWorkItemCount => PortableThreadPool.ThreadPoolInstance.CompletedWorkItemCount;

        /// <summary>
        /// This method is called to request a new thread pool worker to handle pending work.
        /// </summary>
        internal static void RequestWorkerThread()
        {
            PortableThreadPool.ThreadPoolInstance.RequestWorker();
        }

        internal static bool KeepDispatching(int startTickCount)
        {
            return true;
        }

        internal static void NotifyWorkItemProgress()
        {
            PortableThreadPool.ThreadPoolInstance.NotifyWorkItemComplete();
        }

        internal static bool NotifyWorkItemComplete()
        {
            return PortableThreadPool.ThreadPoolInstance.NotifyWorkItemComplete();
        }

        private static RegisteredWaitHandle RegisterWaitForSingleObject(
             WaitHandle waitObject,
             WaitOrTimerCallback callBack,
             object? state,
             uint millisecondsTimeOutInterval,
             bool executeOnlyOnce,
             bool flowExecutionContext)
        {
            if (waitObject == null)
                throw new ArgumentNullException(nameof(waitObject));

            if (callBack == null)
                throw new ArgumentNullException(nameof(callBack));

            RegisteredWaitHandle registeredHandle = new RegisteredWaitHandle(
                waitObject,
                new _ThreadPoolWaitOrTimerCallback(callBack, state, flowExecutionContext),
                (int)millisecondsTimeOutInterval,
                !executeOnlyOnce);
            PortableThreadPool.ThreadPoolInstance.RegisterWaitHandle(registeredHandle);
            return registeredHandle;
        }
    }
}
