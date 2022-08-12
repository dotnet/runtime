// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    /// <summary>
    /// An object representing the registration of a <see cref="WaitHandle"/> via <see cref="ThreadPool.RegisterWaitForSingleObject"/>.
    /// </summary>
#if !FEATURE_WASM_THREADS
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
#endif
    public sealed partial class RegisteredWaitHandle : MarshalByRefObject
    {
        internal RegisteredWaitHandle(WaitHandle waitHandle, _ThreadPoolWaitOrTimerCallback callbackHelper,
            int millisecondsTimeout, bool repeating)
        {
            Thread.ThrowIfNoThreadStart();
            Handle = waitHandle.SafeWaitHandle;
            Callback = callbackHelper;
            TimeoutDurationMs = millisecondsTimeout;
            Repeating = repeating;
            if (!IsInfiniteTimeout)
            {
                RestartTimeout();
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

        private static readonly LowLevelLock s_callbackLock = new LowLevelLock();

        /// <summary>
        /// The callback to execute when the wait on <see cref="Handle"/> either times out or completes.
        /// </summary>
        internal _ThreadPoolWaitOrTimerCallback Callback { get; }

        /// <summary>
        /// The <see cref="SafeWaitHandle"/> that was registered.
        /// </summary>
        internal SafeWaitHandle Handle { get; }

        /// <summary>
        /// The time this handle times out at in ms.
        /// </summary>
        internal int TimeoutTimeMs { get; private set; }

        internal int TimeoutDurationMs { get; }

        internal bool IsInfiniteTimeout => TimeoutDurationMs == -1;

        internal void RestartTimeout()
        {
            Debug.Assert(!IsInfiniteTimeout);
            TimeoutTimeMs = Environment.TickCount + TimeoutDurationMs;
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

        private static IntPtr InvalidHandleValue => new IntPtr(-1);

        internal bool IsBlocking => UserUnregisterWaitHandleValue == InvalidHandleValue;

        /// <summary>
        /// The number of callbacks that are currently queued on the Thread Pool or executing.
        /// </summary>
        private int _numRequestedCallbacks;

        /// <summary>
        /// Notes if we need to signal the user's unregister event after all callbacks complete.
        /// </summary>
        private bool _signalAfterCallbacksComplete;

        private bool _unregisterCalled;

        private bool _unregistered;

        private AutoResetEvent? _callbacksComplete;

        private AutoResetEvent? _removed;

        /// <summary>
        /// The <see cref="PortableThreadPool.WaitThread"/> this <see cref="RegisteredWaitHandle"/> was registered on.
        /// </summary>
        internal PortableThreadPool.WaitThread? WaitThread { get; set; }

#if CORECLR
        private bool UnregisterPortable(WaitHandle waitObject)
#else
        public bool Unregister(WaitHandle waitObject)
#endif
        {
            // The registered wait handle must have been registered by this time, otherwise the instance is not handed out to
            // the caller of the public variants of RegisterWaitForSingleObject
            Debug.Assert(WaitThread != null);

            s_callbackLock.Acquire();
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

                UserUnregisterWaitHandle = null;
                throw;
            }
            finally
            {
                _unregisterCalled = true;
                s_callbackLock.Release();
            }

            WaitThread!.UnregisterWait(this);
            return true;
        }

        /// <summary>
        /// Signal <see cref="UserUnregisterWaitHandle"/> if it has not been signaled yet and is a valid handle.
        /// </summary>
        private void SignalUserWaitHandle()
        {
            s_callbackLock.VerifyIsLocked();
            SafeWaitHandle? handle = UserUnregisterWaitHandle;
            IntPtr handleValue = UserUnregisterWaitHandleValue;
            try
            {
                if (handleValue != IntPtr.Zero && handleValue != InvalidHandleValue)
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
            s_callbackLock.Acquire();
            try
            {
                Debug.Assert(_numRequestedCallbacks != 0);
            }
            finally
            {
                s_callbackLock.Release();
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
            s_callbackLock.Acquire();
            try
            {
                _numRequestedCallbacks++;
            }
            finally
            {
                s_callbackLock.Release();
            }
        }

        /// <summary>
        /// Called when the wait thread removes this handle registration. This will signal the user's event if there are no callbacks pending,
        /// or note that the user's event must be signaled when the callbacks complete.
        /// </summary>
        internal void OnRemoveWait()
        {
            s_callbackLock.Acquire();
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
                s_callbackLock.Release();
            }
        }

        /// <summary>
        /// Reduces the number of callbacks requested. If there are no more callbacks and the user's handle is queued to be signaled, signal it.
        /// </summary>
        private void CompleteCallbackRequest()
        {
            s_callbackLock.Acquire();
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
                s_callbackLock.Release();
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
}
