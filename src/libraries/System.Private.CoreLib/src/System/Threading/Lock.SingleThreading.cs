// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading;

public sealed partial class Lock
{
    private const uint LockedState = 1;

    private void Initialize()
    {
    }

    private void InitializeUseTrivialWaits(bool _)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int TryEnter_Inlined(int timeoutMs)
    {
        Debug.Assert(timeoutMs >= -1);

        int threadId = ManagedThreadId.Current;
        Debug.Assert(_owningThreadId == UninitializedThreadId || _owningThreadId == threadId);
        if ((_state & LockedState) == 0)
        {
            _state = LockedState;
            _owningThreadId = threadId;
        }
        else
        {
            uint recursionCount = _recursionCount + 1;
            if (recursionCount == 0)
            {
                throw new LockRecursionException(SR.Lock_Enter_LockRecursionException);
            }

            _recursionCount = recursionCount;
        }

        return threadId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExitImpl()
    {
        Debug.Assert(IsHeldByCurrentThread);
        if (_recursionCount != 0)
        {
            _recursionCount--;
        }
        else
        {
            _owningThreadId = UninitializedThreadId;
            _state = 0;
        }
    }

    internal uint ExitAll()
    {
        Debug.Assert(IsHeldByCurrentThread);

        uint recursionCount = _recursionCount;
        _owningThreadId = UninitializedThreadId;
        _state = 0;
        _recursionCount = 0;
        return recursionCount;
    }

    private bool IsHeldByCurrentThreadCore => _owningThreadId != UninitializedThreadId;

    internal static long ContentionCount => 0;
    internal static bool IsSingleProcessor => true;
    internal nint LockIdForEvents => throw new UnreachableException();

    internal void Dispose()
    {
    }

    internal bool TryEnterOneShot(int currentManagedThreadId)
    {
        Debug.Assert(currentManagedThreadId == ManagedThreadId.Current);
        if ((_state & LockedState) != 0)
        {
            return false;
        }

        _state = LockedState;
        _owningThreadId = currentManagedThreadId;
        return true;
    }

    internal bool GetIsHeldByCurrentThread(int currentManagedThreadId)
    {
        Debug.Assert(currentManagedThreadId != UninitializedThreadId);
        return _owningThreadId == currentManagedThreadId;
    }

    internal void InitializeForMonitor(int managedThreadId, uint recursionCount)
    {
        Debug.Assert(managedThreadId == UninitializedThreadId || managedThreadId == ManagedThreadId.Current);
        Debug.Assert(recursionCount == 0 || managedThreadId != UninitializedThreadId);

        _state = managedThreadId == UninitializedThreadId ? 0 : LockedState;
        _owningThreadId = managedThreadId;
        _recursionCount = recursionCount;
    }

    internal bool Wait(int millisecondsTimeout, object associatedObject)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeout, -1);
        if (!IsHeldByCurrentThread)
        {
            throw new SynchronizationLockException();
        }

        if (millisecondsTimeout == Timeout.Infinite)
        {
            throw new PlatformNotSupportedException();
        }

        uint recursionCount = ExitAll();
        try
        {
            // Call into WaitSubsystem.Sleep to ensure we recognize a thread interrupt.
            WaitSubsystem.Sleep(millisecondsTimeout);
            return false;
        }
        finally
        {
            Reenter(recursionCount);
        }
    }

    internal void Pulse()
    {
        if (!IsHeldByCurrentThread)
        {
            throw new SynchronizationLockException();
        }
    }

    internal void PulseAll() => Pulse();
}
