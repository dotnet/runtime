// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    public static partial class Monitor
    {
#if FEATURE_WASM_THREADS
        [ThreadStatic]
        public static bool ThrowOnBlockingWaitOnJSInteropThread;
#endif

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)] // Interpreter is missing this intrinsic
        public static void Enter(object obj) => Enter(obj);

        [Intrinsic]
        public static void Enter(object obj, ref bool lockTaken)
        {
            // TODO: Interpreter is missing this intrinsic
            if (lockTaken)
                throw new ArgumentException(SR.Argument_MustBeFalse, nameof(lockTaken));

            ReliableEnterTimeout(obj, (int)Timeout.Infinite, ref lockTaken);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void InternalExit(object obj);

        public static void Exit(object obj)
        {
            if (obj == null)
                ArgumentNullException.ThrowIfNull(obj);
            if (ObjectHeader.TryExitChecked(obj))
                return;

            InternalExit(obj);
        }

        public static bool TryEnter(object obj)
        {
            bool lockTaken = false;
            TryEnter(obj, 0, ref lockTaken);
            return lockTaken;
        }

        public static void TryEnter(object obj, ref bool lockTaken)
        {
            if (lockTaken)
                throw new ArgumentException(SR.Argument_MustBeFalse, nameof(lockTaken));

            ReliableEnterTimeout(obj, 0, ref lockTaken);
        }

        public static bool TryEnter(object obj, int millisecondsTimeout)
        {
            bool lockTaken = false;
            TryEnter(obj, millisecondsTimeout, ref lockTaken);
            return lockTaken;
        }

        public static void TryEnter(object obj, int millisecondsTimeout, ref bool lockTaken)
        {
            if (lockTaken)
                throw new ArgumentException(SR.Argument_MustBeFalse, nameof(lockTaken));
            ReliableEnterTimeout(obj, millisecondsTimeout, ref lockTaken);
        }

        public static bool IsEntered(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);
            return ObjectHeader.IsEntered(obj);
        }

#if !FEATURE_WASM_THREADS
        [UnsupportedOSPlatform("browser")]
#endif
        public static bool Wait(object obj, int millisecondsTimeout)
        {
            ArgumentNullException.ThrowIfNull(obj);
#if FEATURE_WASM_THREADS
            if (ThrowOnBlockingWaitOnJSInteropThread)
            {
                throw new PlatformNotSupportedException("blocking Wait is not supported on the JS interop threads.");
            }
#endif
            return ObjWait(millisecondsTimeout, obj);
        }

        public static void Pulse(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);
            ObjPulse(obj);
        }

        public static void PulseAll(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);
            ObjPulseAll(obj);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void Monitor_pulse(object obj);

        private static void ObjPulse(object obj)
        {
            if (!ObjectHeader.HasOwner(obj))
                throw new SynchronizationLockException();

            Monitor_pulse(obj);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void Monitor_pulse_all(object obj);

        private static void ObjPulseAll(object obj)
        {
            if (!ObjectHeader.HasOwner(obj))
                throw new SynchronizationLockException();

            Monitor_pulse_all(obj);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool Monitor_wait(object obj, int ms, bool allowInterruption);

        private static bool ObjWait(int millisecondsTimeout, object obj)
        {
            if (millisecondsTimeout < 0 && millisecondsTimeout != (int)Timeout.Infinite)
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout));
            if (!ObjectHeader.HasOwner(obj))
                throw new SynchronizationLockException();

            bool sendWaitEvents =
                millisecondsTimeout != 0 &&
                NativeRuntimeEventSource.Log.IsEnabled(EventLevel.Verbose, NativeRuntimeEventSource.Keywords.WaitHandleKeyword);
            if (sendWaitEvents)
            {
                NativeRuntimeEventSource.Log.WaitHandleWaitStart(NativeRuntimeEventSource.WaitHandleWaitSourceMap.MonitorWait, obj);
            }

            bool result = Monitor_wait(obj, millisecondsTimeout, true);

            if (sendWaitEvents)
            {
                NativeRuntimeEventSource.Log.WaitHandleWaitStop();
            }

            return result;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void try_enter_with_atomic_var(object obj, int millisecondsTimeout, bool allowInterruption, ref bool lockTaken);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReliableEnterTimeout(object obj, int timeout, ref bool lockTaken)
        {
            if (obj == null)
                ArgumentNullException.ThrowIfNull(obj);

            if (timeout < 0 && timeout != (int)Timeout.Infinite)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            // fast path
            if (ObjectHeader.TryEnterFast(obj)) {
                lockTaken = true;
                return;
            }

            try_enter_with_atomic_var(obj, timeout, true, ref lockTaken);
        }

        public static long LockContentionCount => Monitor_get_lock_contention_count() + Lock.ContentionCount;

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern long Monitor_get_lock_contention_count();
    }
}
