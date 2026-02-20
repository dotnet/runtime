// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    public static partial class Monitor
    {
        public static bool TryEnter(object obj, TimeSpan timeout)
            => TryEnter(obj, WaitHandle.ToTimeoutMilliseconds(timeout));

        public static void TryEnter(object obj, TimeSpan timeout, ref bool lockTaken)
            => TryEnter(obj, WaitHandle.ToTimeoutMilliseconds(timeout), ref lockTaken);

#if !FEATURE_WASM_MANAGED_THREADS
        [UnsupportedOSPlatform("browser")]
#endif
        public static bool Wait(object obj, TimeSpan timeout) => Wait(obj, WaitHandle.ToTimeoutMilliseconds(timeout));

#if !FEATURE_WASM_MANAGED_THREADS
        [UnsupportedOSPlatform("browser")]
#endif
        public static bool Wait(object obj) => Wait(obj, Timeout.Infinite);

        // Remoting is not supported, exitContext argument is unused
#if !FEATURE_WASM_MANAGED_THREADS
        [UnsupportedOSPlatform("browser")]
#endif
        public static bool Wait(object obj, int millisecondsTimeout, bool exitContext)
            => Wait(obj, millisecondsTimeout);

        // Remoting is not supported, exitContext argument is unused
#if !FEATURE_WASM_MANAGED_THREADS
        [UnsupportedOSPlatform("browser")]
#endif
        public static bool Wait(object obj, TimeSpan timeout, bool exitContext)
            => Wait(obj, WaitHandle.ToTimeoutMilliseconds(timeout));

#if !MONO
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Enter(object obj, ref bool lockTaken)
        {
            // Aggressively inline lockTaken check as it is likely to be optimized away
            if (lockTaken)
                ThrowLockTakenException();

            Enter(obj);
            lockTaken = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TryEnter(object obj, ref bool lockTaken)
        {
            // Aggressively inline lockTaken check as it is likely to be optimized away
            if (lockTaken)
                ThrowLockTakenException();

            lockTaken = TryEnter(obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TryEnter(object obj, int millisecondsTimeout, ref bool lockTaken)
        {
            // Aggressively inline lockTaken check as it is likely to be optimized away
            if (lockTaken)
                ThrowLockTakenException();

            lockTaken = TryEnter(obj, millisecondsTimeout);
        }

        [DoesNotReturn]
        private static void ThrowLockTakenException()
        {
            throw new ArgumentException(SR.Argument_MustBeFalse, "lockTaken");
        }

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

        #region Public Wait/Pulse methods

        [UnsupportedOSPlatform("browser")]
        public static bool Wait(object obj, int millisecondsTimeout)
        {
            Thread.ThrowIfSingleThreaded();
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
#endif
    }
}
