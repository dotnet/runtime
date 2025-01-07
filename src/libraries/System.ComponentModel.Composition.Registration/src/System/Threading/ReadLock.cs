// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    internal struct ReadLock : IDisposable
    {
        private readonly ReadWriteLock _lock;
        private int _isDisposed;

        public ReadLock(ReadWriteLock @lock)
        {
            _isDisposed = 0;
            _lock = @lock;
            _lock.EnterReadLock();
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
            {
                _lock.ExitReadLock();
            }
        }
    }
}
