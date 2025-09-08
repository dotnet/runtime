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
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    public static partial class Monitor
    {
        #region Object->Lock mapping
        internal static Lock GetLockObject(object obj)
        {
            IntPtr lockHandle = GetLockHandleIfExists(obj);
            if (lockHandle != 0)
            {
                return GCHandle<Lock>.FromIntPtr(lockHandle).Target;
            }

            return GetLockObjectFallback(obj);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static Lock GetLockObjectFallback(object obj)
            {
#pragma warning disable CS9216 // A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
                object lockObj = new Lock();
#pragma warning restore CS9216
                GetOrCreateLockObject(ObjectHandleOnStack.Create(ref obj), ObjectHandleOnStack.Create(ref lockObj));
                return (Lock)lockObj!;
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr GetLockHandleIfExists(object obj);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Monitor_GetOrCreateLockObject")]
        private static partial void GetOrCreateLockObject(ObjectHandleOnStack obj, ObjectHandleOnStack lockObj);

        #endregion

        #region Object->Condition mapping

        private static readonly ConditionalWeakTable<object, Condition> s_conditionTable = [];
        private static readonly Func<object, Condition> s_createCondition = (o) => new Condition(GetLockObject(o));

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
            ObjectHeader.HeaderLockResult result = ObjectHeader.TryAcquireThinLock(obj);
            if (result == ObjectHeader.HeaderLockResult.Success)
                return;

            GetLockObject(obj).Enter();
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
            ObjectHeader.HeaderLockResult result = ObjectHeader.TryAcquireThinLock(obj);
            if (result == ObjectHeader.HeaderLockResult.Success)
                return true;

            if (result == ObjectHeader.HeaderLockResult.Failure)
                return false;

            return GetLockObject(obj).TryEnter();
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

            ObjectHeader.HeaderLockResult result = ObjectHeader.TryAcquireThinLock(obj);
            if (result == ObjectHeader.HeaderLockResult.Success)
                return true;

            return GetLockObject(obj).TryEnter(millisecondsTimeout);
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
            ObjectHeader.HeaderLockResult result = ObjectHeader.Release(obj);

            if (result == ObjectHeader.HeaderLockResult.Success)
            {
                return;
            }

            if (result == ObjectHeader.HeaderLockResult.Failure)
            {
                throw new SynchronizationLockException();
            }

            GetLockObject(obj).Exit();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool IsEntered(object obj)
        {
            ObjectHeader.HeaderLockResult result = ObjectHeader.IsAcquired(obj);
            if (result == ObjectHeader.HeaderLockResult.Success)
                return true;

            if (result == ObjectHeader.HeaderLockResult.Failure)
                return false;

            return GetLockObject(obj).IsHeldByCurrentThread;
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
