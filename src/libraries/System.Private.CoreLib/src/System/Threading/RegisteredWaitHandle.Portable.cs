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

        public bool Unregister(WaitHandle waitObject) => UnregisterPortableCore(waitObject);

        internal void PerformCallback(bool timedOut)
        {
            PerformCallbackPortableCore(timedOut);
        }
    }
}
