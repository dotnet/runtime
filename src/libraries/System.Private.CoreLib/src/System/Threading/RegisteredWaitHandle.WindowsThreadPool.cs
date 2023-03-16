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
#if !FEATURE_WASM_THREADS
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
#endif
    public sealed partial class RegisteredWaitHandle : MarshalByRefObject
    {
        [UnmanagedCallersOnly]
        private static void RegisteredWaitCallbackCore(IntPtr instance, IntPtr context, IntPtr wait, uint waitResult)
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

        private void PerformCallbackCore(bool timedOut)
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

        private unsafe void RestartWaitCore()
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

        private bool UnregisterCore(WaitHandle waitObject)
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
    }

}
