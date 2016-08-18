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
    using System.Runtime.Remoting;
    using System;
    using System.Security.Permissions;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Microsoft.Win32.SafeHandles;
    using System.Runtime.Versioning;
    using System.Runtime.ConstrainedExecution;
    using System.Diagnostics.Contracts;
    using System.Diagnostics.CodeAnalysis;
    using Win32Native = Microsoft.Win32.Win32Native;

[System.Runtime.InteropServices.ComVisible(true)]
#if FEATURE_REMOTING
    public abstract class WaitHandle : MarshalByRefObject, IDisposable {
#else // FEATURE_REMOTING
    public abstract class WaitHandle : IDisposable {
#endif // FEATURE_REMOTING
        public const int WaitTimeout = 0x102;                    

        private const int MAX_WAITHANDLES = 64;

#pragma warning disable 414  // Field is not used from managed.
        private IntPtr waitHandle;  // !!! DO NOT MOVE THIS FIELD. (See defn of WAITHANDLEREF in object.h - has hardcoded access to this field.)
#pragma warning restore 414

        [System.Security.SecurityCritical] // auto-generated
        internal volatile SafeWaitHandle safeWaitHandle;

        internal bool hasThreadAffinity;

        [System.Security.SecuritySafeCritical]  // auto-generated
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

        [System.Security.SecuritySafeCritical]  // auto-generated
        private void Init()
        {
            safeWaitHandle = null;
            waitHandle = InvalidHandle;
            hasThreadAffinity = false;
        }
    
    
        [Obsolete("Use the SafeWaitHandle property instead.")]
        public virtual IntPtr Handle 
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get { return safeWaitHandle == null ? InvalidHandle : safeWaitHandle.DangerousGetHandle();}
        
            [System.Security.SecurityCritical]  // auto-generated_required
#if !FEATURE_CORECLR
            [SecurityPermissionAttribute(SecurityAction.InheritanceDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
#endif
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
                    if (safeWaitHandle != null)
                    {
                        safeWaitHandle.SetHandleAsInvalid();
                        safeWaitHandle = null;
                    }
                }
                else
                {
                     safeWaitHandle = new SafeWaitHandle(value, true);
                }
                waitHandle = value;
            }
        }


        public SafeWaitHandle SafeWaitHandle 
        {
            [System.Security.SecurityCritical]  // auto-generated_required
#if !FEATURE_CORECLR
            [SecurityPermissionAttribute(SecurityAction.InheritanceDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
#endif
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            get
            {
                if (safeWaitHandle == null)
                {
                    safeWaitHandle = new SafeWaitHandle(InvalidHandle, false);
                }
                return safeWaitHandle;
            }
        
            [System.Security.SecurityCritical]  // auto-generated_required
#if !FEATURE_CORECLR
            [SecurityPermissionAttribute(SecurityAction.InheritanceDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
#endif
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
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
                         safeWaitHandle = null;
                         waitHandle = InvalidHandle;
                    }
                    else
                    {
                         safeWaitHandle = value;
                         waitHandle = safeWaitHandle.DangerousGetHandle();
                    }
                }
            }
        }

        // Assembly-private version that doesn't do a security check.  Reduces the
        // number of link-time security checks when reading & writing to a file,
        // and helps avoid a link time check while initializing security (If you
        // call a Serialization method that requires security before security
        // has started up, the link time check will start up security, run 
        // serialization code for some security attribute stuff, call into 
        // FileStream, which will then call Sethandle, which requires a link time
        // security check.).  While security has fixed that problem, we still
        // don't need to do a linktime check here.
        [System.Security.SecurityCritical]  // auto-generated
        internal void SetHandleInternal(SafeWaitHandle handle)
        {
            safeWaitHandle = handle;
            waitHandle = handle.DangerousGetHandle();
        }
    
        public virtual bool WaitOne (int millisecondsTimeout, bool exitContext)
        {
            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException("millisecondsTimeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            }
            Contract.EndContractBlock();
            return  WaitOne((long)millisecondsTimeout,exitContext);
        }

        public virtual bool WaitOne (TimeSpan timeout, bool exitContext)
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (-1 > tm || (long) Int32.MaxValue < tm)
            {
                throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            }
            return WaitOne(tm,exitContext);
        }

        public virtual bool WaitOne ()
        {
            //Infinite Timeout
            return  WaitOne(-1,false);
        }

        public virtual bool WaitOne(int millisecondsTimeout)
        {
            return WaitOne(millisecondsTimeout, false); 
        }

        public virtual bool WaitOne(TimeSpan timeout)
        {
            return WaitOne(timeout, false); 
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "Reviewed for thread-safety.")]
        private bool WaitOne(long timeout, bool exitContext)
        {
            return InternalWaitOne(safeWaitHandle, timeout, hasThreadAffinity, exitContext);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static bool InternalWaitOne(SafeHandle waitableSafeHandle, long millisecondsTimeout, bool hasThreadAffinity, bool exitContext)
        {
            if (waitableSafeHandle == null)
            {
                throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_Generic"));
            }
            Contract.EndContractBlock();
            int ret = WaitOneNative(waitableSafeHandle, (uint)millisecondsTimeout, hasThreadAffinity, exitContext);

            if(AppDomainPauseManager.IsPaused)
                AppDomainPauseManager.ResumeEvent.WaitOneWithoutFAS();
            
            if (ret == WAIT_ABANDONED)
            {
                ThrowAbandonedMutexException();
            }
            return (ret != WaitTimeout);
        }
        
        [System.Security.SecurityCritical]
        internal bool WaitOneWithoutFAS()
        {
            // version of waitone without fast application switch (FAS) support
            // This is required to support the Wait which FAS needs (otherwise recursive dependency comes in)
            if (safeWaitHandle == null)
            {
                throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_Generic"));
            }
            Contract.EndContractBlock();

            long timeout = -1;
            int ret = WaitOneNative(safeWaitHandle, (uint)timeout, hasThreadAffinity, false);
            if (ret == WAIT_ABANDONED)
            {
                ThrowAbandonedMutexException();
            }
            return (ret != WaitTimeout);
         }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int WaitOneNative(SafeHandle waitableSafeHandle, uint millisecondsTimeout, bool hasThreadAffinity, bool exitContext);
    
        /*========================================================================
        ** Waits for signal from all the objects. 
        ** timeout indicates how long to wait before the method returns.
        ** This method will return either when all the object have been pulsed
        ** or timeout milliseonds have elapsed.
        ** If exitContext is true then the synchronization domain for the context 
        ** (if in a synchronized context) is exited before the wait and reacquired 
        ========================================================================*/
        
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private static extern int WaitMultiple(WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext, bool WaitAll);

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static bool WaitAll(WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext)
        {
            if (waitHandles == null)
            {
                throw new ArgumentNullException("waitHandles", Environment.GetResourceString("ArgumentNull_Waithandles"));
            }
            if(waitHandles.Length == 0)
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
#if FEATURE_CORECLR
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyWaithandleArray"));
#else
                throw new ArgumentNullException("waitHandles", Environment.GetResourceString("Argument_EmptyWaithandleArray"));
#endif
            }
            if (waitHandles.Length > MAX_WAITHANDLES)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_MaxWaitHandles"));
            }
            if (-1 > millisecondsTimeout)
            {
                throw new ArgumentOutOfRangeException("millisecondsTimeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            }
            Contract.EndContractBlock();
            WaitHandle[] internalWaitHandles = new WaitHandle[waitHandles.Length];
            for (int i = 0; i < waitHandles.Length; i ++)
            {
                WaitHandle waitHandle = waitHandles[i];

                if (waitHandle == null)
                    throw new ArgumentNullException("waitHandles[" + i + "]", Environment.GetResourceString("ArgumentNull_ArrayElement"));

#if FEATURE_REMOTING        
                if (RemotingServices.IsTransparentProxy(waitHandle))
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WaitOnTransparentProxy"));
#endif

                internalWaitHandles[i] = waitHandle;
            }
#if _DEBUG
            // make sure we do not use waitHandles any more.
            waitHandles = null;
#endif

            int ret = WaitMultiple(internalWaitHandles, millisecondsTimeout, exitContext, true /* waitall*/ );

            if(AppDomainPauseManager.IsPaused)
                AppDomainPauseManager.ResumeEvent.WaitOneWithoutFAS();

            if ((WAIT_ABANDONED <= ret) && (WAIT_ABANDONED+internalWaitHandles.Length > ret))
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
            if (-1 > tm || (long) Int32.MaxValue < tm)
            {
                throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            }
            return WaitAll(waitHandles,(int)tm, exitContext);
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
        ** signalled or timeout milliseonds have elapsed.
        ** If exitContext is true then the synchronization domain for the context 
        ** (if in a synchronized context) is exited before the wait and reacquired 
        ========================================================================*/
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static int WaitAny(WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext)
        {
            if (waitHandles==null)
            {
                throw new ArgumentNullException("waitHandles", Environment.GetResourceString("ArgumentNull_Waithandles"));
            }
            if(waitHandles.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyWaithandleArray"));
            }
            if (MAX_WAITHANDLES < waitHandles.Length)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_MaxWaitHandles"));
            }
            if (-1 > millisecondsTimeout)
            {
                throw new ArgumentOutOfRangeException("millisecondsTimeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            }
            Contract.EndContractBlock();
            WaitHandle[] internalWaitHandles = new WaitHandle[waitHandles.Length];
            for (int i = 0; i < waitHandles.Length; i ++)
            {
                WaitHandle waitHandle = waitHandles[i];

                if (waitHandle == null)
                    throw new ArgumentNullException("waitHandles[" + i + "]", Environment.GetResourceString("ArgumentNull_ArrayElement"));

#if FEATURE_REMOTING        
                if (RemotingServices.IsTransparentProxy(waitHandle))
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WaitOnTransparentProxy"));
#endif

                internalWaitHandles[i] = waitHandle;
            }
#if _DEBUG
            // make sure we do not use waitHandles any more.
            waitHandles = null;
#endif
            int ret = WaitMultiple(internalWaitHandles, millisecondsTimeout, exitContext, false /* waitany*/ );

            if(AppDomainPauseManager.IsPaused)
                AppDomainPauseManager.ResumeEvent.WaitOneWithoutFAS();

            if ((WAIT_ABANDONED <= ret) && (WAIT_ABANDONED+internalWaitHandles.Length > ret))
            {
                int mutexIndex = ret -WAIT_ABANDONED;
                if(0 <= mutexIndex && mutexIndex < internalWaitHandles.Length)
                {
                    ThrowAbandonedMutexException(mutexIndex,internalWaitHandles[mutexIndex]);
                }
                else
                {
                    ThrowAbandonedMutexException();
                }
            }
            
            GC.KeepAlive(internalWaitHandles);
                return ret;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static int WaitAny(
                                    WaitHandle[] waitHandles, 
                                    TimeSpan timeout,
                                    bool exitContext)
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (-1 > tm || (long) Int32.MaxValue < tm)
            {
                throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            }
            return WaitAny(waitHandles,(int)tm, exitContext);
        }
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static int WaitAny(WaitHandle[] waitHandles, TimeSpan timeout)
        {
            return WaitAny(waitHandles, timeout, true); 
        }


        /*========================================================================
        ** Shorthand for WaitAny with timeout = Timeout.Infinite and exitContext = true
        ========================================================================*/
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static int WaitAny(WaitHandle[] waitHandles)
        {
            return WaitAny(waitHandles, Timeout.Infinite, true);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static int WaitAny(WaitHandle[] waitHandles, int millisecondsTimeout)
        {
            return WaitAny(waitHandles, millisecondsTimeout, true); 
        }

        /*=================================================
        ==
        ==  SignalAndWait
        ==
        ==================================================*/

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)] 
        private static extern int SignalAndWaitOne(SafeWaitHandle waitHandleToSignal,SafeWaitHandle waitHandleToWaitOn, int millisecondsTimeout,
                                            bool hasThreadAffinity,  bool exitContext);

        public static bool SignalAndWait(
                                        WaitHandle toSignal,
                                        WaitHandle toWaitOn)
        {
#if PLATFORM_UNIX
            throw new PlatformNotSupportedException();
#else
            return SignalAndWait(toSignal,toWaitOn,-1,false);
#endif
        }

        public static bool SignalAndWait(
                                        WaitHandle toSignal,
                                        WaitHandle toWaitOn,
                                        TimeSpan timeout,
                                        bool exitContext)
        {
#if PLATFORM_UNIX
            throw new PlatformNotSupportedException();
#else
            long tm = (long)timeout.TotalMilliseconds;
            if (-1 > tm || (long) Int32.MaxValue < tm)
            {
                throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            }
            return SignalAndWait(toSignal,toWaitOn,(int)tm,exitContext);
#endif
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "Reviewed for thread-safety.")]
        public static bool SignalAndWait(
                                        WaitHandle toSignal,
                                        WaitHandle toWaitOn,
                                        int millisecondsTimeout,
                                        bool exitContext)
        {
#if PLATFORM_UNIX
            throw new PlatformNotSupportedException();
#else
            if(null == toSignal)
            {
                throw new ArgumentNullException("toSignal");
            }
            if(null == toWaitOn)
            {
                throw new ArgumentNullException("toWaitOn");
            }
            if (-1 > millisecondsTimeout)
            {
                throw new ArgumentOutOfRangeException("millisecondsTimeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            }
            Contract.EndContractBlock();

            //NOTE: This API is not supporting Pause/Resume as it's not exposed in CoreCLR (not in WP or SL)
            int ret = SignalAndWaitOne(toSignal.safeWaitHandle,toWaitOn.safeWaitHandle,millisecondsTimeout,
                                toWaitOn.hasThreadAffinity,exitContext);

#if !FEATURE_CORECLR
            if(WAIT_FAILED != ret  && toSignal.hasThreadAffinity)
            {
                Thread.EndCriticalRegion();
                Thread.EndThreadAffinity();
            }
#endif

            if(WAIT_ABANDONED == ret)
            {
                ThrowAbandonedMutexException();
            }

            if(ERROR_TOO_MANY_POSTS == ret)
            {
                throw new InvalidOperationException(Environment.GetResourceString("Threading.WaitHandleTooManyPosts"));
            }

            //Object was signaled
            if(WAIT_OBJECT_0 == ret)
            {
                return true;
            }

            //Timeout
            return false;
#endif
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
            
        [System.Security.SecuritySafeCritical]  // auto-generated
        protected virtual void Dispose(bool explicitDisposing)
        {
            if (safeWaitHandle != null)
            {
                safeWaitHandle.Close();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
