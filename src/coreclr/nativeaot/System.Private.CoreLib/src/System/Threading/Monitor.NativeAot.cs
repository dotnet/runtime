// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
**
**
** Purpose: Synchronizes access to a shared resource or region of code in a multi-threaded
**             program.
**
**
=============================================================================*/

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

using Internal.Runtime;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;

namespace System.Threading
{
    public static partial class Monitor
    {
        #region Object->Lock mapping
        internal static Lock GetLockObject(object obj)
        {
            return ObjectHeader.GetLockObject(obj);
        }
        #endregion

        #region Public Enter/Exit methods

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Enter(object obj)
        {
            int currentThreadID = ManagedThreadId.CurrentManagedThreadIdUnchecked;
            int resultOrIndex = ObjectHeader.Acquire(obj, currentThreadID);
            if (resultOrIndex < 0)
                return;

            Lock lck = resultOrIndex == 0 ?
                ObjectHeader.GetLockObject(obj) :
                SyncTable.GetLockObject(resultOrIndex);

            lck.TryEnterSlow(Timeout.Infinite, currentThreadID);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryEnter(object obj)
        {
            int currentThreadID = ManagedThreadId.CurrentManagedThreadIdUnchecked;
            int resultOrIndex = ObjectHeader.TryAcquire(obj, currentThreadID);
            if (resultOrIndex < 0)
                return true;

            if (resultOrIndex == 0)
                return false;

            Lock lck = SyncTable.GetLockObject(resultOrIndex);

            // The one-shot fast path is not covered by the slow path below for a zero timeout when the thread ID is
            // initialized, so cover it here in case it wasn't already done
            if (currentThreadID != Lock.UninitializedThreadId && lck.TryEnterOneShot(currentThreadID))
                return true;

            return lck.TryEnterSlow(0, currentThreadID) != Lock.UninitializedThreadId;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryEnter(object obj, int millisecondsTimeout)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeout, -1);

            int currentThreadID = ManagedThreadId.CurrentManagedThreadIdUnchecked;
            int resultOrIndex = ObjectHeader.TryAcquire(obj, currentThreadID);
            if (resultOrIndex < 0)
                return true;

            Lock lck = resultOrIndex == 0 ?
                ObjectHeader.GetLockObject(obj) :
                SyncTable.GetLockObject(resultOrIndex);

            // The one-shot fast path is not covered by the slow path below for a zero timeout when the thread ID is
            // initialized, so cover it here in case it wasn't already done
            if (millisecondsTimeout == 0 && currentThreadID != Lock.UninitializedThreadId && lck.TryEnterOneShot(currentThreadID))
                return true;

            return lck.TryEnterSlow(millisecondsTimeout, currentThreadID) != Lock.UninitializedThreadId;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Exit(object obj)
        {
            ObjectHeader.Release(obj);
        }

        // Marked no-inlining to prevent recursive inlining of IsAcquired.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool IsEntered(object obj)
        {
            return ObjectHeader.IsAcquired(obj);
        }
        #endregion

        private static void SynchronizedMethodEnter(object obj, ref bool lockTaken)
        {
            // Inlined Monitor.Enter with a few tweaks
            int currentThreadID = ManagedThreadId.CurrentManagedThreadIdUnchecked;
            int resultOrIndex = ObjectHeader.Acquire(obj, currentThreadID);
            if (resultOrIndex < 0)
            {
                lockTaken = true;
                return;
            }

            Lock lck = resultOrIndex == 0 ?
                ObjectHeader.GetLockObject(obj) :
                SyncTable.GetLockObject(resultOrIndex);

            lck.TryEnterSlow(Timeout.Infinite, currentThreadID);
            lockTaken = true;
        }

        private static void SynchronizedMethodExit(object obj, ref bool lockTaken)
        {
            // Inlined Monitor.Exit with a few tweaks
            if (!lockTaken)
                return;

            ObjectHeader.Release(obj);
            lockTaken = false;
        }
    }
}
