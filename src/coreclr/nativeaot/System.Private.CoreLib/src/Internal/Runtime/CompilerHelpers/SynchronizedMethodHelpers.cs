// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// Set of helpers used to implement synchronized methods.
    /// </summary>
    internal static class SynchronizedMethodHelpers
    {
        private static void MonitorEnter(object obj, ref bool lockTaken)
        {
            // Inlined Monitor.Enter with a few tweaks
            int resultOrIndex = ObjectHeader.Acquire(obj);
            if (resultOrIndex < 0)
            {
                lockTaken = true;
                return;
            }

            Lock lck = resultOrIndex == 0 ?
                ObjectHeader.GetLockObject(obj) :
                SyncTable.GetLockObject(resultOrIndex);

            if (lck.TryAcquire(0))
            {
                lockTaken = true;
                return;
            }

            Monitor.TryAcquireContended(lck, obj, Timeout.Infinite);
            lockTaken = true;
        }
        private static void MonitorExit(object obj, ref bool lockTaken)
        {
            // Inlined Monitor.Exit with a few tweaks
            if (!lockTaken)
                return;

            ObjectHeader.Release(obj);
            lockTaken = false;
        }

        private static unsafe void MonitorEnterStatic(MethodTable* pMT, ref bool lockTaken)
        {
            // Inlined Monitor.Enter with a few tweaks
            object obj = GetStaticLockObject(pMT);
            int resultOrIndex = ObjectHeader.Acquire(obj);
            if (resultOrIndex < 0)
            {
                lockTaken = true;
                return;
            }

            Lock lck = resultOrIndex == 0 ?
                ObjectHeader.GetLockObject(obj) :
                SyncTable.GetLockObject(resultOrIndex);

            if (lck.TryAcquire(0))
            {
                lockTaken = true;
                return;
            }

            Monitor.TryAcquireContended(lck, obj, Timeout.Infinite);
            lockTaken = true;
        }
        private static unsafe void MonitorExitStatic(MethodTable* pMT, ref bool lockTaken)
        {
            // Inlined Monitor.Exit with a few tweaks
            if (!lockTaken)
                return;

            object obj = GetStaticLockObject(pMT);
            ObjectHeader.Release(obj);
            lockTaken = false;
        }

        private static unsafe Type GetStaticLockObject(MethodTable* pMT)
        {
            return Type.GetTypeFromMethodTable(pMT);
        }
    }
}
