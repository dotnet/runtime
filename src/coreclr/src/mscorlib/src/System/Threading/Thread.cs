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

namespace System.Threading {
    using System.Threading;
    using System.Runtime;
    using System.Runtime.InteropServices;
#if FEATURE_REMOTING    
    using System.Runtime.Remoting.Contexts;
    using System.Runtime.Remoting.Messaging;
#endif
    using System;
    using System.Diagnostics;
    using System.Security.Permissions;
    using System.Security.Principal;
    using System.Globalization;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Security;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    internal delegate Object InternalCrossContextDelegate(Object[] args);

    internal class ThreadHelper
    {
        [System.Security.SecuritySafeCritical]
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

        [System.Security.SecurityCritical]
        static internal ContextCallback _ccb = new ContextCallback(ThreadStart_Context);
        
        [System.Security.SecurityCritical]
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
        #if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
        #else
        [System.Security.SecurityCritical]
        #endif
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
        #if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
        #else
        [System.Security.SecurityCritical]
        #endif
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
    public sealed class Thread : CriticalFinalizerObject, _Thread
    {
        /*=========================================================================
        ** Data accessed from managed code that needs to be defined in
        ** ThreadBaseObject to maintain alignment between the two classes.
        ** DON'T CHANGE THESE UNLESS YOU MODIFY ThreadBaseObject in vm\object.h
        =========================================================================*/
#if FEATURE_REMOTING        
        private Context         m_Context;
#endif 
        private ExecutionContext m_ExecutionContext;    // this call context follows the logical thread
#if FEATURE_CORECLR
        private SynchronizationContext m_SynchronizationContext;    // On CoreCLR, this is maintained separately from ExecutionContext
#endif

        private String          m_Name;
        private Delegate        m_Delegate;             // Delegate
        
#if FEATURE_LEAK_CULTURE_INFO 
        private CultureInfo     m_CurrentCulture;
        private CultureInfo     m_CurrentUICulture;
#endif
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
#if !FEATURE_LEAK_CULTURE_INFO
        [ThreadStatic]
        internal static CultureInfo     m_CurrentCulture;
        [ThreadStatic]
        internal static CultureInfo     m_CurrentUICulture;
#endif

        static AsyncLocal<CultureInfo> s_asyncLocalCurrentCulture; 
        static AsyncLocal<CultureInfo> s_asyncLocalCurrentUICulture;

        static void AsyncLocalSetCurrentCulture(AsyncLocalValueChangedArgs<CultureInfo> args)
        {
#if FEATURE_LEAK_CULTURE_INFO 
            Thread.CurrentThread.m_CurrentCulture = args.CurrentValue;
#else
            m_CurrentCulture = args.CurrentValue;
#endif // FEATURE_LEAK_CULTURE_INFO
        }

        static void AsyncLocalSetCurrentUICulture(AsyncLocalValueChangedArgs<CultureInfo> args)
        {
#if FEATURE_LEAK_CULTURE_INFO 
            Thread.CurrentThread.m_CurrentUICulture = args.CurrentValue;
#else
            m_CurrentUICulture = args.CurrentValue;
#endif // FEATURE_LEAK_CULTURE_INFO
        }

#if FEATURE_CORECLR
        // Adding an empty default ctor for annotation purposes
        [System.Security.SecuritySafeCritical] // auto-generated
        internal Thread(){}
#endif // FEATURE_CORECLR

