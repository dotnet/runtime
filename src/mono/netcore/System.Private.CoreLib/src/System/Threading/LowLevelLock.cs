// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading
{
    // This class provides implementation of uninterruptible lock for internal
    // use by thread pool.
    internal class LowLevelLock : IDisposable
    {
        public void Dispose()
        {
        }

        public bool TryAcquire()
        {
            bool lockTaken = false;
            Monitor.try_enter_with_atomic_var(this, 0, false, ref lockTaken);
            return lockTaken;
        }

        public void Acquire()
        {
            bool lockTaken = false;
            Monitor.try_enter_with_atomic_var(this, Timeout.Infinite, false, ref lockTaken);
        }

        public void Release()
        {
            Monitor.Exit(this);
        }

        [Conditional("DEBUG")]
        public void VerifyIsLocked()
        {
            Debug.Assert(Monitor.IsEntered(this));
        }
    }
}
