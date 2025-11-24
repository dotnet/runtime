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
                Lock lockObj = GCHandle<Lock>.FromIntPtr(lockHandle).Target;
                GC.KeepAlive(obj);
                return lockObj;
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

        #region Public Enter/Exit methods
        public static void Enter(object obj)
        {
            ObjectHeader.HeaderLockResult result = ObjectHeader.TryAcquireThinLock(obj);
            if (result == ObjectHeader.HeaderLockResult.Success)
                return;

            GetLockObject(obj).Enter();
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

        public static bool TryEnter(object obj, int millisecondsTimeout)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeout, -1);

            ObjectHeader.HeaderLockResult result = ObjectHeader.TryAcquireThinLock(obj);
            if (result == ObjectHeader.HeaderLockResult.Success)
                return true;

            return GetLockObject(obj).TryEnter(millisecondsTimeout);
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

        // Marked no-inlining to prevent recursive inlining of IsAcquired.
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

        internal static void SynchronizedMethodEnter(object obj, ref bool lockTaken)
        {
            ObjectHeader.HeaderLockResult result = ObjectHeader.TryAcquireThinLock(obj);
            if (result == ObjectHeader.HeaderLockResult.Success)
            {
                lockTaken = true;
                return;
            }

            GetLockObject(obj).Enter();
            lockTaken = true;
        }

        internal static void SynchronizedMethodExit(object obj, ref bool lockTaken)
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

            GetLockObject(obj).Exit();
        }
    }
}
