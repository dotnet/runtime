// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Microsoft.Internal
{
    internal sealed class Lock : IDisposable
    {
#if FEATURE_SLIMLOCK
        private readonly ReaderWriterLockSlim _thisLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
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

#else
        // ReaderWriterLockSlim is not yet implemented on SilverLight
        // Satisfies our requirements until it is implemented
        private readonly object _thisLock = new object();

        public Lock()
        {
        }

        public void EnterReadLock()
        {
            Monitor.Enter(_thisLock);
        }

        public void EnterWriteLock()
        {
            Monitor.Enter(_thisLock);
        }

        public void ExitReadLock()
        {
            Monitor.Exit(_thisLock);
        }

        public void ExitWriteLock()
        {
            Monitor.Exit(_thisLock);
        }

        public void Dispose()
        {
        }
#endif //FEATURE_SLIMLOCK
    }
}
