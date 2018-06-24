// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
/*=============================================================================
**
**
**
** Purpose: Class to represent all synchronization objects in the runtime (that allow multiple wait)
**
**
=============================================================================*/

namespace System.Threading
{
    using System.Threading;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Microsoft.Win32.SafeHandles;
    using System.Runtime.Versioning;
    using System.Runtime.ConstrainedExecution;
    using System.Diagnostics.CodeAnalysis;
    using Win32Native = Microsoft.Win32.Win32Native;

    public abstract class WaitHandle : MarshalByRefObject, IDisposable
    {
        public const int WaitTimeout = 0x102;

        private const int MAX_WAITHANDLES = 64;

#pragma warning disable 414  // Field is not used from managed.
        private IntPtr waitHandle;  // !!! DO NOT MOVE THIS FIELD. (See defn of WAITHANDLEREF in object.h - has hard-coded access to this field.)
#pragma warning restore 414

        internal volatile SafeWaitHandle _waitHandle;

        internal bool hasThreadAffinity;

        private static IntPtr GetInvalidHandle()
        {
            return Win32Native.INVALID_HANDLE_VALUE;
        }
        protected static readonly IntPtr InvalidHandle = GetInvalidHandle();
        private const int WAIT_OBJECT_0 = 0;
        private const int WAIT_ABANDONED = 0x80;
        private const int WAIT_FAILED = 0x7FFFFFFF;
        private const int ERROR_TOO_MANY_POSTS = 0x12A;

        internal enum OpenExistingResult
        {
            Success,
            NameNotFound,
            PathNotFound,
            NameInvalid
        }

        protected WaitHandle()
        {
            Init();
        }

        private void Init()
        {
            _waitHandle = null;
            waitHandle = InvalidHandle;
            hasThreadAffinity = false;
        }


        [Obsolete("Use the SafeWaitHandle property instead.")]
        public virtual IntPtr Handle
        {
            get { return _waitHandle == null ? InvalidHandle : _waitHandle.DangerousGetHandle(); }
            set
            {
                if (value == InvalidHandle)
                {
                    // This line leaks a handle.  However, it's currently
                    // not perfectly clear what the right behavior is here 
                    // anyways.  This preserves Everett behavior.  We should 
                    // ideally do these things:
                    // *) Expose a settable SafeHandle property on WaitHandle.
                    // *) Expose a settable OwnsHandle property on SafeHandle.
                    if (_waitHandle != null)
                    {
                        _waitHandle.SetHandleAsInvalid();
                        _waitHandle = null;
                    }
                }
                else
                {
                    _waitHandle = new SafeWaitHandle(value, true);
                }
                waitHandle = value;
            }
        }

        public SafeWaitHandle SafeWaitHandle
        {
            get
            {
                if (_waitHandle == null)
                {
                    _waitHandle = new SafeWaitHandle(InvalidHandle, false);
                }
                return _waitHandle;
            }

            set
            {
                // Set safeWaitHandle and waitHandle in a CER so we won't take
                // a thread abort between the statements and leave the wait
                // handle in an invalid state. Note this routine is not thread
                // safe however.
                RuntimeHelpers.PrepareConstrainedRegions();
                try { }
                finally
                {
                    if (value == null)
                    {
                        _waitHandle = null;
                        waitHandle = InvalidHandle;
                    }
                    else
                    {
                        _waitHandle = value;
                        waitHandle = _waitHandle.DangerousGetHandle();
                    }
                }
            }
        }

        public virtual bool WaitOne(int millisecondsTimeout, bool exitContext)
        {
            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            }
            return WaitOne((long)millisecondsTimeout, exitContext);
        }

