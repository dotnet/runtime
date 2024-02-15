// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading
{
    internal partial struct LowLevelMonitor
    {
        private IntPtr _nativeMonitor;

        public void Initialize()
        {
            _nativeMonitor = Interop.Sys.LowLevelMonitor_Create();
            if (_nativeMonitor == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }
        }

        private void DisposeCore()
        {
            if (_nativeMonitor == IntPtr.Zero)
            {
                return;
            }

            Interop.Sys.LowLevelMonitor_Destroy(_nativeMonitor);
            _nativeMonitor = IntPtr.Zero;
        }

        private void AcquireCore()
        {
#if FEATURE_WASM_MANAGED_THREADS
            Thread.AssureBlockingPossible();
#endif
            Interop.Sys.LowLevelMonitor_Acquire(_nativeMonitor);
        }

        private void ReleaseCore()
        {
            Interop.Sys.LowLevelMonitor_Release(_nativeMonitor);
        }

        private void WaitCore()
        {
#if FEATURE_WASM_MANAGED_THREADS
            Thread.AssureBlockingPossible();
#endif
            Interop.Sys.LowLevelMonitor_Wait(_nativeMonitor);
        }

        private bool WaitCore(int timeoutMilliseconds)
        {
            Debug.Assert(timeoutMilliseconds >= -1);

            if (timeoutMilliseconds < 0)
            {
                WaitCore();
                return true;
            }

#if FEATURE_WASM_MANAGED_THREADS
            Thread.AssureBlockingPossible();
#endif

            return Interop.Sys.LowLevelMonitor_TimedWait(_nativeMonitor, timeoutMilliseconds);
        }

        private void Signal_ReleaseCore()
        {
            Interop.Sys.LowLevelMonitor_Signal_Release(_nativeMonitor);
        }
    }
}
