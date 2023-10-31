// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Microsoft.Internal
{
    internal struct WriteLock : IDisposable
    {
        private readonly ReadWriteLock _lock;
        private int _isDisposed;

        public WriteLock(ReadWriteLock @lock)
        {
            _isDisposed = 0;
            _lock = @lock;
            _lock.EnterWriteLock();
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
            {
                _lock.ExitWriteLock();
            }
        }
    }
}
