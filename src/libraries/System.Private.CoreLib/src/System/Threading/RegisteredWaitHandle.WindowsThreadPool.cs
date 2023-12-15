// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    //
    // Windows-specific implementation of ThreadPool
    //
    public sealed partial class RegisteredWaitHandle : MarshalByRefObject
    {
        private readonly object? _lock;
        private bool _unregistering;

        // Handle to this object to keep it alive
        private GCHandle _gcHandle;

        // Pointer to the TP_WAIT structure
        private IntPtr _tpWait;

        internal unsafe RegisteredWaitHandle(SafeWaitHandle waitHandle, _ThreadPoolWaitOrTimerCallback callbackHelper,
            uint millisecondsTimeout, bool repeating)
        {
            Debug.Assert(ThreadPool.UseWindowsThreadPool);

            _lock = new object();

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

            if (NativeRuntimeEventSource.Log.IsEnabled())
                NativeRuntimeEventSource.Log.ThreadPoolIOEnqueue(this);
        }

#pragma warning disable IDE0060 // Remove unused parameter
        [UnmanagedCallersOnly]
        internal static void RegisteredWaitCallback(IntPtr instance, IntPtr context, IntPtr wait, uint waitResult)
        {
            var wrapper = ThreadPoolCallbackWrapper.Enter();

            GCHandle handle = (GCHandle)context;
            RegisteredWaitHandle registeredWaitHandle = (RegisteredWaitHandle)handle.Target!;
            Debug.Assert((handle == registeredWaitHandle._gcHandle) && (wait == registeredWaitHandle._tpWait));

            bool timedOut = (waitResult == (uint)Interop.Kernel32.WAIT_TIMEOUT);
            registeredWaitHandle.PerformCallbackWindowsThreadPool(timedOut);
            ThreadPool.IncrementCompletedWorkItemCount();

            wrapper.Exit();
        }
#pragma warning restore IDE0060

        private void PerformCallbackWindowsThreadPool(bool timedOut)
        {
            // Prevent the race condition with Unregister and the previous PerformCallback call, which may still be
            // holding the _lock.
            // If another thread is running Unregister, no need to restart the timer or clean up
            lock (_lock!)
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

            if (NativeRuntimeEventSource.Log.IsEnabled())
                NativeRuntimeEventSource.Log.ThreadPoolIODequeue(this);

            _ThreadPoolWaitOrTimerCallback.PerformWaitOrTimerCallback(_callbackHelper!, timedOut);
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
            Interop.Kernel32.SetThreadpoolWait(_tpWait, _waitHandle!.DangerousGetHandle(), (IntPtr)pTimeout);
        }

        private bool UnregisterWindowsThreadPool(WaitHandle waitObject)
        {
            // Hold the lock during the synchronous part of Unregister (as in CoreCLR)
            lock (_lock!)
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
            Debug.Assert(ThreadPool.UseWindowsThreadPool);
            // If _gcHandle is allocated, it points to this object, so this object must not be collected by the GC
            Debug.Assert(!_gcHandle.IsAllocated);

            // If this object gets resurrected and another thread calls Unregister, that creates a race condition.
            // Do not block the finalizer thread. If another thread is running Unregister, it will clean up for us.
            // The _lock may be null in case of OOM in the constructor.
            if ((_lock != null) && Monitor.TryEnter(_lock))
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
                    Monitor.Exit(_lock);
                }
            }
        }
    }

}
