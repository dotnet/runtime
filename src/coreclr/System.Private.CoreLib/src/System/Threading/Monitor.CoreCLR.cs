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
        public static void Enter(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj, null);

            if (!TryEnter_FastPath(obj))
            {
                Enter_Slowpath(obj);
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool TryEnter_FastPath(object obj);

        // These must match the values in syncblk.h
        private enum EnterHelperResult
        {
            Contention = 0,
            Entered = 1,
            UseSlowPath = 2
        }

        // These must match the values in syncblk.h
        private enum LeaveHelperAction
        {
            None = 0,
            Signal = 1,
            Yield = 2,
            Contention = 3,
            Error = 4,
        };

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern EnterHelperResult TryEnter_FastPath_WithTimeout(object obj, int timeout);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Monitor_Enter_Slowpath")]
        private static partial void Enter_Slowpath(ObjectHandleOnStack obj);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Enter_Slowpath(object obj)
        {
            Enter_Slowpath(ObjectHandleOnStack.Create(ref obj));
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Monitor_TryEnter_Slowpath")]
        private static partial int TryEnter_Slowpath(ObjectHandleOnStack obj, int timeout);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool TryEnter_Slowpath(object obj)
        {
            if (TryEnter_Slowpath(ObjectHandleOnStack.Create(ref obj), 0) != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool TryEnter_Slowpath(object obj, int timeout)
        {
            if (TryEnter_Slowpath(ObjectHandleOnStack.Create(ref obj), timeout) != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // Use a ref bool instead of out to ensure that unverifiable code must
        // initialize this value to something.  If we used out, the value
        // could be uninitialized if we threw an exception in our prolog.
        // The JIT should inline this method to allow check of lockTaken argument to be optimized out
        // in the typical case. Note that the method has to be transparent for inlining to be allowed by the VM.
        public static void Enter(object obj, ref bool lockTaken)
        {
            if (lockTaken)
                ThrowLockTakenException();

            ArgumentNullException.ThrowIfNull(obj, null);

            if (!TryEnter_FastPath(obj))
            {
                Enter_Slowpath(obj);
            }
            lockTaken = true;
            Debug.Assert(lockTaken);
        }

        [DoesNotReturn]
        private static void ThrowLockTakenException()
        {
            throw new ArgumentException(SR.Argument_MustBeFalse, "lockTaken");
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern LeaveHelperAction Exit_FastPath(object obj);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Monitor_Exit_Slowpath")]
        private static partial void Exit_Slowpath(ObjectHandleOnStack obj, LeaveHelperAction exitBehavior);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Exit_Slowpath(LeaveHelperAction exitBehavior, object obj)
        {
            Exit_Slowpath(ObjectHandleOnStack.Create(ref obj), exitBehavior);
        }

        /*=========================================================================
        ** Release the monitor lock. If one or more threads are waiting to acquire the
        ** lock, and the current thread has executed as many Exits as
        ** Enters, one of the threads will be unblocked and allowed to proceed.
        **
        ** Exceptions: ArgumentNullException if object is null.
        **             SynchronizationLockException if the current thread does not
        **             own the lock.
        =========================================================================*/
        public static void Exit(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj, null);

            LeaveHelperAction exitBehavior = Exit_FastPath(obj);

            if (exitBehavior == LeaveHelperAction.None)
                return;

            Exit_Slowpath(exitBehavior, obj);
        }

        // Used to implement synchronized methods on non Windows-X86 architectures
        internal static void ExitIfLockTaken(object obj, ref bool lockTaken)
        {
            ArgumentNullException.ThrowIfNull(obj, null);

            if (lockTaken)
            {
                LeaveHelperAction exitBehavior = Exit_FastPath(obj);

                if (exitBehavior == LeaveHelperAction.None)
                {
                    lockTaken = false;
                    return;
                }

                Exit_Slowpath(exitBehavior, obj);
                lockTaken = false;
            }
        }

        /*=========================================================================
        ** Similar to Enter, but will never block. That is, if the current thread can
        ** acquire the monitor lock without blocking, it will do so and TRUE will
        ** be returned. Otherwise FALSE will be returned.
        **
        ** Exceptions: ArgumentNullException if object is null.
        =========================================================================*/
        public static bool TryEnter(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj, null);

            EnterHelperResult tryEnterResult = TryEnter_FastPath_WithTimeout(obj, 0);
            if (tryEnterResult == EnterHelperResult.Entered)
            {
                return true;
            }
            else if (tryEnterResult == EnterHelperResult.Contention)
            {
                return false;
            }

            return TryEnter_Slowpath(obj);
        }

        private static void TryEnter_Timeout_WithLockTaken(object obj, int millisecondsTimeout, ref bool lockTaken)
        {
            if (millisecondsTimeout >= -1)
            {
                EnterHelperResult tryEnterResult = TryEnter_FastPath_WithTimeout(obj, millisecondsTimeout);
                if (tryEnterResult == EnterHelperResult.Entered)
                {
                    lockTaken = true;
                    return;
                }
                else if (millisecondsTimeout == 0 && (tryEnterResult == EnterHelperResult.Contention))
                {
                    return;
                }
            }

            if (TryEnter_Slowpath(obj, millisecondsTimeout))
            {
                lockTaken = true;
            }
        }

        // The JIT should inline this method to allow check of lockTaken argument to be optimized out
        // in the typical case. Note that the method has to be transparent for inlining to be allowed by the VM.
        public static void TryEnter(object obj, ref bool lockTaken)
        {
            if (lockTaken)
                ThrowLockTakenException();

            ArgumentNullException.ThrowIfNull(obj, null);

            TryEnter_Timeout_WithLockTaken(obj, 0, ref lockTaken);
        }

        /*=========================================================================
        ** Version of TryEnter that will block, but only up to a timeout period
        ** expressed in milliseconds. If timeout == Timeout.Infinite the method
        ** becomes equivalent to Enter.
        **
        ** Exceptions: ArgumentNullException if object is null.
        **             ArgumentException if timeout < -1 (Timeout.Infinite).
        =========================================================================*/
        public static bool TryEnter(object obj, int millisecondsTimeout)
        {
            ArgumentNullException.ThrowIfNull(obj, null);

            if (millisecondsTimeout >= -1)
            {
                EnterHelperResult tryEnterResult = TryEnter_FastPath_WithTimeout(obj, millisecondsTimeout);
                if (tryEnterResult == EnterHelperResult.Entered)
                {
                    return true;
                }
                else if (millisecondsTimeout == 0 && (tryEnterResult == EnterHelperResult.Contention))
                {
                    return false;
                }
            }

            return TryEnter_Slowpath(obj, millisecondsTimeout);
        }

        // The JIT should inline this method to allow check of lockTaken argument to be optimized out
        // in the typical case. Note that the method has to be transparent for inlining to be allowed by the VM.
        public static void TryEnter(object obj, int millisecondsTimeout, ref bool lockTaken)
        {
            if (lockTaken)
                ThrowLockTakenException();

            ArgumentNullException.ThrowIfNull(obj, null);

            TryEnter_Timeout_WithLockTaken(obj, millisecondsTimeout, ref lockTaken);
        }

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
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Monitor_Wait")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool Wait(ObjectHandleOnStack obj, int millisecondsTimeout);

        [UnsupportedOSPlatform("browser")]
        public static bool Wait(object obj, int millisecondsTimeout)
        {
            ArgumentNullException.ThrowIfNull(obj);
            ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeout, -1);

            return Wait(ObjectHandleOnStack.Create(ref obj), millisecondsTimeout);
        }

        /*========================================================================
        ** Sends a notification to a single waiting object.
        * Exceptions: SynchronizationLockException if this method is not called inside
        * a synchronized block of code.
        ========================================================================*/
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Monitor_Pulse")]
        private static partial void Pulse(ObjectHandleOnStack obj);

        public static void Pulse(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            Pulse(ObjectHandleOnStack.Create(ref obj));
        }
        /*========================================================================
        ** Sends a notification to all waiting objects.
        ========================================================================*/
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Monitor_PulseAll")]
        private static partial void PulseAll(ObjectHandleOnStack obj);

        public static void PulseAll(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            PulseAll(ObjectHandleOnStack.Create(ref obj));
        }

        /// <summary>
        /// Gets the number of times there was contention upon trying to take a <see cref="Monitor"/>'s lock so far.
        /// </summary>
        public static long LockContentionCount => GetLockContentionCount() + Lock.ContentionCount;

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Monitor_GetLockContentionCount")]
        private static partial long GetLockContentionCount();
    }
}
