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

        private static void MonitorEnterStatic(IntPtr pEEType, ref bool lockTaken)
        {
            // Inlined Monitor.Enter with a few tweaks
            object obj = GetStaticLockObject(pEEType);
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
        private static void MonitorExitStatic(IntPtr pEEType, ref bool lockTaken)
        {
            // Inlined Monitor.Exit with a few tweaks
            if (!lockTaken)
                return;

            object obj = GetStaticLockObject(pEEType);
            ObjectHeader.Release(obj);
            lockTaken = false;
        }

        private static Type GetStaticLockObject(IntPtr pEEType)
        {
            return Type.GetTypeFromEETypePtr(new EETypePtr(pEEType));
        }
    }
}
