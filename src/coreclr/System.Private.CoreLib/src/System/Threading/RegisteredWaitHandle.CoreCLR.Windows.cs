﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Threading
{
#if !FEATURE_WASM_THREADS
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
#endif
    public sealed partial class RegisteredWaitHandle : MarshalByRefObject
    {
        private readonly object _lock;
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
            if (!ThreadPool.UseWindowsThreadPool)
            {
                GC.SuppressFinalize(this);
            }
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

        [UnmanagedCallersOnly]
        internal static void RegisteredWaitCallback(IntPtr instance, IntPtr context, IntPtr wait, uint waitResult) =>
            RegisteredWaitCallbackCore(instance, context, wait, waitResult);

        internal unsafe void RestartWait() => RestartWaitCore();

        public bool Unregister(WaitHandle waitObject) => UnregisterCore(waitObject);

        ~RegisteredWaitHandle()
        {
            Debug.Assert(ThreadPool.UseWindowsThreadPool);
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
}
