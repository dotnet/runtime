// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading
{
    internal partial struct LowLevelMonitor
    {
        private nint _nativeMonitor;

        public void Initialize()
        {
            _nativeMonitor = Interop.Sys.LowLevelMonitor_Create();
            if (_nativeMonitor == 0)
            {
                throw new OutOfMemoryException();
            }
        }

        private void DisposeCore()
        {
            if (_nativeMonitor == 0)
            {
                return;
            }

            Interop.Sys.LowLevelMonitor_Destroy(_nativeMonitor);
            _nativeMonitor = 0;
        }

        private void AcquireCore()
        {
            Interop.Sys.LowLevelMonitor_Acquire(_nativeMonitor);
        }

        private void ReleaseCore()
        {
            Interop.Sys.LowLevelMonitor_Release(_nativeMonitor);
        }

        private void WaitCore()
        {
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

            return Interop.Sys.LowLevelMonitor_TimedWait(_nativeMonitor, timeoutMilliseconds);
        }

        private void Signal_ReleaseCore()
        {
            Interop.Sys.LowLevelMonitor_Signal_Release(_nativeMonitor);
        }
    }
}
