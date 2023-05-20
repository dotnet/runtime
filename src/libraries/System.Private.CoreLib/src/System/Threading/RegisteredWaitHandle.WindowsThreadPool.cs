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
    public sealed partial class RegisteredWaitHandle : MarshalByRefObject
    {
#pragma warning disable IDE0060 // Remove unused parameter
        [UnmanagedCallersOnly]
        internal static void RegisteredWaitCallback(IntPtr instance, IntPtr context, IntPtr wait, uint waitResult)
        {
            // Enabling for NativeAot first
#if NATIVEAOT
            var wrapper = ThreadPoolCallbackWrapper.Enter();
#endif
            GCHandle handle = (GCHandle)context;
            RegisteredWaitHandle registeredWaitHandle = (RegisteredWaitHandle)handle.Target!;
            Debug.Assert((handle == registeredWaitHandle._gcHandle) && (wait == registeredWaitHandle._tpWait));

            bool timedOut = (waitResult == (uint)Interop.Kernel32.WAIT_TIMEOUT);
            registeredWaitHandle.PerformCallbackWindowsThreadPool(timedOut);
            ThreadPool.IncrementCompletedWorkItemCount();
#if NATIVEAOT
            wrapper.Exit();
#endif
        }
#pragma warning restore IDE0060

        private void PerformCallbackWindowsThreadPool(bool timedOut)
        {
            // New logic might be wrong here, not sure yet
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
            lock(_lock!)
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