        /*=========================================================================
        ** Creates a new Thread object which will begin execution at
        ** start.ThreadStart on a new thread when the Start method is called.
        **
        ** Exceptions: ArgumentNullException if start == null.
        =========================================================================*/
        [System.Security.SecuritySafeCritical]  // auto-generated
        public Thread(ThreadStart start) {
            if (start == null) {
                throw new ArgumentNullException("start");
            }
            Contract.EndContractBlock();
            SetStartHelper((Delegate)start,0);  //0 will setup Thread with default stackSize
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public Thread(ThreadStart start, int maxStackSize) {
            if (start == null) {
                throw new ArgumentNullException("start");
            }
            if (0 > maxStackSize)
                throw new ArgumentOutOfRangeException("maxStackSize",Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            SetStartHelper((Delegate)start, maxStackSize);
        }
        [System.Security.SecuritySafeCritical]  // auto-generated
        public Thread(ParameterizedThreadStart start) {
            if (start == null) {
                throw new ArgumentNullException("start");
            }
            Contract.EndContractBlock();
            SetStartHelper((Delegate)start, 0);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public Thread(ParameterizedThreadStart start, int maxStackSize) {
            if (start == null) {
                throw new ArgumentNullException("start");
            }
            if (0 > maxStackSize)
                throw new ArgumentOutOfRangeException("maxStackSize",Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            SetStartHelper((Delegate)start, maxStackSize);
        }

        [ComVisible(false)]
        public override int GetHashCode()
        {
            return m_ManagedThreadId;
        }

        extern public int ManagedThreadId
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            [System.Security.SecuritySafeCritical]  // auto-generated
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
        [HostProtection(Synchronization=true,ExternalThreading=true)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public void Start()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Start(ref stackMark);
        }

        [HostProtection(Synchronization=true,ExternalThreading=true)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public void Start(object parameter)
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

        [System.Security.SecuritySafeCritical]
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
#if FEATURE_IMPERSONATION
            IPrincipal principal = (IPrincipal)CallContext.Principal;
#else
            IPrincipal principal = null;
#endif
            StartInternal(principal, ref stackMark);
        }


#if FEATURE_CORECLR
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
#else // !FEATURE_CORECLR
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal ExecutionContext.Reader GetExecutionContextReader()
        {
            return new ExecutionContext.Reader(m_ExecutionContext);
        }

        internal bool ExecutionContextBelongsToCurrentScope
        {
            get { return !m_ExecutionContextBelongsToOuterScope; }
            set { m_ExecutionContextBelongsToOuterScope = !value; }
        }

#if DEBUG
        internal bool ForbidExecutionContextMutation
        {
            set { m_ForbidExecutionContextMutation = value; }
        }
#endif

        // note: please don't access this directly from mscorlib.  Use GetMutableExecutionContext or GetExecutionContextReader instead.
        public  ExecutionContext ExecutionContext
        {
            [SecuritySafeCritical]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            get
            {
                ExecutionContext result;
                if (this == Thread.CurrentThread)
                    result = GetMutableExecutionContext();
                else
                    result = m_ExecutionContext;

                return result;
            }
        }

        [SecurityCritical]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        internal ExecutionContext GetMutableExecutionContext()
        {
            Contract.Assert(Thread.CurrentThread == this);
#if DEBUG
            Contract.Assert(!m_ForbidExecutionContextMutation);
#endif
            if (m_ExecutionContext == null)
            {
                m_ExecutionContext = new ExecutionContext();
            }
            else if (!ExecutionContextBelongsToCurrentScope)
            {
                ExecutionContext copy = m_ExecutionContext.CreateMutableCopy();
                m_ExecutionContext = copy;
            }

            ExecutionContextBelongsToCurrentScope = true;
            return m_ExecutionContext;
        }

        [SecurityCritical]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal void SetExecutionContext(ExecutionContext value, bool belongsToCurrentScope) 
        {
            m_ExecutionContext = value;
            ExecutionContextBelongsToCurrentScope = belongsToCurrentScope;
        }

        [SecurityCritical]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal void SetExecutionContext(ExecutionContext.Reader value, bool belongsToCurrentScope)
        {
            m_ExecutionContext = value.DangerousGetRawExecutionContext();
            ExecutionContextBelongsToCurrentScope = belongsToCurrentScope;
        }
#endif //!FEATURE_CORECLR

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void StartInternal(IPrincipal principal, ref StackCrawlMark stackMark);
#if FEATURE_COMPRESSEDSTACK
        /// <internalonly/>
        [System.Security.SecurityCritical]  // auto-generated_required
        [DynamicSecurityMethodAttribute()]
        [Obsolete("Thread.SetCompressedStack is no longer supported. Please use the System.Threading.CompressedStack class")]         
        public void SetCompressedStack( CompressedStack stack )
        {
            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ThreadAPIsNotSupported"));
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal extern IntPtr SetAppDomainStack( SafeCompressedStackHandle csHandle);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal extern void RestoreAppDomainStack( IntPtr appDomainStack);
        

        /// <internalonly/>
        [System.Security.SecurityCritical]  // auto-generated_required
        [Obsolete("Thread.GetCompressedStack is no longer supported. Please use the System.Threading.CompressedStack class")]
        public CompressedStack GetCompressedStack()
        {
            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ThreadAPIsNotSupported"));
        }
#endif // #if FEATURE_COMPRESSEDSTACK


        // Helper method to get a logical thread ID for StringBuilder (for
        // correctness) and for FileStream's async code path (for perf, to
        // avoid creating a Thread instance).
        [System.Security.SecurityCritical]  // auto-generated
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

#if !FEATURE_CORECLR
        [System.Security.SecuritySafeCritical]  // auto-generated
        [SecurityPermissionAttribute(SecurityAction.Demand, ControlThread=true)]
        public void Abort(Object stateInfo)
        {
            // If two aborts come at the same time, it is possible that the state info
            //  gets set by one, and the actual abort gets delivered by another. But this
            //  is not distinguishable by an application.
            // The accessor helper will only set the value if it isn't already set,
            //  and that particular bit of native code can test much faster than this
            //  code could, because testing might cause a cross-appdomain marshalling.
            AbortReason = stateInfo;

            // Note: we demand ControlThread permission, then call AbortInternal directly
            // rather than delegating to the Abort() function below. We do this to ensure
            // that only callers with ControlThread are allowed to change the AbortReason
            // of the thread. We call AbortInternal directly to avoid demanding the same
            // permission twice.
            AbortInternal();
        }
#endif

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #else
        [System.Security.SecuritySafeCritical]
        #endif
#pragma warning disable 618
        [SecurityPermissionAttribute(SecurityAction.Demand, ControlThread = true)]
#pragma warning restore 618
        public void Abort()
        {
            AbortInternal();
        }

        // Internal helper (since we can't place security demands on
        // ecalls/fcalls).
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void AbortInternal();

#if !FEATURE_CORECLR
        /*=========================================================================
        ** Resets a thread abort.
        ** Should be called by trusted code only
          =========================================================================*/
        [System.Security.SecuritySafeCritical]  // auto-generated
        [SecurityPermissionAttribute(SecurityAction.Demand, ControlThread=true)]
        public static void ResetAbort()
        {
            Thread thread = Thread.CurrentThread;
            if ((thread.ThreadState & ThreadState.AbortRequested) == 0)
                throw new ThreadStateException(Environment.GetResourceString("ThreadState_NoAbortRequested"));
            thread.ResetAbortNative();
            thread.ClearAbortReason();
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void ResetAbortNative();

        /*=========================================================================
        ** Suspends the thread. If the thread is already suspended, this call has
        ** no effect.
        **
        ** Exceptions: ThreadStateException if the thread has not been started or
        **             it is dead.
        =========================================================================*/
        [System.Security.SecuritySafeCritical]  // auto-generated
        [Obsolete("Thread.Suspend has been deprecated.  Please use other classes in System.Threading, such as Monitor, Mutex, Event, and Semaphore, to synchronize Threads or protect resources.  http://go.microsoft.com/fwlink/?linkid=14202", false)][SecurityPermission(SecurityAction.Demand, ControlThread=true)]
        [SecurityPermission(SecurityAction.Demand, ControlThread=true)]
        public void Suspend() { SuspendInternal(); }

        // Internal helper (since we can't place security demands on
        // ecalls/fcalls).
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void SuspendInternal();

        /*=========================================================================
        ** Resumes a thread that has been suspended.
        **
        ** Exceptions: ThreadStateException if the thread has not been started or
        **             it is dead or it isn't in the suspended state.
        =========================================================================*/
        [System.Security.SecuritySafeCritical]  // auto-generated
        [Obsolete("Thread.Resume has been deprecated.  Please use other classes in System.Threading, such as Monitor, Mutex, Event, and Semaphore, to synchronize Threads or protect resources.  http://go.microsoft.com/fwlink/?linkid=14202", false)]
        [SecurityPermission(SecurityAction.Demand, ControlThread=true)]
        public void Resume() { ResumeInternal(); }

        // Internal helper (since we can't place security demands on
        // ecalls/fcalls).
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void ResumeInternal();

        /*=========================================================================
        ** Interrupts a thread that is inside a Wait(), Sleep() or Join().  If that
        ** thread is not currently blocked in that manner, it will be interrupted
        ** when it next begins to block.
        =========================================================================*/
        [System.Security.SecuritySafeCritical]  // auto-generated
        [SecurityPermission(SecurityAction.Demand, ControlThread=true)]
        public void Interrupt() { InterruptInternal(); }

        // Internal helper (since we can't place security demands on
        // ecalls/fcalls).
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void InterruptInternal();
#endif

        /*=========================================================================
        ** Returns the priority of the thread.
        **
        ** Exceptions: ThreadStateException if the thread is dead.
        =========================================================================*/

        public ThreadPriority Priority {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get { return (ThreadPriority)GetPriorityNative(); }
            [System.Security.SecuritySafeCritical]  // auto-generated
            [HostProtection(SelfAffectingThreading=true)]
            set { SetPriorityNative((int)value); }
        }
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern int GetPriorityNative();
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void SetPriorityNative(int priority);

        /*=========================================================================
        ** Returns true if the thread has been started and is not dead.
        =========================================================================*/
        public extern bool IsAlive {
            [System.Security.SecuritySafeCritical]  // auto-generated
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /*=========================================================================
        ** Returns true if the thread is a threadpool thread.
        =========================================================================*/
        public extern bool IsThreadPoolThread {
            [System.Security.SecuritySafeCritical]  // auto-generated
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /*=========================================================================
        ** Waits for the thread to die or for timeout milliseconds to elapse.
        ** Returns true if the thread died, or false if the wait timed out. If
        ** Timeout.Infinite is given as the parameter, no timeout will occur.
        **
        ** Exceptions: ArgumentException if timeout < 0.
        **             ThreadInterruptedException if the thread is interrupted while waiting.
        **             ThreadStateException if the thread has not been started yet.
        =========================================================================*/
        [System.Security.SecurityCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern bool JoinInternal(int millisecondsTimeout);

        [System.Security.SecuritySafeCritical]
        [HostProtection(Synchronization=true, ExternalThreading=true)]
        public void Join()
        {
            JoinInternal(Timeout.Infinite);
        }

        [System.Security.SecuritySafeCritical]
        [HostProtection(Synchronization=true, ExternalThreading=true)]
        public bool Join(int millisecondsTimeout)
        {
            return JoinInternal(millisecondsTimeout);
        }

        [HostProtection(Synchronization=true, ExternalThreading=true)]
        public bool Join(TimeSpan timeout)
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (tm < -1 || tm > (long) Int32.MaxValue)
                throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));

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
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void SleepInternal(int millisecondsTimeout);

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static void Sleep(int millisecondsTimeout)
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
                throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            Sleep((int)tm);
        }


        /* wait for a length of time proportial to 'iterations'.  Each iteration is should
           only take a few machine instructions.  Calling this API is preferable to coding
           a explict busy loop because the hardware can be informed that it is busy waiting. */

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [HostProtection(Synchronization=true,ExternalThreading=true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private static extern void SpinWaitInternal(int iterations);

        [System.Security.SecuritySafeCritical]  // auto-generated
        [HostProtection(Synchronization=true,ExternalThreading=true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static void SpinWait(int iterations)
        {
            SpinWaitInternal(iterations);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [HostProtection(Synchronization = true, ExternalThreading = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private static extern bool YieldInternal();

        [System.Security.SecuritySafeCritical]  // auto-generated
        [HostProtection(Synchronization = true, ExternalThreading = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static bool Yield()
        {
            return YieldInternal();
        }
        
        public static Thread CurrentThread {
            [System.Security.SecuritySafeCritical]  // auto-generated
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            get {
                Contract.Ensures(Contract.Result<Thread>() != null);
                return GetCurrentThreadNative();
            }
        }
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall), ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private static extern Thread GetCurrentThreadNative();

        [System.Security.SecurityCritical]  // auto-generated
        private void SetStartHelper(Delegate start, int maxStackSize)
        {
#if FEATURE_CORECLR
            // We only support default stacks in CoreCLR
            Contract.Assert(maxStackSize == 0);
#else
            // Only fully-trusted code is allowed to create "large" stacks.  Partial-trust falls back to
            // the default stack size.
            ulong defaultStackSize = GetProcessDefaultStackSize();
            if ((ulong)(uint)maxStackSize > defaultStackSize)
            {
                try
                {
                    SecurityPermission.Demand(PermissionType.FullTrust);
                }
                catch (SecurityException)
                {
                    maxStackSize = (int)Math.Min(defaultStackSize, (ulong)(uint)int.MaxValue);
                }
            }
#endif

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

        [SecurityCritical]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern ulong GetProcessDefaultStackSize();

        /*=========================================================================
        ** PRIVATE Sets the IThreadable interface for the thread. Assumes that
        ** start != null.
        =========================================================================*/
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void SetStart(Delegate start, int maxStackSize);

        /*=========================================================================
        ** Clean up the thread when it goes away.
        =========================================================================*/
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        ~Thread()
        {
            // Delegate to the unmanaged portion.
            InternalFinalize();
        }

        [System.Security.SecurityCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void InternalFinalize();

#if FEATURE_COMINTEROP
        [System.Security.SecurityCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern void DisableComObjectEagerCleanup();
#endif //FEATURE_COMINTEROP

        /*=========================================================================
        ** Return whether or not this thread is a background thread.  Background
        ** threads do not affect when the Execution Engine shuts down.
        **
        ** Exceptions: ThreadStateException if the thread is dead.
        =========================================================================*/
        public bool IsBackground {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get { return IsBackgroundNative(); }
            [System.Security.SecuritySafeCritical]  // auto-generated
            [HostProtection(SelfAffectingThreading=true)]
            set { SetBackgroundNative(value); }
        }
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern bool IsBackgroundNative();
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void SetBackgroundNative(bool isBackground);


        /*=========================================================================
        ** Return the thread state as a consistent set of bits.  This is more
        ** general then IsAlive or IsBackground.
        =========================================================================*/
        public ThreadState ThreadState {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get { return (ThreadState)GetThreadStateNative(); }
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern int GetThreadStateNative();

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
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                return (ApartmentState)GetApartmentStateNative();
            }

            [System.Security.SecuritySafeCritical]  // auto-generated
            [HostProtection(Synchronization=true, SelfAffectingThreading=true)]
            set
            {
                SetApartmentStateNative((int)value, true);
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public ApartmentState GetApartmentState()
        {
            return (ApartmentState)GetApartmentStateNative();
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [HostProtection(Synchronization=true, SelfAffectingThreading=true)]
        public bool TrySetApartmentState(ApartmentState state)
        {
            return SetApartmentStateHelper(state, false);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [HostProtection(Synchronization=true, SelfAffectingThreading=true)]
        public void SetApartmentState(ApartmentState state)
        {
            bool result = SetApartmentStateHelper(state, true);
            if (!result)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ApartmentStateSwitchFailed"));
        }

        [System.Security.SecurityCritical]  // auto-generated
        private bool SetApartmentStateHelper(ApartmentState state, bool fireMDAOnMismatch)
        {
            ApartmentState retState = (ApartmentState)SetApartmentStateNative((int)state, fireMDAOnMismatch);

            // Special case where we pass in Unknown and get back MTA.
            //  Once we CoUninitialize the thread, the OS will still
            //  report the thread as implicitly in the MTA if any
            //  other thread in the process is CoInitialized.
            if ((state == System.Threading.ApartmentState.Unknown) && (retState == System.Threading.ApartmentState.MTA))
                return true;
            
            if (retState != state)
                return false;

            return true;            
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern int GetApartmentStateNative();
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern int SetApartmentStateNative(int state, bool fireMDAOnMismatch);
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void StartupSetApartmentStateInternal();
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

        /*=========================================================================
        ** Allocates an un-named data slot. The slot is allocated on ALL the
        ** threads.
        =========================================================================*/
        [HostProtection(SharedState=true, ExternalThreading=true)]
        public static LocalDataStoreSlot AllocateDataSlot()
        {
            return LocalDataStoreManager.AllocateDataSlot();
        }

        /*=========================================================================
        ** Allocates a named data slot. The slot is allocated on ALL the
        ** threads.  Named data slots are "public" and can be manipulated by
        ** anyone.
        =========================================================================*/
        [HostProtection(SharedState=true, ExternalThreading=true)]
        public static LocalDataStoreSlot AllocateNamedDataSlot(String name)
        {
            return LocalDataStoreManager.AllocateNamedDataSlot(name);
        }

        /*=========================================================================
        ** Looks up a named data slot. If the name has not been used, a new slot is
        ** allocated.  Named data slots are "public" and can be manipulated by
        ** anyone.
        =========================================================================*/
        [HostProtection(SharedState=true, ExternalThreading=true)]
        public static LocalDataStoreSlot GetNamedDataSlot(String name)
        {
            return LocalDataStoreManager.GetNamedDataSlot(name);
        }

        /*=========================================================================
        ** Frees a named data slot. The slot is allocated on ALL the
        ** threads.  Named data slots are "public" and can be manipulated by
        ** anyone.
        =========================================================================*/
        [HostProtection(SharedState=true, ExternalThreading=true)]
        public static void FreeNamedDataSlot(String name)
        {
            LocalDataStoreManager.FreeNamedDataSlot(name);
        }

        /*=========================================================================
        ** Retrieves the value from the specified slot on the current thread, for that thread's current domain.
        =========================================================================*/
        [HostProtection(SharedState=true, ExternalThreading=true)]
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
        [HostProtection(SharedState=true, ExternalThreading=true)]
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
        // Ifdef:
        // FEATURE_LEAK_CULTURE_INFO      : CultureInfos can leak across AppDomains, not
        //                                  enabled in Silverlight
        // 
        // Implementation notes:
        // In Silverlight, culture members thread static (per Thread, per AppDomain). 
        //
        // Quirks:
        // An interesting side-effect of isolating cultures within an AppDomain is that we
        // now need to special case resource lookup for mscorlib, which transitions to the 
        // default domain to lookup resources. See Environment.cs for more details.
        // 
#if FEATURE_LEAK_CULTURE_INFO
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static extern private bool nativeGetSafeCulture(Thread t, int appDomainId, bool isUI, ref CultureInfo safeCulture);
#endif // FEATURE_LEAK_CULTURE_INFO

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

            [System.Security.SecuritySafeCritical]  // auto-generated
            [HostProtection(ExternalThreading=true)]
            set {
                if (value == null) {
                    throw new ArgumentNullException("value");
                }
                Contract.EndContractBlock();

                //If they're trying to use a Culture with a name that we can't use in resource lookup,
                //don't even let them set it on the thread.
                CultureInfo.VerifyCultureName(value, true);

                // If you add more pre-conditions to this method, check to see if you also need to 
                // add them to CultureInfo.DefaultThreadCurrentUICulture.set.

#if FEATURE_LEAK_CULTURE_INFO
                if (nativeSetThreadUILocale(value.SortName) == false)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidResourceCultureName", value.Name));
                }
                value.StartCrossDomainTracking();
#else
                if (m_CurrentUICulture == null && m_CurrentCulture == null)
                    nativeInitCultureAccessors();
#endif

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

#if FEATURE_LEAK_CULTURE_INFO
        [System.Security.SecuritySafeCritical]  // auto-generated
#endif
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

#if FEATURE_LEAK_CULTURE_INFO
            CultureInfo culture = null;

            if (!nativeGetSafeCulture(this, GetDomainID(), true, ref culture) || culture == null) {
                return CultureInfo.UserDefaultUICulture;
            }
                
            return culture;
#else
            return m_CurrentUICulture;
#endif
#endif
        }

        // This returns the exposed context for a given context ID.
#if FEATURE_LEAK_CULTURE_INFO
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static extern private bool nativeSetThreadUILocale(String locale);
#endif

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

            [System.Security.SecuritySafeCritical]  // auto-generated
#if FEATURE_LEAK_CULTURE_INFO
            [SecurityPermission(SecurityAction.Demand, ControlThread = true)]
#endif
            set {
                if (null==value) {
                    throw new ArgumentNullException("value");
                }
                Contract.EndContractBlock();

                // If you add more pre-conditions to this method, check to see if you also need to 
                // add them to CultureInfo.DefaultThreadCurrentCulture.set.

#if FEATURE_LEAK_CULTURE_INFO
                //If we can't set the nativeThreadLocale, we'll just let it stay
                //at whatever value it had before.  This allows people who use
                //just managed code not to be limited by the underlying OS.
                CultureInfo.nativeSetThreadLocale(value.SortName);
                value.StartCrossDomainTracking();
#else
                if (m_CurrentCulture == null && m_CurrentUICulture == null)
                    nativeInitCultureAccessors();
#endif

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

#if FEATURE_LEAK_CULTURE_INFO
        [System.Security.SecuritySafeCritical]  // auto-generated
#endif
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

#if FEATURE_LEAK_CULTURE_INFO
            CultureInfo culture = null;
              
            if (!nativeGetSafeCulture(this, GetDomainID(), false, ref culture) || culture == null) {
                return CultureInfo.UserDefaultCulture;
            }
                
            return culture;
#else
            return m_CurrentCulture;
#endif
#endif
        }

#if! FEATURE_LEAK_CULTURE_INFO
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void nativeInitCultureAccessors();
#endif

        /*=============================================================*/

        /*======================================================================
        **  Current thread context is stored in a slot in the thread local store
        **  CurrentContext gets the Context from the slot.
        ======================================================================*/
#if FEATURE_REMOTING
        public static Context CurrentContext
        {
            [System.Security.SecurityCritical]  // auto-generated_required
            get
            {
                return CurrentThread.GetCurrentContextInternal();
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal Context GetCurrentContextInternal()
        {
            if (m_Context == null)
            {
                m_Context = Context.DefaultContext;
            }
            return m_Context;
        }
#endif        


#if FEATURE_IMPERSONATION
        // Get and set thread's current principal (for role based security).
        public static IPrincipal CurrentPrincipal
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                lock (CurrentThread)
                {
                    IPrincipal principal = (IPrincipal)
                        CallContext.Principal;
                    if (principal == null)
                    {
                        principal = GetDomain().GetThreadPrincipal();
                        CallContext.Principal = principal;
                    }
                    return principal;
                }
            }

            [System.Security.SecuritySafeCritical]  // auto-generated
            [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)]
            set
            {
                CallContext.Principal = value;
            }
        }

        // Private routine called from unmanaged code to set an initial
        // principal for a newly created thread.
        [System.Security.SecurityCritical]  // auto-generated
        private void SetPrincipalInternal(IPrincipal principal)
        {
            GetMutableExecutionContext().LogicalCallContext.SecurityData.Principal = principal;
        }
#endif // FEATURE_IMPERSONATION

#if FEATURE_REMOTING   

        // This returns the exposed context for a given context ID.
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern Context GetContextInternal(IntPtr id);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern Object InternalCrossContextCallback(Context ctx, IntPtr ctxID, Int32 appDomainID, InternalCrossContextDelegate ftnToCall, Object[] args);

        [System.Security.SecurityCritical]  // auto-generated
        internal Object InternalCrossContextCallback(Context ctx, InternalCrossContextDelegate ftnToCall, Object[] args)
        {
            return InternalCrossContextCallback(ctx, ctx.InternalContextID, 0, ftnToCall, args);
        }

        // CompleteCrossContextCallback is called by the EE after transitioning to the requested context
        private static Object CompleteCrossContextCallback(InternalCrossContextDelegate ftnToCall, Object[] args)
        {
            return ftnToCall(args);
        }
#endif // FEATURE_REMOTING

        /*======================================================================
        ** Returns the current domain in which current thread is running.
        ======================================================================*/

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern AppDomain GetDomainInternal();
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern AppDomain GetFastDomainInternal();

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static AppDomain GetDomain()
        {
            Contract.Ensures(Contract.Result<AppDomain>() != null);


            AppDomain ad;
            ad = GetFastDomainInternal();
            if (ad == null)
                ad = GetDomainInternal();

#if FEATURE_REMOTING        
            Contract.Assert(CurrentThread.m_Context == null || CurrentThread.m_Context.AppDomain == ad, "AppDomains on the managed & unmanaged threads should match");
#endif
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
        public  String Name {
            get {
                return m_Name;

            }
            [System.Security.SecuritySafeCritical]  // auto-generated
            [HostProtection(ExternalThreading=true)]
            set {
                lock(this) {
                    if (m_Name != null)
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WriteOnce"));
                    m_Name = value;

                    InformThreadNameChange(GetNativeHandle(), value, (value != null) ? value.Length : 0);
                }
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void InformThreadNameChange(ThreadHandle t, String name, int len);

        internal Object AbortReason {
            [System.Security.SecurityCritical]  // auto-generated
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
            [System.Security.SecurityCritical]  // auto-generated
            set { SetAbortReason(value); }
        }

#if !FEATURE_CORECLR
        /*
         *  This marks the beginning of a critical code region.
         */
        [System.Security.SecuritySafeCritical]  // auto-generated
        [HostProtection(Synchronization=true, ExternalThreading=true)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static extern void BeginCriticalRegion();

        /*
         *  This marks the end of a critical code region.
         */
        [System.Security.SecuritySafeCritical]  // auto-generated
        [HostProtection(Synchronization=true, ExternalThreading=true)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static extern void EndCriticalRegion();

        /*
         *  This marks the beginning of a code region that requires thread affinity.
         */
        [System.Security.SecurityCritical]  // auto-generated_required
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static extern void BeginThreadAffinity();

        /*
         *  This marks the end of a code region that requires thread affinity.
         */
        [System.Security.SecurityCritical]  // auto-generated_required
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static extern void EndThreadAffinity();
#endif // !FEATURE_CORECLR

        /*=========================================================================
        ** Volatile Read & Write and MemoryBarrier methods.
        ** Provides the ability to read and write values ensuring that the values
        ** are read/written each time they are accessed.
        =========================================================================*/

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static byte VolatileRead(ref byte address)
        {
            byte ret = address;
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            return ret;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static short VolatileRead(ref short address)
        {
            short ret = address;
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            return ret;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static int VolatileRead(ref int address)
        {
            int ret = address;
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            return ret;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static long VolatileRead(ref long address)
        {
            long ret = address;
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            return ret;
        }

        [CLSCompliant(false)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static sbyte VolatileRead(ref sbyte address)
        {
            sbyte ret = address;
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            return ret;
        }

        [CLSCompliant(false)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static ushort VolatileRead(ref ushort address)
        {
            ushort ret = address;
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            return ret;
        }

        [CLSCompliant(false)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static uint VolatileRead(ref uint address)
        {
            uint ret = address;
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            return ret;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static IntPtr VolatileRead(ref IntPtr address)
        {
            IntPtr ret = address;
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            return ret;
        }

        [CLSCompliant(false)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static UIntPtr VolatileRead(ref UIntPtr address)
        {
            UIntPtr ret = address;
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            return ret;
        }

        [CLSCompliant(false)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static ulong VolatileRead(ref ulong address)
        {
            ulong ret = address;
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            return ret;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static float VolatileRead(ref float address)
        {
            float ret = address;
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            return ret;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static double VolatileRead(ref double address)
        {
            double ret = address;
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            return ret;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static Object VolatileRead(ref Object address)
        {
            Object ret = address;
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            return ret;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static void VolatileWrite(ref byte address, byte value)
        {
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            address = value;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static void VolatileWrite(ref short address, short value)
        {
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            address = value;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static void VolatileWrite(ref int address, int value)
        {
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            address = value;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static void VolatileWrite(ref long address, long value)
        {
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            address = value;
        }

        [CLSCompliant(false)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static void VolatileWrite(ref sbyte address, sbyte value)
        {
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            address = value;
        }

        [CLSCompliant(false)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static void VolatileWrite(ref ushort address, ushort value)
        {
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            address = value;
        }

        [CLSCompliant(false)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static void VolatileWrite(ref uint address, uint value)
        {
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            address = value;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static void VolatileWrite(ref IntPtr address, IntPtr value)
        {
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            address = value;
        }

        [CLSCompliant(false)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static void VolatileWrite(ref UIntPtr address, UIntPtr value)
        {
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            address = value;
        }

        [CLSCompliant(false)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static void VolatileWrite(ref ulong address, ulong value)
        {
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            address = value;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static void VolatileWrite(ref float address, float value)
        {
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            address = value;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static void VolatileWrite(ref double address, double value)
        {
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            address = value;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static void VolatileWrite(ref Object address, Object value)
        {
            MemoryBarrier(); // Call MemoryBarrier to ensure the proper semantic in a portable way.
            address = value;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
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

#if !FEATURE_CORECLR
        void _Thread.GetTypeInfoCount(out uint pcTInfo)
        {
            throw new NotImplementedException();
        }

        void _Thread.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
        {
            throw new NotImplementedException();
        }

        void _Thread.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
        {
            throw new NotImplementedException();
        }

        void _Thread.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
        {
            throw new NotImplementedException();
        }
#endif

        // Helper function to set the AbortReason for a thread abort.
        //  Checks that they're not alredy set, and then atomically updates
        //  the reason info (object + ADID).
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern void SetAbortReason(Object o);
    
        // Helper function to retrieve the AbortReason from a thread
        //  abort.  Will perform cross-AppDomain marshalling if the object
        //  lives in a different AppDomain from the requester.
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern Object GetAbortReason();
    
        // Helper function to clear the AbortReason.  Takes care of
        //  AppDomain related cleanup if required.
        [System.Security.SecurityCritical]  // auto-generated
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
