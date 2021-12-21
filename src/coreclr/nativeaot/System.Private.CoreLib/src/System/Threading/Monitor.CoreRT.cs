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

using Internal.Runtime.CompilerServices;

namespace System.Threading
{
    public static partial class Monitor
    {
        #region Object->Lock/Condition mapping

        private static ConditionalWeakTable<object, Condition> s_conditionTable = new ConditionalWeakTable<object, Condition>();
        private static ConditionalWeakTable<object, Condition>.CreateValueCallback s_createCondition = (o) => new Condition(GetLock(o));

        internal static Lock GetLock(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            Debug.Assert(!(obj is Lock),
                "Do not use Monitor.Enter or TryEnter on a Lock instance; use Lock methods directly instead.");

            return ObjectHeader.GetLockObject(obj);
        }

        private static Condition GetCondition(object obj)
        {
            Debug.Assert(
                !(obj is Condition || obj is Lock),
                "Do not use Monitor.Pulse or Wait on a Lock or Condition instance; use the methods on Condition instead.");
            return s_conditionTable.GetValue(obj, s_createCondition);
        }
        #endregion

        #region Public Enter/Exit methods

        public static void Enter(object obj)
        {
            Lock lck = GetLock(obj);
            if (lck.TryAcquire(0))
                return;
            TryAcquireContended(lck, obj, Timeout.Infinite);
        }

        public static void Enter(object obj, ref bool lockTaken)
        {
            if (lockTaken)
                throw new ArgumentException(SR.Argument_MustBeFalse, nameof(lockTaken));

            Lock lck = GetLock(obj);
            if (lck.TryAcquire(0))
            {
                lockTaken = true;
                return;
            }
            TryAcquireContended(lck, obj, Timeout.Infinite);
            lockTaken = true;
        }

        public static bool TryEnter(object obj)
        {
            return GetLock(obj).TryAcquire(0);
        }

        public static void TryEnter(object obj, ref bool lockTaken)
        {
            if (lockTaken)
                throw new ArgumentException(SR.Argument_MustBeFalse, nameof(lockTaken));

            lockTaken = GetLock(obj).TryAcquire(0);
        }

        public static bool TryEnter(object obj, int millisecondsTimeout)
        {
            if (millisecondsTimeout < -1)
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);

            Lock lck = GetLock(obj);
            if (lck.TryAcquire(0))
                return true;
            return TryAcquireContended(lck, obj, millisecondsTimeout);
        }

        public static void TryEnter(object obj, int millisecondsTimeout, ref bool lockTaken)
        {
            if (lockTaken)
                throw new ArgumentException(SR.Argument_MustBeFalse, nameof(lockTaken));
            if (millisecondsTimeout < -1)
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);

            Lock lck = GetLock(obj);
            if (lck.TryAcquire(0))
            {
                lockTaken = true;
                return;
            }
            lockTaken = TryAcquireContended(lck, obj, millisecondsTimeout);
        }

        public static void Exit(object obj)
        {
            GetLock(obj).Release();
        }

        public static bool IsEntered(object obj)
        {
            return GetLock(obj).IsAcquired;
        }

        #endregion

        #region Public Wait/Pulse methods

        [UnsupportedOSPlatform("browser")]
        public static bool Wait(object obj, int millisecondsTimeout)
        {
            Condition condition = GetCondition(obj);
            DebugBlockingItem blockingItem;

            using (new DebugBlockingScope(obj, DebugBlockingItemType.MonitorEvent, millisecondsTimeout, out blockingItem))
            {
                return condition.Wait(millisecondsTimeout);
            }
        }

        public static void Pulse(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            GetCondition(obj).SignalOne();
        }

        public static void PulseAll(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            GetCondition(obj).SignalAll();
        }

        #endregion

        #region Slow path for Entry/TryEnter methods.

        internal static bool TryAcquireContended(Lock lck, object obj, int millisecondsTimeout)
        {
            DebugBlockingItem blockingItem;

            using (new DebugBlockingScope(obj, DebugBlockingItemType.MonitorCriticalSection, millisecondsTimeout, out blockingItem))
            {
                return lck.TryAcquire(millisecondsTimeout, trackContentions: true);
            }
        }

        #endregion

        #region Debugger support

        // The debugger binds to the fields below by name. Do not change any names or types without
        // updating the debugger!

        // The head of the list of DebugBlockingItem stack objects used by the debugger to implement
        // ICorDebugThread4::GetBlockingObjects. Usually the list either is empty or contains a single
        // item. However, a wait on an STA thread may reenter via the message pump and cause the thread
        // to be blocked on a second object.
        [ThreadStatic]
        private static IntPtr t_firstBlockingItem;

        // Different ways a thread can be blocked that the debugger will expose.
        // Do not change or add members without updating the debugger code.
        private enum DebugBlockingItemType
        {
            MonitorCriticalSection = 0,
            MonitorEvent = 1
        }

        // Represents an item a thread is blocked on. This structure is allocated on the stack and accessed by the debugger.
        // Fields are volatile to avoid potential compiler optimizations.
        private struct DebugBlockingItem
        {
            // The object the thread is waiting on
            public volatile object _object;

            // Indicates how the thread is blocked on the item
            public volatile DebugBlockingItemType _blockingType;

            // Blocking timeout in milliseconds or Timeout.Infinite for no timeout
            public volatile int _timeout;

            // Next pointer in the linked list of DebugBlockingItem records
            public volatile IntPtr _next;
        }

        private unsafe struct DebugBlockingScope : IDisposable
        {
            public DebugBlockingScope(object obj, DebugBlockingItemType blockingType, int timeout, out DebugBlockingItem blockingItem)
            {
                blockingItem._object = obj;
                blockingItem._blockingType = blockingType;
                blockingItem._timeout = timeout;
                blockingItem._next = t_firstBlockingItem;

                t_firstBlockingItem = (IntPtr)Unsafe.AsPointer(ref blockingItem);
            }

            public void Dispose()
            {
                t_firstBlockingItem = Unsafe.Read<DebugBlockingItem>((void*)t_firstBlockingItem)._next;
            }
        }

        #endregion

        #region Metrics

        private static readonly ThreadInt64PersistentCounter s_lockContentionCounter = new ThreadInt64PersistentCounter();

        [ThreadStatic]
        private static object t_ContentionCountObject;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static object CreateThreadLocalContentionCountObject()
        {
            Debug.Assert(t_ContentionCountObject == null);

            object threadLocalContentionCountObject = s_lockContentionCounter.CreateThreadLocalCountObject();
            t_ContentionCountObject = threadLocalContentionCountObject;
            return threadLocalContentionCountObject;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void IncrementLockContentionCount() => ThreadInt64PersistentCounter.Increment(t_ContentionCountObject ?? CreateThreadLocalContentionCountObject());

        /// <summary>
        /// Gets the number of times there was contention upon trying to take a <see cref="Monitor"/>'s lock so far.
        /// </summary>
        public static long LockContentionCount => s_lockContentionCounter.Count;

        #endregion
    }
}
