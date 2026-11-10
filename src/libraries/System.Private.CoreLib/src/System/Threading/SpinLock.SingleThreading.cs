// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading;

public partial struct SpinLock
{
    private int _owner;

    private void EnterCore(ref bool lockTaken) => TryEnterCore(Timeout.Infinite, ref lockTaken);

    private void TryEnterCore(ref bool lockTaken) => TryEnterCore(0, ref lockTaken);

    private void TryEnterCore(int millisecondsTimeout, ref bool lockTaken)
    {
        if (lockTaken)
        {
            lockTaken = false;
            throw new ArgumentException(SR.SpinLock_TryReliableEnter_ArgumentException);
        }

        if (millisecondsTimeout < -1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(millisecondsTimeout), millisecondsTimeout, SR.SpinLock_TryEnter_ArgumentOutOfRange);
        }

        if (IsThreadOwnerTrackingEnabled)
        {
            int threadId = Environment.CurrentManagedThreadId;
            if (_owner != LOCK_UNOWNED)
            {
                throw new LockRecursionException(SR.SpinLock_TryEnter_LockRecursionException);
            }

            _owner = threadId;
        }
        else if ((_owner & LOCK_ANONYMOUS_OWNED) == 0)
        {
            _owner |= LOCK_ANONYMOUS_OWNED;
        }
        else
        {
            if (millisecondsTimeout == Timeout.Infinite)
            {
                throw new PlatformNotSupportedException();
            }

            if (millisecondsTimeout != 0)
            {
                Thread.Sleep(millisecondsTimeout);
            }

            return;
        }

        lockTaken = true;
    }

    private void ExitCore() => ExitCore(useMemoryBarrier: true);

    private void ExitCore(bool useMemoryBarrier)
    {
        if (IsThreadOwnerTrackingEnabled)
        {
            int threadId = Environment.CurrentManagedThreadId;
            if (_owner != threadId)
            {
                ThrowHelper.ThrowSynchronizationLockException_LockExit();
            }

            _owner = LOCK_UNOWNED;
        }
        else if (useMemoryBarrier)
        {
            _owner--;
        }
        else
        {
            _owner &= ~LOCK_ANONYMOUS_OWNED;
        }
    }
}
