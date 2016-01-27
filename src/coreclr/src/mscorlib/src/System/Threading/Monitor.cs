// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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


namespace System.Threading {

    using System;
    using System.Security.Permissions;
    using System.Runtime;
    using System.Runtime.Remoting;
    using System.Threading;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    [HostProtection(Synchronization=true, ExternalThreading=true)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public static class Monitor 
    {
        /*=========================================================================
        ** Obtain the monitor lock of obj. Will block if another thread holds the lock
        ** Will not block if the current thread holds the lock,
        ** however the caller must ensure that the same number of Exit
        ** calls are made as there were Enter calls.
        **
        ** Exceptions: ArgumentNullException if object is null.
        =========================================================================*/
        [System.Security.SecuritySafeCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern void Enter(Object obj);


        // Use a ref bool instead of out to ensure that unverifiable code must
        // initialize this value to something.  If we used out, the value 
        // could be uninitialized if we threw an exception in our prolog.
        // The JIT should inline this method to allow check of lockTaken argument to be optimized out
        // in the typical case. Note that the method has to be transparent for inlining to be allowed by the VM.
        public static void Enter(Object obj, ref bool lockTaken)
        {
            if (lockTaken)
                ThrowLockTakenException();

            ReliableEnter(obj, ref lockTaken);
            Contract.Assert(lockTaken);
        }

        private static void ThrowLockTakenException()
        {
            throw new ArgumentException(Environment.GetResourceString("Argument_MustBeFalse"), "lockTaken");
        }

        [System.Security.SecuritySafeCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void ReliableEnter(Object obj, ref bool lockTaken);



        /*=========================================================================
        ** Release the monitor lock. If one or more threads are waiting to acquire the
        ** lock, and the current thread has executed as many Exits as
        ** Enters, one of the threads will be unblocked and allowed to proceed.
        **
        ** Exceptions: ArgumentNullException if object is null.
        **             SynchronizationLockException if the current thread does not
        **             own the lock.
        =========================================================================*/
        [System.Security.SecuritySafeCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static extern void Exit(Object obj);
    
        /*=========================================================================
        ** Similar to Enter, but will never block. That is, if the current thread can
        ** acquire the monitor lock without blocking, it will do so and TRUE will
        ** be returned. Otherwise FALSE will be returned.
        **
        ** Exceptions: ArgumentNullException if object is null.
        =========================================================================*/
        public static bool TryEnter(Object obj)
        {
            bool lockTaken = false;
            TryEnter(obj, 0, ref lockTaken);
            return lockTaken;
        }

        // The JIT should inline this method to allow check of lockTaken argument to be optimized out
        // in the typical case. Note that the method has to be transparent for inlining to be allowed by the VM.
        public static void TryEnter(Object obj, ref bool lockTaken)
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
        **             ArgumentException if timeout < 0.
        =========================================================================*/
        // The JIT should inline this method to allow check of lockTaken argument to be optimized out
        // in the typical case. Note that the method has to be transparent for inlining to be allowed by the VM.
        public static bool TryEnter(Object obj, int millisecondsTimeout)
        {
            bool lockTaken = false;
            TryEnter(obj, millisecondsTimeout, ref lockTaken);
            return lockTaken;
        }

        private static int MillisecondsTimeoutFromTimeSpan(TimeSpan timeout)
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (tm < -1 || tm > (long)Int32.MaxValue)
                throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            return (int)tm;
        }

        public static bool TryEnter(Object obj, TimeSpan timeout)
        {
            return TryEnter(obj, MillisecondsTimeoutFromTimeSpan(timeout));
        }

        // The JIT should inline this method to allow check of lockTaken argument to be optimized out
        // in the typical case. Note that the method has to be transparent for inlining to be allowed by the VM.
        public static void TryEnter(Object obj, int millisecondsTimeout, ref bool lockTaken)
        {
            if (lockTaken)
                ThrowLockTakenException();

            ReliableEnterTimeout(obj, millisecondsTimeout, ref lockTaken);
        }

        public static void TryEnter(Object obj, TimeSpan timeout, ref bool lockTaken)
        {
            if (lockTaken)
                ThrowLockTakenException();

            ReliableEnterTimeout(obj, MillisecondsTimeoutFromTimeSpan(timeout), ref lockTaken);
        }

        [System.Security.SecuritySafeCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void ReliableEnterTimeout(Object obj, int timeout, ref bool lockTaken);

        [System.Security.SecuritySafeCritical]
        public static bool IsEntered(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            return IsEnteredNative(obj);
        }

        [System.Security.SecurityCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool IsEnteredNative(Object obj);

        /*========================================================================
    ** Waits for notification from the object (via a Pulse/PulseAll). 
    ** timeout indicates how long to wait before the method returns.
    ** This method acquires the monitor waithandle for the object 
    ** If this thread holds the monitor lock for the object, it releases it. 
    ** On exit from the method, it obtains the monitor lock back. 
    ** If exitContext is true then the synchronization domain for the context 
    ** (if in a synchronized context) is exited before the wait and reacquired 
    **
        ** Exceptions: ArgumentNullException if object is null.
    ========================================================================*/
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool ObjWait(bool exitContext, int millisecondsTimeout, Object obj);

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static bool Wait(Object obj, int millisecondsTimeout, bool exitContext)
        {
            if (obj == null)
                throw (new ArgumentNullException("obj"));
            return ObjWait(exitContext, millisecondsTimeout, obj);
        }

        public static bool Wait(Object obj, TimeSpan timeout, bool exitContext)
        {
            return Wait(obj, MillisecondsTimeoutFromTimeSpan(timeout), exitContext);
        }

        public static bool Wait(Object obj, int millisecondsTimeout)
        {
            return Wait(obj, millisecondsTimeout, false);
        }

        public static bool Wait(Object obj, TimeSpan timeout)
        {
            return Wait(obj, MillisecondsTimeoutFromTimeSpan(timeout), false);
        }

        public static bool Wait(Object obj)
        {
            return Wait(obj, Timeout.Infinite, false);
        }

        /*========================================================================
        ** Sends a notification to a single waiting object. 
        * Exceptions: SynchronizationLockException if this method is not called inside
        * a synchronized block of code.
        ========================================================================*/
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void ObjPulse(Object obj);

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static void Pulse(Object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            Contract.EndContractBlock();

            ObjPulse(obj);
        }
        /*========================================================================
        ** Sends a notification to all waiting objects. 
        ========================================================================*/
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void ObjPulseAll(Object obj);

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static void PulseAll(Object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            Contract.EndContractBlock();

            ObjPulseAll(obj);
        }
    }
}
