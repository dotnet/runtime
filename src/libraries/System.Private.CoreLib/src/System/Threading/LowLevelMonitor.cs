// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Threading
{
    /// <summary>
    /// Wraps a non-recursive mutex and condition.
    ///
    /// Used by the wait subsystem on Unix, so this class cannot have any dependencies on the wait subsystem.
    /// </summary>
    internal sealed partial class LowLevelMonitor : IDisposable
    {
#if DEBUG
        private Thread? _ownerThread;
#endif

        ~LowLevelMonitor()
        {
            Dispose();
        }

        public void Dispose()
        {
            VerifyIsNotLockedByAnyThread();
            DisposeCore();
            GC.SuppressFinalize(this);
        }

#if DEBUG
        public bool IsLocked => _ownerThread == Thread.CurrentThread;
#endif

        public void VerifyIsLocked()
        {
#if DEBUG
            Debug.Assert(IsLocked);
#endif
        }

        public void VerifyIsNotLocked()
        {
#if DEBUG
            Debug.Assert(!IsLocked);
#endif
        }

        private void VerifyIsNotLockedByAnyThread()
        {
#if DEBUG
            Debug.Assert(_ownerThread == null);
#endif
        }

        private void ResetOwnerThread()
        {
#if DEBUG
            VerifyIsLocked();
            _ownerThread = null;
#endif
        }

        private void SetOwnerThreadToCurrent()
        {
#if DEBUG
            VerifyIsNotLockedByAnyThread();
            _ownerThread = Thread.CurrentThread;
#endif
        }

        public void Acquire()
        {
            VerifyIsNotLocked();
            AcquireCore();
            SetOwnerThreadToCurrent();
        }

        public void Release()
        {
            ResetOwnerThread();
            ReleaseCore();
        }

        public void Wait()
        {
            ResetOwnerThread();
            WaitCore();
            SetOwnerThreadToCurrent();
        }

        public void Signal_Release()
        {
            ResetOwnerThread();
            Signal_ReleaseCore();
        }

        // The following methods typical in a monitor are omitted since they are currently not necessary for the way in which
        // this class is used:
        //   - TryAcquire
        //   - Signal (use Signal_Release instead)
        //   - SignalAll
    }
}
