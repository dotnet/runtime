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

namespace System.Threading
{
    public static partial class Monitor
    {
        #region Object->Lock/Condition mapping

        private static readonly ConditionalWeakTable<object, Condition> s_conditionTable = [];
        private static readonly Func<object, Condition> s_createCondition = (o) => new Condition(SyncTable.GetLockObject(o));

        private static Condition GetCondition(object obj)
        {
            Debug.Assert(
                obj is not Condition,
                "Do not use Monitor.Pulse or Wait on a Condition instance; use the methods on Condition instead.");
            return s_conditionTable.GetOrAdd(obj, s_createCondition);
        }
        #endregion

        #region Public Enter/Exit methods
        public static void Enter(object obj)
        {
            ObjectHeader.AcquireHeaderResult result = ObjectHeader.TryAcquireThinLock(obj);
            if (result == ObjectHeader.AcquireHeaderResult.Success)
                return;

            EnterSlow(obj);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void EnterSlow(object obj)
        {
            Lock lck = SyncTable.GetLockObject(obj);

            lck.Enter();
        }

        public static void Enter(object obj, ref bool lockTaken)
        {
            // we are inlining lockTaken check as the check is likely be optimized away
            if (lockTaken)
                throw new ArgumentException(SR.Argument_MustBeFalse, nameof(lockTaken));

            Enter(obj);
            lockTaken = true;
        }

        public static bool TryEnter(object obj)
        {
            ObjectHeader.AcquireHeaderResult result = ObjectHeader.TryAcquireThinLock(obj);
            if (result == ObjectHeader.AcquireHeaderResult.Success)
                return true;

            if (result == ObjectHeader.AcquireHeaderResult.Contention)
                return false;

            return TryEnterSlow(obj);
        }

        private static bool TryEnterSlow(object obj)
        {
            Lock lck = SyncTable.GetLockObject(obj);
            return lck.TryEnter();
        }

        public static void TryEnter(object obj, ref bool lockTaken)
        {
            // we are inlining lockTaken check as the check is likely be optimized away
            if (lockTaken)
                throw new ArgumentException(SR.Argument_MustBeFalse, nameof(lockTaken));

            lockTaken = TryEnter(obj);
        }

        public static bool TryEnter(object obj, int millisecondsTimeout)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeout, -1);

            ObjectHeader.AcquireHeaderResult result = ObjectHeader.TryAcquireThinLock(obj);
            if (result == ObjectHeader.AcquireHeaderResult.Success)
                return true;

            if (result == ObjectHeader.AcquireHeaderResult.Contention)
                return false;

            return TryEnterSlow(obj, millisecondsTimeout);
        }

        private static bool TryEnterSlow(object obj, int millisecondsTimeout)
        {
            Lock lck = SyncTable.GetLockObject(obj);
            return lck.TryEnter(millisecondsTimeout);
        }

        public static void TryEnter(object obj, int millisecondsTimeout, ref bool lockTaken)
        {
            // we are inlining lockTaken check as the check is likely be optimized away
            if (lockTaken)
                throw new ArgumentException(SR.Argument_MustBeFalse, nameof(lockTaken));

            lockTaken = TryEnter(obj, millisecondsTimeout);
        }

        internal static void ExitIfLockTaken(object obj, ref bool lockTaken)
        {
            if (!lockTaken)
                return;

            Exit(obj);
            lockTaken = false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Exit(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);
            ObjectHeader.ReleaseHeaderResult result = ObjectHeader.Release(obj);

            if (result == ObjectHeader.ReleaseHeaderResult.Success)
            {
                return;
            }

            if (result == ObjectHeader.ReleaseHeaderResult.Error)
            {
                throw new SynchronizationLockException();
            }

            ExitSlow(obj);
        }

        private static void ExitSlow(object obj)
        {
            Lock lck = SyncTable.GetLockObject(obj);
            lck.Exit();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool IsEntered(object obj)
        {
            return ObjectHeader.IsAcquired(obj);
        }

        #endregion

        #region Public Wait/Pulse methods

        [UnsupportedOSPlatform("browser")]
        public static bool Wait(object obj, int millisecondsTimeout)
        {
            return GetCondition(obj).Wait(millisecondsTimeout, obj);
        }

        public static void Pulse(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            GetCondition(obj).SignalOne();
        }

        public static void PulseAll(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            GetCondition(obj).SignalAll();
        }

        #endregion

        #region Metrics

        /// <summary>
        /// Gets the number of times there was contention upon trying to take a <see cref="Monitor"/>'s lock so far.
        /// </summary>
        public static long LockContentionCount => Lock.ContentionCount;

        #endregion
    }
}
