// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Threading
{
    internal sealed class Lock : IDisposable
    {
        private ReaderWriterLockSlim _thisLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private int _isDisposed = 0;

        public void EnterReadLock()
        {
            _thisLock.EnterReadLock();
        }

        public void EnterWriteLock()
        {
            _thisLock.EnterWriteLock();
        }

        public void ExitReadLock()
        {
            _thisLock.ExitReadLock();
        }

        public void ExitWriteLock()
        {
            _thisLock.ExitWriteLock();
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
            {
                _thisLock.Dispose();
            }
        }
    }
}
