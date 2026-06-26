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
            ObjectHeader.AcquireThinLock(obj);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryEnter(object obj)
        {
            return ObjectHeader.TryAcquireThinLock(obj);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryEnter(object obj, int millisecondsTimeout)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeout, -1);
            return ObjectHeader.TryAcquireThinLock(obj, millisecondsTimeout);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Exit(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);
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
            ObjectHeader.AcquireThinLock(obj);
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
