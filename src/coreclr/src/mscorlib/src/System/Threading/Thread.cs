// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
/*=============================================================================
**
**
**
** Purpose: Class for creating and managing a thread.
**
**
=============================================================================*/

using Internal.Runtime.Augments;

namespace System.Threading
{
    using System.Threading;
    using System.Runtime;
    using System.Runtime.InteropServices;
    using System;
    using System.Globalization;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Security;
    using System.Runtime.Versioning;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;

    internal delegate Object InternalCrossContextDelegate(Object[] args);

    internal class ThreadHelper
    {
        private Delegate _start;
        private Object _startArg = null;
        private ExecutionContext _executionContext = null;
        internal ThreadHelper(Delegate start)
        {
            _start = start;
        }

        internal void SetExecutionContextHelper(ExecutionContext ec)
        {
            _executionContext = ec;
        }

        static internal ContextCallback _ccb = new ContextCallback(ThreadStart_Context);

        static private void ThreadStart_Context(Object state)
        {
            ThreadHelper t = (ThreadHelper)state;
            if (t._start is ThreadStart)
            {
                ((ThreadStart)t._start)();
            }
            else
            {
                ((ParameterizedThreadStart)t._start)(t._startArg);
            }
        }

        // call back helper
        internal void ThreadStart(object obj)
        {
            _startArg = obj;
            if (_executionContext != null)
            {
                ExecutionContext.Run(_executionContext, _ccb, (Object)this);
            }
            else
            {
                ((ParameterizedThreadStart)_start)(obj);
            }
        }

        // call back helper
        internal void ThreadStart()
        {
            if (_executionContext != null)
            {
                ExecutionContext.Run(_executionContext, _ccb, (Object)this);
            }
            else
            {
                ((ThreadStart)_start)();
            }
        }
    }

    internal struct ThreadHandle
    {
        private IntPtr m_ptr;

        internal ThreadHandle(IntPtr pThread)
        {
            m_ptr = pThread;
        }
    }

    public sealed class Thread : RuntimeThread
    {
        /*=========================================================================
        ** Data accessed from managed code that needs to be defined in
        ** ThreadBaseObject to maintain alignment between the two classes.
        ** DON'T CHANGE THESE UNLESS YOU MODIFY ThreadBaseObject in vm\object.h
        =========================================================================*/
        private ExecutionContext m_ExecutionContext;    // this call context follows the logical thread
        private SynchronizationContext m_SynchronizationContext;    // On CoreCLR, this is maintained separately from ExecutionContext

        private String m_Name;
        private Delegate m_Delegate;             // Delegate

        private Object m_ThreadStartArg;

        /*=========================================================================
        ** The base implementation of Thread is all native.  The following fields
        ** should never be used in the C# code.  They are here to define the proper
        ** space so the thread object may be allocated.  DON'T CHANGE THESE UNLESS
        ** YOU MODIFY ThreadBaseObject in vm\object.h
        =========================================================================*/
#pragma warning disable 169
#pragma warning disable 414  // These fields are not used from managed.
        // IntPtrs need to be together, and before ints, because IntPtrs are 64-bit
        //  fields on 64-bit platforms, where they will be sorted together.

        private IntPtr DONT_USE_InternalThread;        // Pointer
        private int m_Priority;                     // INT32
        private int m_ManagedThreadId;              // INT32

#pragma warning restore 414
#pragma warning restore 169

        private bool m_ExecutionContextBelongsToOuterScope;
#if DEBUG
        private bool m_ForbidExecutionContextMutation;
#endif

        // Do not move! Order of above fields needs to be preserved for alignment
        // with native code
        // See code:#threadCultureInfo
        [ThreadStatic]
        internal static CultureInfo m_CurrentCulture;
        [ThreadStatic]
        internal static CultureInfo m_CurrentUICulture;

        private static AsyncLocal<CultureInfo> s_asyncLocalCurrentCulture;
        private static AsyncLocal<CultureInfo> s_asyncLocalCurrentUICulture;

        private static void AsyncLocalSetCurrentCulture(AsyncLocalValueChangedArgs<CultureInfo> args)
        {
            m_CurrentCulture = args.CurrentValue;
        }

        private static void AsyncLocalSetCurrentUICulture(AsyncLocalValueChangedArgs<CultureInfo> args)
        {
            m_CurrentUICulture = args.CurrentValue;
        }

        // Adding an empty default ctor for annotation purposes
        internal Thread() { }

        /*=========================================================================
        ** Creates a new Thread object which will begin execution at
        ** start.ThreadStart on a new thread when the Start method is called.
        **
        ** Exceptions: ArgumentNullException if start == null.
        =========================================================================*/
        public Thread(ThreadStart start)
        {
            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }
            Contract.EndContractBlock();
            SetStartHelper((Delegate)start, 0);  //0 will setup Thread with default stackSize
        }