        public virtual bool WaitOne(TimeSpan timeout, bool exitContext)
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (-1 > tm || (long)int.MaxValue < tm)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            }
            return WaitOne(tm, exitContext);
        }

        public virtual bool WaitOne()
        {
            //Infinite Timeout
            return WaitOne(-1, false);
        }

        public virtual bool WaitOne(int millisecondsTimeout)
        {
            return WaitOne(millisecondsTimeout, false);
        }

        public virtual bool WaitOne(TimeSpan timeout)
        {
            return WaitOne(timeout, false);
        }

        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "Reviewed for thread-safety.")]
        private bool WaitOne(long timeout, bool exitContext)
        {
            return InternalWaitOne(_waitHandle, timeout, hasThreadAffinity, exitContext);
        }

        internal static bool InternalWaitOne(SafeHandle waitableSafeHandle, long millisecondsTimeout, bool hasThreadAffinity, bool exitContext)
        {
            if (waitableSafeHandle == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_Generic);
            }
            int ret = WaitOneNative(waitableSafeHandle, (uint)millisecondsTimeout, hasThreadAffinity, exitContext);

            if (ret == WAIT_ABANDONED)
            {
                ThrowAbandonedMutexException();
            }
            return (ret != WaitTimeout);
        }

        internal bool WaitOneWithoutFAS()
        {
            // version of waitone without fast application switch (FAS) support
            // This is required to support the Wait which FAS needs (otherwise recursive dependency comes in)
            if (_waitHandle == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_Generic);
            }

            long timeout = -1;
            int ret = WaitOneNative(_waitHandle, (uint)timeout, hasThreadAffinity, false);
            if (ret == WAIT_ABANDONED)
            {
                ThrowAbandonedMutexException();
            }
            return (ret != WaitTimeout);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int WaitOneNative(SafeHandle waitableSafeHandle, uint millisecondsTimeout, bool hasThreadAffinity, bool exitContext);

        /*========================================================================
        ** Waits for signal from all the objects. 
        ** timeout indicates how long to wait before the method returns.
        ** This method will return either when all the object have been pulsed
        ** or timeout milliseconds have elapsed.
        ** If exitContext is true then the synchronization domain for the context 
        ** (if in a synchronized context) is exited before the wait and reacquired 
        ========================================================================*/

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int WaitMultiple(WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext, bool WaitAll);

        public static bool WaitAll(WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext)
        {
            if (waitHandles == null)
            {
                throw new ArgumentNullException(nameof(waitHandles), SR.ArgumentNull_Waithandles);
            }
            if (waitHandles.Length == 0)
            {
                //
                // Some history: in CLR 1.0 and 1.1, we threw ArgumentException in this case, which was correct.
                // Somehow, in 2.0, this became ArgumentNullException.  This was not fixed until Silverlight 2,
                // which went back to ArgumentException.
                //
                // Now we're in a bit of a bind.  Backward-compatibility requires us to keep throwing ArgumentException
                // in CoreCLR, and ArgumentNullException in the desktop CLR.  This is ugly, but so is breaking
                // user code.
                //
                throw new ArgumentException(SR.Argument_EmptyWaithandleArray);
            }
            if (waitHandles.Length > MAX_WAITHANDLES)
            {
                throw new NotSupportedException(SR.NotSupported_MaxWaitHandles);
            }
            if (-1 > millisecondsTimeout)
            {
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            }
            WaitHandle[] internalWaitHandles = new WaitHandle[waitHandles.Length];
            for (int i = 0; i < waitHandles.Length; i++)
            {
                WaitHandle waitHandle = waitHandles[i];

                if (waitHandle == null)
                    throw new ArgumentNullException("waitHandles[" + i + "]", SR.ArgumentNull_ArrayElement);

                internalWaitHandles[i] = waitHandle;
            }
#if DEBUG
            // make sure we do not use waitHandles any more.
            waitHandles = null;
#endif

            int ret = WaitMultiple(internalWaitHandles, millisecondsTimeout, exitContext, true /* waitall*/ );

            if ((WAIT_ABANDONED <= ret) && (WAIT_ABANDONED + internalWaitHandles.Length > ret))
            {
                //In the case of WaitAll the OS will only provide the
                //    information that mutex was abandoned.
                //    It won't tell us which one.  So we can't set the Index or provide access to the Mutex
                ThrowAbandonedMutexException();
            }

            GC.KeepAlive(internalWaitHandles);
            return (ret != WaitTimeout);
        }

        public static bool WaitAll(
                                    WaitHandle[] waitHandles,
                                    TimeSpan timeout,
                                    bool exitContext)
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (-1 > tm || (long)int.MaxValue < tm)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            }
            return WaitAll(waitHandles, (int)tm, exitContext);
        }


        /*========================================================================
        ** Shorthand for WaitAll with timeout = Timeout.Infinite and exitContext = true
        ========================================================================*/
        public static bool WaitAll(WaitHandle[] waitHandles)
        {
            return WaitAll(waitHandles, Timeout.Infinite, true);
        }

        public static bool WaitAll(WaitHandle[] waitHandles, int millisecondsTimeout)
        {
            return WaitAll(waitHandles, millisecondsTimeout, true);
        }

        public static bool WaitAll(WaitHandle[] waitHandles, TimeSpan timeout)
        {
            return WaitAll(waitHandles, timeout, true);
        }


        /*========================================================================
        ** Waits for notification from any of the objects. 
        ** timeout indicates how long to wait before the method returns.
        ** This method will return either when either one of the object have been 
        ** signaled or timeout milliseconds have elapsed.
        ** If exitContext is true then the synchronization domain for the context 
        ** (if in a synchronized context) is exited before the wait and reacquired 
        ========================================================================*/

        public static int WaitAny(WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext)
        {
            if (waitHandles == null)
            {
                throw new ArgumentNullException(nameof(waitHandles), SR.ArgumentNull_Waithandles);
            }
            if (waitHandles.Length == 0)
            {
                throw new ArgumentException(SR.Argument_EmptyWaithandleArray);
            }
            if (MAX_WAITHANDLES < waitHandles.Length)
            {
                throw new NotSupportedException(SR.NotSupported_MaxWaitHandles);
            }
            if (-1 > millisecondsTimeout)
            {
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            }
            WaitHandle[] internalWaitHandles = new WaitHandle[waitHandles.Length];
            for (int i = 0; i < waitHandles.Length; i++)
            {
                WaitHandle waitHandle = waitHandles[i];

                if (waitHandle == null)
                    throw new ArgumentNullException("waitHandles[" + i + "]", SR.ArgumentNull_ArrayElement);

                internalWaitHandles[i] = waitHandle;
            }
#if DEBUG
            // make sure we do not use waitHandles any more.
            waitHandles = null;
#endif
            int ret = WaitMultiple(internalWaitHandles, millisecondsTimeout, exitContext, false /* waitany*/ );

            if ((WAIT_ABANDONED <= ret) && (WAIT_ABANDONED + internalWaitHandles.Length > ret))
            {
                int mutexIndex = ret - WAIT_ABANDONED;
                if (0 <= mutexIndex && mutexIndex < internalWaitHandles.Length)
                {
                    ThrowAbandonedMutexException(mutexIndex, internalWaitHandles[mutexIndex]);
                }
                else
                {
                    ThrowAbandonedMutexException();
                }
            }

            GC.KeepAlive(internalWaitHandles);
            return ret;
        }

        public static int WaitAny(
                                    WaitHandle[] waitHandles,
                                    TimeSpan timeout,
                                    bool exitContext)
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (-1 > tm || (long)int.MaxValue < tm)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            }
            return WaitAny(waitHandles, (int)tm, exitContext);
        }
        public static int WaitAny(WaitHandle[] waitHandles, TimeSpan timeout)
        {
            return WaitAny(waitHandles, timeout, true);
        }


        /*========================================================================
        ** Shorthand for WaitAny with timeout = Timeout.Infinite and exitContext = true
        ========================================================================*/
        public static int WaitAny(WaitHandle[] waitHandles)
        {
            return WaitAny(waitHandles, Timeout.Infinite, true);
        }

        public static int WaitAny(WaitHandle[] waitHandles, int millisecondsTimeout)
        {
            return WaitAny(waitHandles, millisecondsTimeout, true);
        }

        /*=================================================
        ==
        ==  SignalAndWait
        ==
        ==================================================*/
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int SignalAndWaitOne(SafeWaitHandle waitHandleToSignal, SafeWaitHandle waitHandleToWaitOn, int millisecondsTimeout,
                                            bool hasThreadAffinity, bool exitContext);

        public static bool SignalAndWait(
                                        WaitHandle toSignal,
                                        WaitHandle toWaitOn)
        {
            return SignalAndWait(toSignal, toWaitOn, -1, false);
        }

        public static bool SignalAndWait(
                                        WaitHandle toSignal,
                                        WaitHandle toWaitOn,
                                        TimeSpan timeout,
                                        bool exitContext)
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (-1 > tm || (long)int.MaxValue < tm)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            }
            return SignalAndWait(toSignal, toWaitOn, (int)tm, exitContext);
        }

        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "Reviewed for thread-safety.")]
        public static bool SignalAndWait(
                                        WaitHandle toSignal,
                                        WaitHandle toWaitOn,
                                        int millisecondsTimeout,
                                        bool exitContext)
        {
            if (null == toSignal)
            {
                throw new ArgumentNullException(nameof(toSignal));
            }
            if (null == toWaitOn)
            {
                throw new ArgumentNullException(nameof(toWaitOn));
            }
            if (-1 > millisecondsTimeout)
            {
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            }

            //NOTE: This API is not supporting Pause/Resume as it's not exposed in CoreCLR (not in WP or SL)
            int ret = SignalAndWaitOne(toSignal._waitHandle, toWaitOn._waitHandle, millisecondsTimeout,
                                toWaitOn.hasThreadAffinity, exitContext);

            if (WAIT_ABANDONED == ret)
            {
                ThrowAbandonedMutexException();
            }

            if (ERROR_TOO_MANY_POSTS == ret)
            {
                throw new InvalidOperationException(SR.Threading_WaitHandleTooManyPosts);
            }

            //Object was signaled
            if (WAIT_OBJECT_0 == ret)
            {
                return true;
            }

            //Timeout
            return false;
        }

        private static void ThrowAbandonedMutexException()
        {
            throw new AbandonedMutexException();
        }

        private static void ThrowAbandonedMutexException(int location, WaitHandle handle)
        {
            throw new AbandonedMutexException(location, handle);
        }

        public virtual void Close()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool explicitDisposing)
        {
            if (_waitHandle != null)
            {
                _waitHandle.Close();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
