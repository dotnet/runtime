// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Threading
{
    /// <summary>
    /// Wraps a non-recursive mutex and condition.
    ///
    /// Used by the other threading subsystems, so this type cannot have any dependencies on them.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    internal partial struct LowLevelMonitor
    {
#if DEBUG
        private Thread? _ownerThread;
#endif

        public void Dispose()
        {
            VerifyIsNotLockedByAnyThread();
            DisposeCore();
        }

#if DEBUG
        public bool IsLocked => _ownerThread == Thread.CurrentThread;
#endif

        [Conditional("DEBUG")]
        public void VerifyIsLocked()
        {
#if DEBUG
            Debug.Assert(IsLocked);
#endif
        }

        [Conditional("DEBUG")]
        public void VerifyIsNotLocked()
        {
#if DEBUG
            Debug.Assert(!IsLocked);
#endif
        }

        [Conditional("DEBUG")]
        private void VerifyIsNotLockedByAnyThread()
        {
#if DEBUG
            Debug.Assert(_ownerThread == null);
#endif
        }

        [Conditional("DEBUG")]
        private void ResetOwnerThread()
        {
            VerifyIsLocked();
#if DEBUG
            _ownerThread = null;
#endif
        }

        [Conditional("DEBUG")]
        private void SetOwnerThreadToCurrent()
        {
            VerifyIsNotLockedByAnyThread();
#if DEBUG
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

        public bool Wait(int timeoutMilliseconds)
        {
            Debug.Assert(timeoutMilliseconds >= -1);

            ResetOwnerThread();
            bool waitResult = WaitCore(timeoutMilliseconds);
            SetOwnerThreadToCurrent();
            return waitResult;
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
