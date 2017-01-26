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

namespace System.Threading {
    using System.Threading;
    using System.Runtime;
    using System.Runtime.InteropServices;
    using System;
    using System.Security.Permissions;
    using System.Security.Principal;
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
        static ThreadHelper() {}

        Delegate _start;
        Object _startArg = null;
        ExecutionContext _executionContext = null;
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

    // deliberately not [serializable]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(_Thread))]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class Thread : RuntimeThread, _Thread
    {
        /*=========================================================================
        ** Data accessed from managed code that needs to be defined in
        ** ThreadBaseObject to maintain alignment between the two classes.
        ** DON'T CHANGE THESE UNLESS YOU MODIFY ThreadBaseObject in vm\object.h
        =========================================================================*/
        private ExecutionContext m_ExecutionContext;    // this call context follows the logical thread
        private SynchronizationContext m_SynchronizationContext;    // On CoreCLR, this is maintained separately from ExecutionContext

        private String          m_Name;
        private Delegate        m_Delegate;             // Delegate

        private Object          m_ThreadStartArg;

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
                                                        
        private IntPtr  DONT_USE_InternalThread;        // Pointer
        private int     m_Priority;                     // INT32
        private int     m_ManagedThreadId;              // INT32

#pragma warning restore 414
#pragma warning restore 169

        private bool m_ExecutionContextBelongsToOuterScope;
#if DEBUG
        private bool m_ForbidExecutionContextMutation;
#endif

        /*=========================================================================
        ** This manager is responsible for storing the global data that is
        ** shared amongst all the thread local stores.
        =========================================================================*/
        static private LocalDataStoreMgr s_LocalDataStoreMgr;

        /*=========================================================================
        ** Thread-local data store
        =========================================================================*/
        [ThreadStatic]
        static private LocalDataStoreHolder s_LocalDataStore;

        // Do not move! Order of above fields needs to be preserved for alignment
        // with native code
        // See code:#threadCultureInfo
        [ThreadStatic]
        internal static CultureInfo     m_CurrentCulture;
        [ThreadStatic]
        internal static CultureInfo     m_CurrentUICulture;

        static AsyncLocal<CultureInfo> s_asyncLocalCurrentCulture; 
        static AsyncLocal<CultureInfo> s_asyncLocalCurrentUICulture;

        static void AsyncLocalSetCurrentCulture(AsyncLocalValueChangedArgs<CultureInfo> args)
        {
            m_CurrentCulture = args.CurrentValue;
        }

        static void AsyncLocalSetCurrentUICulture(AsyncLocalValueChangedArgs<CultureInfo> args)
        {
            m_CurrentUICulture = args.CurrentValue;
        }

        // Adding an empty default ctor for annotation purposes
        internal Thread(){}

        /*=========================================================================
        ** Creates a new Thread object which will begin execution at
        ** start.ThreadStart on a new thread when the Start method is called.
        **
        ** Exceptions: ArgumentNullException if start == null.
        =========================================================================*/
        public Thread(ThreadStart start) {
            if (start == null) {
                throw new ArgumentNullException(nameof(start));
            }
            Contract.EndContractBlock();
            SetStartHelper((Delegate)start,0);  //0 will setup Thread with default stackSize
        }

        public Thread(ThreadStart start, int maxStackSize) {
            if (start == null) {
                throw new ArgumentNullException(nameof(start));
            }
            if (0 > maxStackSize)
                throw new ArgumentOutOfRangeException(nameof(maxStackSize),Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            SetStartHelper((Delegate)start, maxStackSize);
        }
        public Thread(ParameterizedThreadStart start) {
            if (start == null) {
                throw new ArgumentNullException(nameof(start));
            }
            Contract.EndContractBlock();
            SetStartHelper((Delegate)start, 0);
        }

        public Thread(ParameterizedThreadStart start, int maxStackSize) {
            if (start == null) {
                throw new ArgumentNullException(nameof(start));
            }
            if (0 > maxStackSize)
                throw new ArgumentOutOfRangeException(nameof(maxStackSize),Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            SetStartHelper((Delegate)start, maxStackSize);
        }

        [ComVisible(false)]
        public override int GetHashCode()
        {
            return m_ManagedThreadId;
        }

        extern public new int ManagedThreadId
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public new void Start()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Start(ref stackMark);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public new void Start(object parameter)
        {
            //In the case of a null delegate (second call to start on same thread)
            //    StartInternal method will take care of the error reporting
            if(m_Delegate is ThreadStart)
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
                ExecutionContext ec = ExecutionContext.Capture(
                    ref stackMark,
                    ExecutionContext.CaptureOptions.IgnoreSyncCtx);
                t.SetExecutionContextHelper(ec);
            }

            IPrincipal principal = null;
            StartInternal(principal, ref stackMark);
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
        private extern void StartInternal(IPrincipal principal, ref StackCrawlMark stackMark);


        // Helper method to get a logical thread ID for StringBuilder (for
        // correctness) and for FileStream's async code path (for perf, to
        // avoid creating a Thread instance).
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static IntPtr InternalGetCurrentThread();

        /*=========================================================================
        ** Raises a ThreadAbortException in the thread, which usually
        ** results in the thread's death. The ThreadAbortException is a special
        ** exception that is not catchable. The finally clauses of all try
        ** statements will be executed before the thread dies. This includes the
        ** finally that a thread might be executing at the moment the Abort is raised.
        ** The thread is not stopped immediately--you must Join on the
        ** thread to guarantee it has stopped.
        ** It is possible for a thread to do an unbounded amount of computation in
        ** the finally's and thus indefinitely delay the threads death.
        ** If Abort() is called on a thread that has not been started, the thread
        ** will abort when Start() is called.
        ** If Abort is called twice on the same thread, a DuplicateThreadAbort
        ** exception is thrown.
        =========================================================================*/
        public void Abort()
        {
            AbortInternal();
        }

        // Internal helper (since we can't place security demands on
        // ecalls/fcalls).
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void AbortInternal();

        public bool Join(TimeSpan timeout)
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (tm < -1 || tm > (long) Int32.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));

            return Join((int)tm);
        }

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
            if(AppDomainPauseManager.IsPaused)
                AppDomainPauseManager.ResumeEvent.WaitOneWithoutFAS();
        }

