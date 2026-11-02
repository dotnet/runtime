// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading;

public partial class ReaderWriterLockSlim
{
    private readonly bool _fIsReentrant;
    private int _readCount;
    private int _writeCount;
    private int _upgradeCount;
    private bool _fDisposed;

    public ReaderWriterLockSlim() : this(LockRecursionPolicy.NoRecursion)
    {
    }

    public ReaderWriterLockSlim(LockRecursionPolicy recursionPolicy) =>
        _fIsReentrant = recursionPolicy == LockRecursionPolicy.SupportsRecursion;

    private bool TryEnterReadLockCore(TimeoutTracker timeout)
    {
        ObjectDisposedException.ThrowIf(_fDisposed, this);
        if (!_fIsReentrant)
        {
            if (_writeCount != 0)
            {
                throw new LockRecursionException(SR.LockRecursionException_ReadAfterWriteNotAllowed);
            }

            if (_readCount != 0)
            {
                throw new LockRecursionException(SR.LockRecursionException_RecursiveReadNotAllowed);
            }
        }

        _readCount++;
        return true;
    }

    private bool TryEnterWriteLockCore(TimeoutTracker timeout)
    {
        ObjectDisposedException.ThrowIf(_fDisposed, this);
        if (!_fIsReentrant && _writeCount != 0)
        {
            throw new LockRecursionException(SR.LockRecursionException_RecursiveWriteNotAllowed);
        }

        if (_readCount != 0 && (!_fIsReentrant || (_writeCount == 0 && _upgradeCount == 0)))
        {
            throw new LockRecursionException(SR.LockRecursionException_WriteAfterReadNotAllowed);
        }

        _writeCount++;
        return true;
    }

    private bool TryEnterUpgradeableReadLockCore(TimeoutTracker timeout)
    {
        ObjectDisposedException.ThrowIf(_fDisposed, this);
        if (!_fIsReentrant)
        {
            if (_upgradeCount != 0)
            {
                throw new LockRecursionException(SR.LockRecursionException_RecursiveUpgradeNotAllowed);
            }

            if (_writeCount != 0)
            {
                throw new LockRecursionException(SR.LockRecursionException_UpgradeAfterWriteNotAllowed);
            }
        }

        if (_readCount != 0 && (!_fIsReentrant || (_upgradeCount == 0 && _writeCount == 0)))
        {
            throw new LockRecursionException(SR.LockRecursionException_UpgradeAfterReadNotAllowed);
        }

        _upgradeCount++;
        return true;
    }

    public void ExitReadLock()
    {
        if (_readCount < 1)
        {
            throw new SynchronizationLockException(SR.SynchronizationLockException_MisMatchedRead);
        }

        _readCount--;
    }

    public void ExitWriteLock()
    {
        if (_writeCount < 1)
        {
            throw new SynchronizationLockException(SR.SynchronizationLockException_MisMatchedWrite);
        }

        _writeCount--;
    }

    public void ExitUpgradeableReadLock()
    {
        if (_upgradeCount < 1)
        {
            throw new SynchronizationLockException(SR.SynchronizationLockException_MisMatchedUpgrade);
        }

        _upgradeCount--;
    }

    public void Dispose()
    {
        if (!_fDisposed)
        {
            if (IsReadLockHeld || IsWriteLockHeld || IsUpgradeableReadLockHeld)
            {
                throw new SynchronizationLockException(SR.SynchronizationLockException_IncorrectDispose);
            }

            _fDisposed = true;
        }
    }

    public bool IsReadLockHeld => _readCount > 0;
    public bool IsWriteLockHeld => _writeCount > 0;
    public bool IsUpgradeableReadLockHeld => _upgradeCount > 0;
    public LockRecursionPolicy RecursionPolicy =>
        _fIsReentrant ? LockRecursionPolicy.SupportsRecursion : LockRecursionPolicy.NoRecursion;
    public int CurrentReadCount => IsReadLockHeld ? 1 : 0;
    public int RecursiveReadCount => _readCount;
    public int RecursiveWriteCount => _writeCount;
    public int RecursiveUpgradeCount => _upgradeCount;
    public int WaitingReadCount => 0;
    public int WaitingWriteCount => 0;
    public int WaitingUpgradeCount => 0;
}