        internal Thread(ThreadStart start, int maxStackSize)
        {
            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }
            if (0 > maxStackSize)
                throw new ArgumentOutOfRangeException(nameof(maxStackSize), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            SetStartHelper((Delegate)start, maxStackSize);
        }
        public Thread(ParameterizedThreadStart start)
        {
            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }
            Contract.EndContractBlock();
            SetStartHelper((Delegate)start, 0);
        }

        internal Thread(ParameterizedThreadStart start, int maxStackSize)
        {
            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }
            if (0 > maxStackSize)
                throw new ArgumentOutOfRangeException(nameof(maxStackSize), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            SetStartHelper((Delegate)start, maxStackSize);
        }

        public override int GetHashCode()
        {
            return m_ManagedThreadId;
        }

        extern public new int ManagedThreadId
        {
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            get;
        }

        // Returns handle for interop with EE. The handle is guaranteed to be non-null.
        internal unsafe ThreadHandle GetNativeHandle()
        {
            IntPtr thread = DONT_USE_InternalThread;

            // This should never happen under normal circumstances. m_assembly is always assigned before it is handed out to the user.
            // There are ways how to create an unitialized objects through remoting, etc. Avoid AVing in the EE by throwing a nice
            // exception here.
            if (thread.IsNull())
                throw new ArgumentException(null, Environment.GetResourceString("Argument_InvalidHandle"));

            return new ThreadHandle(thread);
        }


        /*=========================================================================
        ** Spawns off a new thread which will begin executing at the ThreadStart
        ** method on the IThreadable interface passed in the constructor. Once the
        ** thread is dead, it cannot be restarted with another call to Start.
        **
        ** Exceptions: ThreadStateException if the thread has already been started.
        =========================================================================*/
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public new void Start()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Start(ref stackMark);
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public new void Start(object parameter)
        {
            //In the case of a null delegate (second call to start on same thread)
            //    StartInternal method will take care of the error reporting
            if (m_Delegate is ThreadStart)
            {
                //We expect the thread to be setup with a ParameterizedThreadStart
                //    if this constructor is called.
                //If we got here then that wasn't the case
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ThreadWrongThreadStart"));
            }
            m_ThreadStartArg = parameter;
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Start(ref stackMark);
        }

        private void Start(ref StackCrawlMark stackMark)
        {
#if FEATURE_COMINTEROP_APARTMENT_SUPPORT
            // Eagerly initialize the COM Apartment state of the thread if we're allowed to.
            StartupSetApartmentStateInternal();
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

            // Attach current thread's security principal object to the new
            // thread. Be careful not to bind the current thread to a principal
            // if it's not already bound.
            if (m_Delegate != null)
            {
                // If we reach here with a null delegate, something is broken. But we'll let the StartInternal method take care of
                // reporting an error. Just make sure we dont try to dereference a null delegate.
                ThreadHelper t = (ThreadHelper)(m_Delegate.Target);
                ExecutionContext ec = ExecutionContext.Capture();
                t.SetExecutionContextHelper(ec);
            }

            StartInternal(ref stackMark);
        }

        internal ExecutionContext ExecutionContext
        {
            get { return m_ExecutionContext; }
            set { m_ExecutionContext = value; }
        }

        internal SynchronizationContext SynchronizationContext
        {
            get { return m_SynchronizationContext; }
            set { m_SynchronizationContext = value; }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void StartInternal(ref StackCrawlMark stackMark);


        // Helper method to get a logical thread ID for StringBuilder (for
        // correctness) and for FileStream's async code path (for perf, to
        // avoid creating a Thread instance).
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static IntPtr InternalGetCurrentThread();

        /*=========================================================================
        ** Suspends the current thread for timeout milliseconds. If timeout == 0,
        ** forces the thread to give up the remainer of its timeslice.  If timeout
        ** == Timeout.Infinite, no timeout will occur.
        **
        ** Exceptions: ArgumentException if timeout < 0.
        **             ThreadInterruptedException if the thread is interrupted while sleeping.
        =========================================================================*/
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void SleepInternal(int millisecondsTimeout);

        public static new void Sleep(int millisecondsTimeout)
        {
            SleepInternal(millisecondsTimeout);
            // Ensure we don't return to app code when the pause is underway
            if (AppDomainPauseManager.IsPaused)
                AppDomainPauseManager.ResumeEvent.WaitOneWithoutFAS();
        }

        public static void Sleep(TimeSpan timeout)
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (tm < -1 || tm > (long)Int32.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            Sleep((int)tm);
        }


        /* wait for a length of time proportial to 'iterations'.  Each iteration is should
           only take a few machine instructions.  Calling this API is preferable to coding
           a explict busy loop because the hardware can be informed that it is busy waiting. */

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void SpinWaitInternal(int iterations);

        public static new void SpinWait(int iterations)
        {
            SpinWaitInternal(iterations);
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern bool YieldInternal();

        internal static new bool Yield()
        {
            return YieldInternal();
        }

        public static new Thread CurrentThread
        {
            get
            {
                Contract.Ensures(Contract.Result<Thread>() != null);
                return GetCurrentThreadNative();
            }
        }
        [MethodImplAttribute(MethodImplOptions.InternalCall), ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private static extern Thread GetCurrentThreadNative();

        private void SetStartHelper(Delegate start, int maxStackSize)
        {
            Debug.Assert(maxStackSize >= 0);

            ThreadHelper threadStartCallBack = new ThreadHelper(start);
            if (start is ThreadStart)
            {
                SetStart(new ThreadStart(threadStartCallBack.ThreadStart), maxStackSize);
            }
            else
            {
                SetStart(new ParameterizedThreadStart(threadStartCallBack.ThreadStart), maxStackSize);
            }
        }

        /*=========================================================================
        ** PRIVATE Sets the IThreadable interface for the thread. Assumes that
        ** start != null.
        =========================================================================*/
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void SetStart(Delegate start, int maxStackSize);

        /*=========================================================================
        ** Clean up the thread when it goes away.
        =========================================================================*/
        ~Thread()
        {
            // Delegate to the unmanaged portion.
            InternalFinalize();
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void InternalFinalize();

#if FEATURE_COMINTEROP_APARTMENT_SUPPORT

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void StartupSetApartmentStateInternal();
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

        // #threadCultureInfo
        //
        // Background:
        // In the desktop runtime, we allow a thread's cultures to travel with the thread
        // across AppDomain boundaries. Furthermore we update the native thread with the
        // culture of the managed thread. Because of security concerns and potential SxS
        // effects, in Silverlight we are making the changes listed below. 
        // 
        // Silverlight Changes:
        // - thread instance member cultures (CurrentCulture and CurrentUICulture) 
        //   confined within AppDomains
        // - changes to these properties don't affect the underlying native thread
        // 
        // Implementation notes:
        // In Silverlight, culture members thread static (per Thread, per AppDomain). 
        //
        // Quirks:
        // An interesting side-effect of isolating cultures within an AppDomain is that we
        // now need to special case resource lookup for mscorlib, which transitions to the 
        // default domain to lookup resources. See Environment.cs for more details.
        // 

        // As the culture can be customized object then we cannot hold any 
        // reference to it before we check if it is safe because the app domain 
        // owning this customized culture may get unloaded while executing this 
        // code. To achieve that we have to do the check using nativeGetSafeCulture 
        // as the thread cannot get interrupted during the FCALL. 
        // If the culture is safe (not customized or created in current app domain) 
        // then the FCALL will return a reference to that culture otherwise the 
        // FCALL will return failure. In case of failure we'll return the default culture.
        // If the app domain owning a customized culture that is set to the thread and this
        // app domain get unloaded there is a code to clean up the culture from the thread
        // using the code in AppDomain::ReleaseDomainStores.

        public CultureInfo CurrentUICulture
        {
            get
            {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);
#if FEATURE_APPX && !FEATURE_COREFX_GLOBALIZATION
                if (AppDomain.IsAppXModel())
                {
                    return CultureInfo.GetCultureInfoForUserPreferredLanguageInAppX() ?? GetCurrentUICultureNoAppX();
                }
                else
#endif
                {
                    return GetCurrentUICultureNoAppX();
                }
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                Contract.EndContractBlock();

                //If they're trying to use a Culture with a name that we can't use in resource lookup,
                //don't even let them set it on the thread.
                CultureInfo.VerifyCultureName(value, true);

                // If you add more pre-conditions to this method, check to see if you also need to 
                // add them to CultureInfo.DefaultThreadCurrentUICulture.set.

                if (m_CurrentUICulture == null && m_CurrentCulture == null)
                    nativeInitCultureAccessors();

                if (!AppContextSwitches.NoAsyncCurrentCulture)
                {
                    if (s_asyncLocalCurrentUICulture == null)
                    {
                        Interlocked.CompareExchange(ref s_asyncLocalCurrentUICulture, new AsyncLocal<CultureInfo>(AsyncLocalSetCurrentUICulture), null);
                    }

                    // this one will set m_CurrentUICulture too
                    s_asyncLocalCurrentUICulture.Value = value;
                }
                else
                {
                    m_CurrentUICulture = value;
                }
            }
        }

        internal CultureInfo GetCurrentUICultureNoAppX()
        {
            Contract.Ensures(Contract.Result<CultureInfo>() != null);

#if FEATURE_COREFX_GLOBALIZATION
            return CultureInfo.CurrentUICulture;
#else

            // Fetch a local copy of m_CurrentUICulture to 
            // avoid race conditions that malicious user can introduce
            if (m_CurrentUICulture == null)
            {
                CultureInfo appDomainDefaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;
                return (appDomainDefaultUICulture != null ? appDomainDefaultUICulture : CultureInfo.UserDefaultUICulture);
            }

            return m_CurrentUICulture;
#endif
        }

        // This returns the exposed context for a given context ID.

        // As the culture can be customized object then we cannot hold any 
        // reference to it before we check if it is safe because the app domain 
        // owning this customized culture may get unloaded while executing this 
        // code. To achieve that we have to do the check using nativeGetSafeCulture 
        // as the thread cannot get interrupted during the FCALL. 
        // If the culture is safe (not customized or created in current app domain) 
        // then the FCALL will return a reference to that culture otherwise the 
        // FCALL will return failure. In case of failure we'll return the default culture.
        // If the app domain owning a customized culture that is set to the thread and this
        // app domain get unloaded there is a code to clean up the culture from the thread
        // using the code in AppDomain::ReleaseDomainStores.

        public CultureInfo CurrentCulture
        {
            get
            {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);

#if FEATURE_APPX && !FEATURE_COREFX_GLOBALIZATION
                if (AppDomain.IsAppXModel())
                {
                    return CultureInfo.GetCultureInfoForUserPreferredLanguageInAppX() ?? GetCurrentCultureNoAppX();
                }
                else
#endif
                {
                    return GetCurrentCultureNoAppX();
                }
            }

            set
            {
                if (null == value)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                Contract.EndContractBlock();

                // If you add more pre-conditions to this method, check to see if you also need to 
                // add them to CultureInfo.DefaultThreadCurrentCulture.set.

                if (m_CurrentCulture == null && m_CurrentUICulture == null)
                    nativeInitCultureAccessors();

                if (!AppContextSwitches.NoAsyncCurrentCulture)
                {
                    if (s_asyncLocalCurrentCulture == null)
                    {
                        Interlocked.CompareExchange(ref s_asyncLocalCurrentCulture, new AsyncLocal<CultureInfo>(AsyncLocalSetCurrentCulture), null);
                    }
                    // this one will set m_CurrentCulture too
                    s_asyncLocalCurrentCulture.Value = value;
                }
                else
                {
                    m_CurrentCulture = value;
                }
            }
        }

        private CultureInfo GetCurrentCultureNoAppX()
        {
#if FEATURE_COREFX_GLOBALIZATION
            return CultureInfo.CurrentCulture;
#else
            Contract.Ensures(Contract.Result<CultureInfo>() != null);

            // Fetch a local copy of m_CurrentCulture to 
            // avoid race conditions that malicious user can introduce
            if (m_CurrentCulture == null)
            {
                CultureInfo appDomainDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
                return (appDomainDefaultCulture != null ? appDomainDefaultCulture : CultureInfo.UserDefaultCulture);
            }

            return m_CurrentCulture;
#endif
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void nativeInitCultureAccessors();

        /*======================================================================
        ** Returns the current domain in which current thread is running.
        ======================================================================*/

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern AppDomain GetDomainInternal();
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern AppDomain GetFastDomainInternal();

        internal static AppDomain GetDomain()
        {
            Contract.Ensures(Contract.Result<AppDomain>() != null);


            AppDomain ad;
            ad = GetFastDomainInternal();
            if (ad == null)
                ad = GetDomainInternal();

            return ad;
        }


        /*
         *  This returns a unique id to identify an appdomain.
         */
        internal static int GetDomainID()
        {
            return GetDomain().GetId();
        }


        // Retrieves the name of the thread.
        //
        public new String Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                lock (this)
                {
                    if (m_Name != null)
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WriteOnce"));
                    m_Name = value;

                    InformThreadNameChange(GetNativeHandle(), value, (value != null) ? value.Length : 0);
                }
            }
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void InformThreadNameChange(ThreadHandle t, String name, int len);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern void MemoryBarrier();
    } // End of class Thread

    // declaring a local var of this enum type and passing it by ref into a function that needs to do a
    // stack crawl will both prevent inlining of the calle and pass an ESP point to stack crawl to
    // Declaring these in EH clauses is illegal; they must declared in the main method body
    [Serializable]
    internal enum StackCrawlMark
    {
        LookForMe = 0,
        LookForMyCaller = 1,
        LookForMyCallersCaller = 2,
        LookForThread = 3
    }
}