        public static void Sleep(TimeSpan timeout)
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (tm < -1 || tm > (long) Int32.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            Sleep((int)tm);
        }


        /* wait for a length of time proportial to 'iterations'.  Each iteration is should
           only take a few machine instructions.  Calling this API is preferable to coding
           a explict busy loop because the hardware can be informed that it is busy waiting. */

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private static extern void SpinWaitInternal(int iterations);

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static new void SpinWait(int iterations)
        {
            SpinWaitInternal(iterations);
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private static extern bool YieldInternal();

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static new bool Yield()
        {
            return YieldInternal();
        }
        
        public static new Thread CurrentThread {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            get {
                Contract.Ensures(Contract.Result<Thread>() != null);
                return GetCurrentThreadNative();
            }
        }
        [MethodImplAttribute(MethodImplOptions.InternalCall), ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private static extern Thread GetCurrentThreadNative();

        private void SetStartHelper(Delegate start, int maxStackSize)
        {
            // We only support default stacks in CoreCLR
            Debug.Assert(maxStackSize == 0);

            ThreadHelper threadStartCallBack = new ThreadHelper(start);
            if(start is ThreadStart)
            {
                SetStart(new ThreadStart(threadStartCallBack.ThreadStart), maxStackSize);
            }
            else
            {
                SetStart(new ParameterizedThreadStart(threadStartCallBack.ThreadStart), maxStackSize);
            }                
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern ulong GetProcessDefaultStackSize();

        /*=========================================================================
        ** PRIVATE Sets the IThreadable interface for the thread. Assumes that
        ** start != null.
        =========================================================================*/
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void SetStart(Delegate start, int maxStackSize);

        /*=========================================================================
        ** Clean up the thread when it goes away.
        =========================================================================*/
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        ~Thread()
        {
            // Delegate to the unmanaged portion.
            InternalFinalize();
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void InternalFinalize();

#if FEATURE_COMINTEROP_APARTMENT_SUPPORT
        /*=========================================================================
        ** An unstarted thread can be marked to indicate that it will host a
        ** single-threaded or multi-threaded apartment.
        **
        ** Exceptions: ArgumentException if state is not a valid apartment state
        **             (ApartmentSTA or ApartmentMTA).
        =========================================================================*/
        [Obsolete("The ApartmentState property has been deprecated.  Use GetApartmentState, SetApartmentState or TrySetApartmentState instead.", false)]
        public ApartmentState ApartmentState
        {
            get
            {
                return (ApartmentState)GetApartmentStateNative();
            }

            set
            {
                SetApartmentStateNative((int)value, true);
            }
        }

        public void SetApartmentState(ApartmentState state)
        {
            bool result = SetApartmentStateHelper(state, true);
            if (!result)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ApartmentStateSwitchFailed"));
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void StartupSetApartmentStateInternal();
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

        /*=========================================================================
        ** Allocates an un-named data slot. The slot is allocated on ALL the
        ** threads.
        =========================================================================*/
        public static LocalDataStoreSlot AllocateDataSlot()
        {
            return LocalDataStoreManager.AllocateDataSlot();
        }

        /*=========================================================================
        ** Allocates a named data slot. The slot is allocated on ALL the
        ** threads.  Named data slots are "public" and can be manipulated by
        ** anyone.
        =========================================================================*/
        public static LocalDataStoreSlot AllocateNamedDataSlot(String name)
        {
            return LocalDataStoreManager.AllocateNamedDataSlot(name);
        }

        /*=========================================================================
        ** Looks up a named data slot. If the name has not been used, a new slot is
        ** allocated.  Named data slots are "public" and can be manipulated by
        ** anyone.
        =========================================================================*/
        public static LocalDataStoreSlot GetNamedDataSlot(String name)
        {
            return LocalDataStoreManager.GetNamedDataSlot(name);
        }

        /*=========================================================================
        ** Frees a named data slot. The slot is allocated on ALL the
        ** threads.  Named data slots are "public" and can be manipulated by
        ** anyone.
        =========================================================================*/
        public static void FreeNamedDataSlot(String name)
        {
            LocalDataStoreManager.FreeNamedDataSlot(name);
        }

        /*=========================================================================
        ** Retrieves the value from the specified slot on the current thread, for that thread's current domain.
        =========================================================================*/
        public static Object GetData(LocalDataStoreSlot slot)
        {
            LocalDataStoreHolder dls = s_LocalDataStore;
            if (dls == null)
            {
                // Make sure to validate the slot even if we take the quick path
                LocalDataStoreManager.ValidateSlot(slot);
                return null;
            }

            return dls.Store.GetData(slot);
        }

        /*=========================================================================
        ** Sets the data in the specified slot on the currently running thread, for that thread's current domain.
        =========================================================================*/
        public static void SetData(LocalDataStoreSlot slot, Object data)
        {
            LocalDataStoreHolder dls = s_LocalDataStore;

            // Create new DLS if one hasn't been created for this domain for this thread
            if (dls == null) {
                dls = LocalDataStoreManager.CreateLocalDataStore();
                s_LocalDataStore = dls;
            }

            dls.Store.SetData(slot, data);
        }


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

        public CultureInfo CurrentUICulture {
            get {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);
#if FEATURE_APPX && !FEATURE_COREFX_GLOBALIZATION
                if(AppDomain.IsAppXModel()) {
                    return CultureInfo.GetCultureInfoForUserPreferredLanguageInAppX() ?? GetCurrentUICultureNoAppX();
                } 
                else 
#endif
                {
                    return GetCurrentUICultureNoAppX();
                }
            }

            set {
                if (value == null) {
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

        internal CultureInfo GetCurrentUICultureNoAppX() {

            Contract.Ensures(Contract.Result<CultureInfo>() != null);

#if FEATURE_COREFX_GLOBALIZATION
            return CultureInfo.CurrentUICulture;
#else

            // Fetch a local copy of m_CurrentUICulture to 
            // avoid race conditions that malicious user can introduce
            if (m_CurrentUICulture == null) {
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

        public CultureInfo CurrentCulture {
            get {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);

#if FEATURE_APPX && !FEATURE_COREFX_GLOBALIZATION
                if(AppDomain.IsAppXModel()) {
                    return CultureInfo.GetCultureInfoForUserPreferredLanguageInAppX() ?? GetCurrentCultureNoAppX();
                } 
                else 
#endif
                {
                    return GetCurrentCultureNoAppX();
                }
            }

            set {
                if (null==value) {
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

        private CultureInfo GetCurrentCultureNoAppX() {

#if FEATURE_COREFX_GLOBALIZATION
            return CultureInfo.CurrentCulture;
#else
            Contract.Ensures(Contract.Result<CultureInfo>() != null);

            // Fetch a local copy of m_CurrentCulture to 
            // avoid race conditions that malicious user can introduce
            if (m_CurrentCulture == null) {
                CultureInfo appDomainDefaultCulture =  CultureInfo.DefaultThreadCurrentCulture;
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

        public static AppDomain GetDomain()
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
        public static int GetDomainID()
        {
            return GetDomain().GetId();
        }


        // Retrieves the name of the thread.
        //
        public new String Name {
            get {
                return m_Name;
            }
            set {
                lock(this) {
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

        internal Object AbortReason {
            get {
                object result = null;
                try
                {
                    result = GetAbortReason();
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ExceptionStateCrossAppDomain"), e);
                }
                return result;
            }
            set { SetAbortReason(value); }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern void MemoryBarrier();

        private static LocalDataStoreMgr LocalDataStoreManager
        {
            get 
            {
                if (s_LocalDataStoreMgr == null)
                {
                    Interlocked.CompareExchange(ref s_LocalDataStoreMgr, new LocalDataStoreMgr(), null);    
                }

                return s_LocalDataStoreMgr;
            }
        }

        // Helper function to set the AbortReason for a thread abort.
        //  Checks that they're not alredy set, and then atomically updates
        //  the reason info (object + ADID).
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern void SetAbortReason(Object o);
    
        // Helper function to retrieve the AbortReason from a thread
        //  abort.  Will perform cross-AppDomain marshalling if the object
        //  lives in a different AppDomain from the requester.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern Object GetAbortReason();
    
        // Helper function to clear the AbortReason.  Takes care of
        //  AppDomain related cleanup if required.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern void ClearAbortReason();


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
