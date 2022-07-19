// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
/*=============================================================================
**
**
**
** Purpose: Synchronizes access to a shared resource or region of code in a multi-threaded
**             program.
**
**
=============================================================================*/

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    public static partial class Monitor
    {
        /*=========================================================================
        ** Obtain the monitor lock of obj. Will block if another thread holds the lock
        ** Will not block if the current thread holds the lock,
        ** however the caller must ensure that the same number of Exit
        ** calls are made as there were Enter calls.
        **
        ** Exceptions: ArgumentNullException if object is null.
        =========================================================================*/
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void Enter(object obj);


        // Use a ref bool instead of out to ensure that unverifiable code must
        // initialize this value to something.  If we used out, the value
        // could be uninitialized if we threw an exception in our prolog.
        // The JIT should inline this method to allow check of lockTaken argument to be optimized out
        // in the typical case. Note that the method has to be transparent for inlining to be allowed by the VM.
        public static void Enter(object obj, ref bool lockTaken)
        {
            if (lockTaken)
                ThrowLockTakenException();

            ReliableEnter(obj, ref lockTaken);
            Debug.Assert(lockTaken);
        }

        [DoesNotReturn]
        private static void ThrowLockTakenException()
        {
            throw new ArgumentException(SR.Argument_MustBeFalse, "lockTaken");
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void ReliableEnter(object obj, ref bool lockTaken);



        /*=========================================================================
        ** Release the monitor lock. If one or more threads are waiting to acquire the
        ** lock, and the current thread has executed as many Exits as
        ** Enters, one of the threads will be unblocked and allowed to proceed.
        **
        ** Exceptions: ArgumentNullException if object is null.
        **             SynchronizationLockException if the current thread does not
        **             own the lock.
        =========================================================================*/
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void Exit(object obj);

        /*=========================================================================
        ** Similar to Enter, but will never block. That is, if the current thread can
        ** acquire the monitor lock without blocking, it will do so and TRUE will
        ** be returned. Otherwise FALSE will be returned.
        **
        ** Exceptions: ArgumentNullException if object is null.
        =========================================================================*/
        public static bool TryEnter(object obj)
        {
            bool lockTaken = false;
            TryEnter(obj, 0, ref lockTaken);
            return lockTaken;
        }

        // The JIT should inline this method to allow check of lockTaken argument to be optimized out
        // in the typical case. Note that the method has to be transparent for inlining to be allowed by the VM.
        public static void TryEnter(object obj, ref bool lockTaken)
        {
            if (lockTaken)
                ThrowLockTakenException();

            ReliableEnterTimeout(obj, 0, ref lockTaken);
        }

        /*=========================================================================
        ** Version of TryEnter that will block, but only up to a timeout period
        ** expressed in milliseconds. If timeout == Timeout.Infinite the method
        ** becomes equivalent to Enter.
        **
        ** Exceptions: ArgumentNullException if object is null.
        **             ArgumentException if timeout < -1 (Timeout.Infinite).
        =========================================================================*/
        // The JIT should inline this method to allow check of lockTaken argument to be optimized out
        // in the typical case. Note that the method has to be transparent for inlining to be allowed by the VM.
        public static bool TryEnter(object obj, int millisecondsTimeout)
        {
            bool lockTaken = false;
            TryEnter(obj, millisecondsTimeout, ref lockTaken);
            return lockTaken;
        }

        // The JIT should inline this method to allow check of lockTaken argument to be optimized out
        // in the typical case. Note that the method has to be transparent for inlining to be allowed by the VM.
        public static void TryEnter(object obj, int millisecondsTimeout, ref bool lockTaken)
        {
            if (lockTaken)
                ThrowLockTakenException();

            ReliableEnterTimeout(obj, millisecondsTimeout, ref lockTaken);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void ReliableEnterTimeout(object obj, int timeout, ref bool lockTaken);

        public static bool IsEntered(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            return IsEnteredNative(obj);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool IsEnteredNative(object obj);

        /*========================================================================
    ** Waits for notification from the object (via a Pulse/PulseAll).
    ** timeout indicates how long to wait before the method returns.
    ** This method acquires the monitor waithandle for the object
    ** If this thread holds the monitor lock for the object, it releases it.
    ** On exit from the method, it obtains the monitor lock back.
    **
        ** Exceptions: ArgumentNullException if object is null.
    ========================================================================*/
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool ObjWait(int millisecondsTimeout, object obj);

        [UnsupportedOSPlatform("browser")]
        public static bool Wait(object obj, int millisecondsTimeout)
        {
            if (obj == null)
                throw (new ArgumentNullException(nameof(obj)));
            if (millisecondsTimeout < -1)
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);

            return ObjWait(millisecondsTimeout, obj);
        }

        /*========================================================================
        ** Sends a notification to a single waiting object.
        * Exceptions: SynchronizationLockException if this method is not called inside
        * a synchronized block of code.
        ========================================================================*/
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void ObjPulse(object obj);

        public static void Pulse(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            ObjPulse(obj);
        }
        /*========================================================================
        ** Sends a notification to all waiting objects.
        ========================================================================*/
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void ObjPulseAll(object obj);

        public static void PulseAll(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            ObjPulseAll(obj);
        }

        /// <summary>
        /// Gets the number of times there was contention upon trying to take a <see cref="Monitor"/>'s lock so far.
        /// </summary>
        public static long LockContentionCount => GetLockContentionCount();

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ObjectNative_GetMonitorLockContentionCount")]
        private static partial long GetLockContentionCount();
    }
}
