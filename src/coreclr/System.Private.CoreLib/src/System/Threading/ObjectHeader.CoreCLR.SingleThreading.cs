// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading;

internal static partial class ObjectHeader
{
    // Valid header read/modify/write sequences must not call managed code, including in the interpreter.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryAcquireThinLock(object obj, int millisecondsTimeout = 0)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return TryAcquireThinLockCore(obj) || GetLockObject(obj).TryEnter_Outlined(millisecondsTimeout);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AcquireThinLock(object obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        if (!TryAcquireThinLockCore(obj))
        {
            GetLockObject(obj).Enter();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe bool TryAcquireThinLockCore(object obj)
    {
        fixed (nint* ppMethodTable = &obj.GetMethodTableRef())
        {
            int* header = GetHeaderPtr(ppMethodTable);
            int bits = *header;

            Debug.Assert((bits & BIT_SBLK_SPIN_LOCK) == 0);

            if ((bits & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX) != 0)
            {
                return false;
            }

            int owner = bits & SBLK_MASK_LOCK_THREADID;
            if (owner == 0)
            {
                *header = bits | ManagedThreadId.SingleThreadId;
                return true;
            }

            Debug.Assert(owner == ManagedThreadId.SingleThreadId);

            int newBits = bits + SBLK_LOCK_RECLEVEL_INC;
            if ((newBits & SBLK_MASK_LOCK_RECLEVEL) == 0)
            {
                return false;
            }

            *header = newBits;
            return true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Release(object obj)
    {
        fixed (nint* ppMethodTable = &obj.GetMethodTableRef())
        {
            int* header = GetHeaderPtr(ppMethodTable);
            int bits = *header;

            Debug.Assert((bits & BIT_SBLK_SPIN_LOCK) == 0);

            if ((bits & (BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | SBLK_MASK_LOCK_THREADID)) == ManagedThreadId.SingleThreadId)
            {
                *header = (bits & SBLK_MASK_LOCK_RECLEVEL) != 0
                    ? bits - SBLK_LOCK_RECLEVEL_INC
                    : bits & ~SBLK_MASK_LOCK_THREADID;
                return;
            }
        }

        GetLockObject(obj).Exit();
    }
}
