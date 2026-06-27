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
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Enter(object obj)
        {
            ObjectHeader.AcquireThinLock(obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Enter(object obj, ref bool lockTaken)
        {
            // Aggressively inline lockTaken check as it is likely to be optimized away
            if (lockTaken)
                ThrowLockTakenException();

            Enter(obj);
            lockTaken = true;
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool IsEntered(object obj)
        {
            return ObjectHeader.IsAcquired(obj);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Exit(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);
            ObjectHeader.Release(obj);
        }

        [DoesNotReturn]
        private static void ThrowLockTakenException()
        {
            throw new ArgumentException(SR.Argument_MustBeFalse, "lockTaken");
        }

        private static void SynchronizedMethodEnter(object obj, ref bool lockTaken)
        {
            ObjectHeader.AcquireThinLock(obj);
            lockTaken = true;
        }

        private static void SynchronizedMethodExit(object obj, ref bool lockTaken)
        {
            // Inlined Monitor.Exit
            if (!lockTaken)
                return;

            ObjectHeader.Release(obj);
        }

        #region Public Wait/Pulse methods

        [UnsupportedOSPlatform("browser")]
        public static bool Wait(object obj, int millisecondsTimeout)
        {
            ArgumentNullException.ThrowIfNull(obj);
            RuntimeFeature.ThrowIfMultithreadingIsNotSupported();

            return GetLockObject(obj).Wait(millisecondsTimeout, obj);
        }

        public static void Pulse(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            GetLockObject(obj).Pulse();
        }

        public static void PulseAll(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            GetLockObject(obj).PulseAll();
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
