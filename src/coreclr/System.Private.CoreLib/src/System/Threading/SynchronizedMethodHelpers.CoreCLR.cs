// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;


using Debug = System.Diagnostics.Debug;

namespace System.Runtime.CompilerHelpers
{
    /// <summary>
    /// Set of helpers used to implement synchronized methods.
    /// </summary>
    internal static class SynchronizedMethodHelpers
    {
        private static void MonitorEnter(object obj, ref bool lockTaken)
        {
            ObjectHeader.HeaderLockResult result = ObjectHeader.TryAcquireThinLock(obj);
            if (result == ObjectHeader.HeaderLockResult.Success)
            {
                lockTaken = true;
                return;
            }

            Monitor.GetLockObject(obj).Enter();
            lockTaken = true;
        }
        private static void MonitorExit(object obj, ref bool lockTaken)
        {
            // Inlined Monitor.Exit with a few tweaks
            if (!lockTaken)
                return;

            ObjectHeader.HeaderLockResult result = ObjectHeader.Release(obj);

            if (result == ObjectHeader.HeaderLockResult.Success)
            {
                return;
            }

            if (result == ObjectHeader.HeaderLockResult.Failure)
            {
                throw new SynchronizationLockException();
            }

            Monitor.GetLockObject(obj).Exit();
        }
    }
}
