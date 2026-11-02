// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics; // for TraceInformation
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    public enum LockRecursionPolicy
    {
        NoRecursion = 0,
        SupportsRecursion = 1,
    }

    /// <summary>
    /// Provides a reader-writer lock with optional recursive acquisition.
    /// </summary>
    public partial class ReaderWriterLockSlim : IDisposable
    {

        public void EnterReadLock()
        {
            TryEnterReadLock(-1);
        }

        //
        // Common timeout support
        //
        private readonly struct TimeoutTracker
        {
            private readonly int _total;
            private readonly int _start;

            public TimeoutTracker(TimeSpan timeout)
            {
                long ltm = (long)timeout.TotalMilliseconds;
                ArgumentOutOfRangeException.ThrowIfLessThan(ltm, -1, nameof(timeout));
                ArgumentOutOfRangeException.ThrowIfGreaterThan(ltm, int.MaxValue, nameof(timeout));

                _total = (int)ltm;
                if (_total != -1 && _total != 0)
                {
                    _start = Environment.TickCount;
                }
            }

            public TimeoutTracker(int millisecondsTimeout)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeout, -1);
                _total = millisecondsTimeout;
                if (_total != -1 && _total != 0)
                {
                    _start = Environment.TickCount;
                }
            }

            public int RemainingMilliseconds
            {
                get
                {
                    if (_total == -1 || _total == 0)
                        return _total;

                    int elapsed = Environment.TickCount - _start;
                    // elapsed may be negative if TickCount has overflowed by 2^31 milliseconds.
                    if (elapsed < 0 || elapsed >= _total)
                        return 0;

                    return _total - elapsed;
                }
            }

            public bool IsExpired => RemainingMilliseconds == 0;
        }

        public bool TryEnterReadLock(TimeSpan timeout)
        {
            return TryEnterReadLock(new TimeoutTracker(timeout));
        }

        public bool TryEnterReadLock(int millisecondsTimeout)
        {
            return TryEnterReadLock(new TimeoutTracker(millisecondsTimeout));
        }

        private bool TryEnterReadLock(TimeoutTracker timeout)
        {
            return TryEnterReadLockCore(timeout);
        }

        public void EnterWriteLock()
        {
            TryEnterWriteLock(-1);
        }

        public bool TryEnterWriteLock(TimeSpan timeout)
        {
            return TryEnterWriteLock(new TimeoutTracker(timeout));
        }

        public bool TryEnterWriteLock(int millisecondsTimeout)
        {
            return TryEnterWriteLock(new TimeoutTracker(millisecondsTimeout));
        }

        private bool TryEnterWriteLock(TimeoutTracker timeout)
        {
            return TryEnterWriteLockCore(timeout);
        }

        public void EnterUpgradeableReadLock()
        {
            TryEnterUpgradeableReadLock(-1);
        }

        public bool TryEnterUpgradeableReadLock(TimeSpan timeout)
        {
            return TryEnterUpgradeableReadLock(new TimeoutTracker(timeout));
        }

        public bool TryEnterUpgradeableReadLock(int millisecondsTimeout)
        {
            return TryEnterUpgradeableReadLock(new TimeoutTracker(millisecondsTimeout));
        }

        private bool TryEnterUpgradeableReadLock(TimeoutTracker timeout)
        {
            return TryEnterUpgradeableReadLockCore(timeout);
        }
    }
}
