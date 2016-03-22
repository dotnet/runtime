// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*============================================================
**
** 
** 
**
**
** Purpose: Capture security  context for a thread
**
** 
===========================================================*/
namespace System.Security
{    
    using Microsoft.Win32;
    using Microsoft.Win32.SafeHandles;
    using System.Threading;
    using System.Runtime.Remoting;
#if FEATURE_IMPERSONATION
    using System.Security.Principal;
#endif
    using System.Collections;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
#if FEATURE_CORRUPTING_EXCEPTIONS
    using System.Runtime.ExceptionServices;
#endif // FEATURE_CORRUPTING_EXCEPTIONS
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    // This enum must be kept in sync with the SecurityContextSource enum in the VM
    public enum SecurityContextSource
    {
        CurrentAppDomain = 0,
        CurrentAssembly
    }

    internal enum SecurityContextDisableFlow
    {
        Nothing = 0,
        WI = 0x1,
        All = 0x3FFF
    }

#if FEATURE_IMPERSONATION
    internal enum WindowsImpersonationFlowMode { 
    IMP_FASTFLOW = 0,
       IMP_NOFLOW = 1,
       IMP_ALWAYSFLOW = 2,
       IMP_DEFAULT = IMP_FASTFLOW 
    }
#endif

#if FEATURE_COMPRESSEDSTACK
    internal struct SecurityContextSwitcher: IDisposable
    {
        internal SecurityContext.Reader prevSC; // prev SC that we restore on an Undo
        internal SecurityContext currSC; //current SC  - SetSecurityContext that created the switcher set this on the Thread
        internal ExecutionContext currEC; // current ExecutionContext on Thread
        internal CompressedStackSwitcher cssw;
#if FEATURE_IMPERSONATION
        internal WindowsImpersonationContext wic;
#endif

        [System.Security.SecuritySafeCritical] // overrides public transparent member
        public void Dispose()
        {
            Undo();
        }

        [System.Security.SecurityCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#if FEATURE_CORRUPTING_EXCEPTIONS
        [HandleProcessCorruptedStateExceptions] 
#endif // FEATURE_CORRUPTING_EXCEPTIONS
        internal bool UndoNoThrow()
        {
            try
            {
                Undo();
            }
            catch
            {
                return false;
            }
            return true;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#if FEATURE_CORRUPTING_EXCEPTIONS
        [HandleProcessCorruptedStateExceptions] 
#endif // FEATURE_CORRUPTING_EXCEPTIONS
        public void Undo()
        {        
            if (currSC == null) 
            {
                return; // mutiple Undo()s called on this switcher object
            }  

            if (currEC != null)
            {
                Contract.Assert(currEC == Thread.CurrentThread.GetMutableExecutionContext(), "SecurityContextSwitcher used from another thread");
                Contract.Assert(currSC == currEC.SecurityContext, "SecurityContextSwitcher context mismatch");
            
                // restore the saved security context 
                currEC.SecurityContext = prevSC.DangerousGetRawSecurityContext();
            }
            else
            {
                // caller must have already restored the ExecutionContext
                Contract.Assert(Thread.CurrentThread.GetExecutionContextReader().SecurityContext.IsSame(prevSC));
            }

            currSC = null; // this will prevent the switcher object being used again        

            bool bNoException = true;
#if FEATURE_IMPERSONATION
            try 
            {
                if (wic != null)
                    bNoException &= wic.UndoNoThrow();
            }
            catch
            {
                // Failfast since we can't continue safely...
                bNoException &= cssw.UndoNoThrow();
                System.Environment.FailFast(Environment.GetResourceString("ExecutionContext_UndoFailed"));
                
            }
#endif
            bNoException &= cssw.UndoNoThrow();


            if (!bNoException)
            {
                // Failfast since we can't continue safely...
                System.Environment.FailFast(Environment.GetResourceString("ExecutionContext_UndoFailed"));                
            }

        }
    }
    

    public sealed class SecurityContext : IDisposable 
    {
#if FEATURE_IMPERSONATION
        // Note that only one of the following variables will be true. The way we set up the flow mode in the g_pConfig guarantees this.
        static bool _LegacyImpersonationPolicy = (GetImpersonationFlowMode() == WindowsImpersonationFlowMode.IMP_NOFLOW);
        static bool _alwaysFlowImpersonationPolicy = (GetImpersonationFlowMode() == WindowsImpersonationFlowMode.IMP_ALWAYSFLOW);
#endif
        /*=========================================================================
        ** Data accessed from managed code that needs to be defined in 
        ** SecurityContextObject  to maintain alignment between the two classes.
        ** DON'T CHANGE THESE UNLESS YOU MODIFY SecurityContextObject in vm\object.h
        =========================================================================*/
        
        private ExecutionContext            _executionContext;
#if FEATURE_IMPERSONATION
        private volatile WindowsIdentity             _windowsIdentity;
#endif
        private volatile CompressedStack          _compressedStack;
        static private volatile SecurityContext _fullTrustSC;
        
        internal volatile bool isNewCapture = false;
        internal volatile SecurityContextDisableFlow _disableFlow = SecurityContextDisableFlow.Nothing;
                
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal SecurityContext()
        {
        }

        internal struct Reader
        {
            SecurityContext m_sc;

            public Reader(SecurityContext sc) { m_sc = sc; }

            public SecurityContext DangerousGetRawSecurityContext() { return m_sc; }

            public bool IsNull { get { return m_sc == null; } }
            public bool IsSame(SecurityContext sc) { return m_sc == sc; }
            public bool IsSame(SecurityContext.Reader sc) { return m_sc == sc.m_sc; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsFlowSuppressed(SecurityContextDisableFlow flags)
            {           
                return (m_sc == null) ? false : ((m_sc._disableFlow & flags) == flags);
            }
        
            public CompressedStack CompressedStack { get { return IsNull ? null : m_sc.CompressedStack; } }

            public WindowsIdentity WindowsIdentity 
            { 
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return IsNull ? null : m_sc.WindowsIdentity; } 
            }
        }
        
            
        static internal SecurityContext FullTrustSecurityContext
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (_fullTrustSC == null)
                    _fullTrustSC = CreateFullTrustSecurityContext();
                return _fullTrustSC;
            }
        }

        // link the security context to an ExecutionContext
        internal ExecutionContext ExecutionContext
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            set
            {
                _executionContext = value;
            }
        }
                
#if FEATURE_IMPERSONATION


        internal WindowsIdentity WindowsIdentity 
        {
            get 
            {
                return _windowsIdentity;
            }
            set
            {
                // Note, we do not dispose of the existing windows identity, since some code such as remoting
                // relies on reusing that identity.  If you are not going to reuse the existing identity, then
                // you should dispose of the existing identity before resetting it.
                    _windowsIdentity = value;
            }
        }
#endif // FEATURE_IMPERSONATION

              
        internal CompressedStack CompressedStack
        {
            get
            {
                return _compressedStack; 
            }
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            set
            {
                _compressedStack =  value;                    
            }
        }

        public void Dispose()
        {
#if FEATURE_IMPERSONATION
            if (_windowsIdentity != null)
                _windowsIdentity.Dispose();
#endif // FEATURE_IMPERSONATION
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public static AsyncFlowControl SuppressFlow()
        {
            return SuppressFlow(SecurityContextDisableFlow.All);
        }
        
        [System.Security.SecurityCritical]  // auto-generated_required
        public static AsyncFlowControl SuppressFlowWindowsIdentity()
        {
            return SuppressFlow(SecurityContextDisableFlow.WI);
        }

        [SecurityCritical]
        internal static AsyncFlowControl SuppressFlow(SecurityContextDisableFlow flags)
        {
            if (IsFlowSuppressed(flags))
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotSupressFlowMultipleTimes"));
            }

            ExecutionContext ec = Thread.CurrentThread.GetMutableExecutionContext();
            if (ec.SecurityContext == null)
                ec.SecurityContext = new SecurityContext();
            AsyncFlowControl afc = new AsyncFlowControl();
            afc.Setup(flags);
            return afc;
        }

        [SecuritySafeCritical]
        public static void RestoreFlow()
        {
            SecurityContext sc = Thread.CurrentThread.GetMutableExecutionContext().SecurityContext;
            if (sc == null || sc._disableFlow == SecurityContextDisableFlow.Nothing)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotRestoreUnsupressedFlow"));
            }
            sc._disableFlow = SecurityContextDisableFlow.Nothing;        
        }

        public static bool IsFlowSuppressed()
        {
            return SecurityContext.IsFlowSuppressed(SecurityContextDisableFlow.All);
        }
#if FEATURE_IMPERSONATION
        public static bool IsWindowsIdentityFlowSuppressed()
        {
            return (_LegacyImpersonationPolicy|| SecurityContext.IsFlowSuppressed(SecurityContextDisableFlow.WI));
        }
#endif        
        [SecuritySafeCritical]
        internal static bool IsFlowSuppressed(SecurityContextDisableFlow flags)
        {           
            return Thread.CurrentThread.GetExecutionContextReader().SecurityContext.IsFlowSuppressed(flags);
        }

        // This method is special from a security perspective - the VM will not allow a stack walk to
        // continue past the call to SecurityContext.Run.  If you change the signature to this method, or
        // provide an alternate way to do a SecurityContext.Run make sure to update
        // SecurityStackWalk::IsSpecialRunFrame in the VM to search for the new method.
        [System.Security.SecurityCritical]  // auto-generated_required
        [DynamicSecurityMethodAttribute()]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static void Run(SecurityContext securityContext, ContextCallback callback, Object state)
        {
            if (securityContext == null )
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NullContext"));
            }
            Contract.EndContractBlock();

            StackCrawlMark stackMark = StackCrawlMark.LookForMe;

            if (!securityContext.isNewCapture)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotNewCaptureContext"));
            }

            securityContext.isNewCapture = false;

            ExecutionContext.Reader ec = Thread.CurrentThread.GetExecutionContextReader();
            
            // Optimization: do the callback directly if both the current and target contexts are equal to the
            // default full-trust security context
            if ( SecurityContext.CurrentlyInDefaultFTSecurityContext(ec)
                && securityContext.IsDefaultFTSecurityContext())
            {
                callback(state);
                
                if (GetCurrentWI(Thread.CurrentThread.GetExecutionContextReader()) != null) 
                {
                    // If we enter here it means the callback did an impersonation
                    // that we need to revert.
                    // We don't need to revert any other security state since it is stack-based 
                    // and automatically goes away when the callback returns.
                    WindowsIdentity.SafeRevertToSelf(ref stackMark);
                    // Ensure we have reverted to the state we entered in.
                    Contract.Assert(GetCurrentWI(Thread.CurrentThread.GetExecutionContextReader()) == null);
                }
            }
            else
            {
                RunInternal(securityContext, callback, state);
            }

        }
        [System.Security.SecurityCritical]  // auto-generated
        internal static void RunInternal(SecurityContext securityContext, ContextCallback callBack, Object state)
        {
            if (cleanupCode == null)
            {
                tryCode = new RuntimeHelpers.TryCode(runTryCode);
                cleanupCode = new RuntimeHelpers.CleanupCode(runFinallyCode);
            }
            SecurityContextRunData runData = new SecurityContextRunData(securityContext, callBack, state);
            RuntimeHelpers.ExecuteCodeWithGuaranteedCleanup(tryCode, cleanupCode, runData);

        }
        
        internal class SecurityContextRunData
        {
            internal SecurityContext sc;
            internal ContextCallback callBack;
            internal Object state;
            internal SecurityContextSwitcher scsw;
            internal SecurityContextRunData(SecurityContext securityContext, ContextCallback cb, Object state)
            {
                this.sc = securityContext;
                this.callBack = cb;
                this.state = state;
                this.scsw = new SecurityContextSwitcher();
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        static internal void runTryCode(Object userData)
        {
            SecurityContextRunData rData = (SecurityContextRunData) userData;
            rData.scsw = SetSecurityContext(rData.sc, Thread.CurrentThread.GetExecutionContextReader().SecurityContext, modifyCurrentExecutionContext: true);
            rData.callBack(rData.state);
            
        }

        [System.Security.SecurityCritical]  // auto-generated
        [PrePrepareMethod]
        static internal void runFinallyCode(Object userData, bool exceptionThrown)
        {
            SecurityContextRunData rData = (SecurityContextRunData) userData;
            rData.scsw.Undo();
        }
                    
        static volatile internal RuntimeHelpers.TryCode tryCode;
        static volatile internal RuntimeHelpers.CleanupCode cleanupCode;



        // Internal API that gets called from public SetSecurityContext and from SetExecutionContext
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [System.Security.SecurityCritical]  // auto-generated
        [DynamicSecurityMethodAttribute()]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        internal static SecurityContextSwitcher SetSecurityContext(SecurityContext sc, SecurityContext.Reader prevSecurityContext, bool modifyCurrentExecutionContext)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return SetSecurityContext(sc, prevSecurityContext, modifyCurrentExecutionContext, ref stackMark);
        }

        [System.Security.SecurityCritical]  // auto-generated
#if FEATURE_CORRUPTING_EXCEPTIONS
        [HandleProcessCorruptedStateExceptions] 
#endif // FEATURE_CORRUPTING_EXCEPTIONS
        internal static SecurityContextSwitcher SetSecurityContext(SecurityContext sc, SecurityContext.Reader prevSecurityContext, bool modifyCurrentExecutionContext, ref StackCrawlMark stackMark)
        {
            // Save the flow state at capture and reset it in the SC.
            SecurityContextDisableFlow _capturedFlowState = sc._disableFlow;
            sc._disableFlow = SecurityContextDisableFlow.Nothing;
            
            //Set up the switcher object
            SecurityContextSwitcher scsw = new SecurityContextSwitcher();
            scsw.currSC = sc;   
            scsw.prevSC = prevSecurityContext;

            if (modifyCurrentExecutionContext)
            {
                // save the current Execution Context
                ExecutionContext currEC = Thread.CurrentThread.GetMutableExecutionContext();
                scsw.currEC = currEC;
                currEC.SecurityContext = sc;
            }

            if (sc != null)
            {
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
#if FEATURE_IMPERSONATION
                    scsw.wic = null;
                    if (!_LegacyImpersonationPolicy)
                    {
                        if (sc.WindowsIdentity != null)
                        {
                            scsw.wic = sc.WindowsIdentity.Impersonate(ref stackMark);
                        }
                        else if ( ((_capturedFlowState & SecurityContextDisableFlow.WI) == 0) 
                            && prevSecurityContext.WindowsIdentity != null)
                        {
                            // revert impersonation if there was no WI flow supression at capture and we're currently impersonating
                            scsw.wic = WindowsIdentity.SafeRevertToSelf(ref stackMark); 
                        }
                    }
#endif
                    scsw.cssw = CompressedStack.SetCompressedStack(sc.CompressedStack, prevSecurityContext.CompressedStack);
                }
                catch 
                {
                    scsw.UndoNoThrow();
                    throw;
                }      
            }
            return scsw;
        }

        /// <internalonly/>
        [System.Security.SecuritySafeCritical]  // auto-generated
        public SecurityContext CreateCopy()
        {
            if (!isNewCapture)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotNewCaptureContext"));
            }                                

            SecurityContext sc = new SecurityContext();
            sc.isNewCapture = true;
            sc._disableFlow = _disableFlow;

#if FEATURE_IMPERSONATION
            if (WindowsIdentity != null)
                sc._windowsIdentity = new WindowsIdentity(WindowsIdentity.AccessToken);
#endif //FEATURE_IMPERSONATION

            if (_compressedStack != null)
                sc._compressedStack = _compressedStack.CreateCopy();

            return sc;
        }

        /// <internalonly/>
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal SecurityContext CreateMutableCopy()
        {
            Contract.Assert(!this.isNewCapture);

            SecurityContext sc = new SecurityContext();
            sc._disableFlow = this._disableFlow;

#if FEATURE_IMPERSONATION
            if (this.WindowsIdentity != null)
                sc._windowsIdentity = new WindowsIdentity(this.WindowsIdentity.AccessToken);
#endif //FEATURE_IMPERSONATION

            if (this._compressedStack != null)
                sc._compressedStack = this._compressedStack.CreateCopy();

            return sc;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static SecurityContext Capture( )
        {
            // check to see if Flow is suppressed
            if (IsFlowSuppressed()) 
                return null;

            StackCrawlMark stackMark= StackCrawlMark.LookForMyCaller;
            SecurityContext sc = SecurityContext.Capture(Thread.CurrentThread.GetExecutionContextReader(), ref stackMark);
            if (sc == null)
                sc = CreateFullTrustSecurityContext();
            return sc;
         }

        // create a clone from a non-existing SecurityContext
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal SecurityContext Capture(ExecutionContext.Reader currThreadEC, ref StackCrawlMark stackMark)
        {
            // check to see if Flow is suppressed
            if (currThreadEC.SecurityContext.IsFlowSuppressed(SecurityContextDisableFlow.All)) 
                return null;
        
            // If we're in FT right now, return null
            if (CurrentlyInDefaultFTSecurityContext(currThreadEC))
                return null;

            return CaptureCore(currThreadEC, ref stackMark);
        }

        [System.Security.SecurityCritical]  // auto-generated
        static private SecurityContext CaptureCore(ExecutionContext.Reader currThreadEC, ref StackCrawlMark stackMark)
        {
            SecurityContext sc = new SecurityContext();
            sc.isNewCapture = true;

#if FEATURE_IMPERSONATION
                // Force create WindowsIdentity
            if (!IsWindowsIdentityFlowSuppressed())
            {
                WindowsIdentity currentIdentity = GetCurrentWI(currThreadEC);
                if (currentIdentity != null)
                    sc._windowsIdentity = new WindowsIdentity(currentIdentity.AccessToken);
            }
            else
            {
                sc._disableFlow = SecurityContextDisableFlow.WI;
            }
#endif // FEATURE_IMPERSONATION

            // Force create CompressedStack
            sc.CompressedStack = CompressedStack.GetCompressedStack(ref stackMark);
            return sc;
        }
        [System.Security.SecurityCritical]  // auto-generated
        static internal SecurityContext CreateFullTrustSecurityContext()
        {
            SecurityContext sc = new SecurityContext();
            sc.isNewCapture = true;
        
#if FEATURE_IMPERSONATION
            if (IsWindowsIdentityFlowSuppressed())
            {
                sc._disableFlow = SecurityContextDisableFlow.WI;
            }
#endif // FEATURE_IMPERSONATION
        

            // Force create CompressedStack
            sc.CompressedStack = new CompressedStack(null);
            return sc;
        }

#if FEATURE_IMPERSONATION

    static internal bool AlwaysFlowImpersonationPolicy { get { return _alwaysFlowImpersonationPolicy; } }

        // Check to see if we have a WI on the thread and return if we do
    [System.Security.SecurityCritical]  // auto-generated
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static internal WindowsIdentity GetCurrentWI(ExecutionContext.Reader threadEC)
    {
        return GetCurrentWI(threadEC, _alwaysFlowImpersonationPolicy);
    }

    [System.Security.SecurityCritical]  // auto-generated
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static internal WindowsIdentity GetCurrentWI(ExecutionContext.Reader threadEC, bool cachedAlwaysFlowImpersonationPolicy)
    {
        Contract.Assert(cachedAlwaysFlowImpersonationPolicy == _alwaysFlowImpersonationPolicy);
        if (cachedAlwaysFlowImpersonationPolicy)
        {
            // Examine the threadtoken at the cost of a kernel call if the user has set the IMP_ALWAYSFLOW mode
            return WindowsIdentity.GetCurrentInternal(TokenAccessLevels.MaximumAllowed, true);
        }

        return threadEC.SecurityContext.WindowsIdentity;
    }

    [System.Security.SecurityCritical]
    static internal void RestoreCurrentWI(ExecutionContext.Reader currentEC, ExecutionContext.Reader prevEC, WindowsIdentity targetWI, bool cachedAlwaysFlowImpersonationPolicy)
    {
        Contract.Assert(currentEC.IsSame(Thread.CurrentThread.GetExecutionContextReader()));
        Contract.Assert(cachedAlwaysFlowImpersonationPolicy == _alwaysFlowImpersonationPolicy);

        // NOTE: cachedAlwaysFlowImpersonationPolicy is a perf optimization to avoid always having to access a static variable here.
        if (cachedAlwaysFlowImpersonationPolicy || prevEC.SecurityContext.WindowsIdentity != targetWI)
        {
            //
            // Either we're always flowing, or the target WI was obtained from the current EC in the first place.
            //
            Contract.Assert(_alwaysFlowImpersonationPolicy || currentEC.SecurityContext.WindowsIdentity == targetWI);

            RestoreCurrentWIInternal(targetWI);
        }
    }

    [System.Security.SecurityCritical]
    static private void RestoreCurrentWIInternal(WindowsIdentity targetWI)
    {
        int hr = Win32.RevertToSelf();
        if (hr < 0)
            Environment.FailFast(Win32Native.GetMessage(hr));

        if (targetWI != null)
        {   
            SafeAccessTokenHandle tokenHandle = targetWI.AccessToken;
            if (tokenHandle != null && !tokenHandle.IsInvalid)
            {
                hr = Win32.ImpersonateLoggedOnUser(tokenHandle);
                if (hr < 0)
                    Environment.FailFast(Win32Native.GetMessage(hr));
            }                
        }
    }

    [System.Security.SecurityCritical]  // auto-generated
    internal bool IsDefaultFTSecurityContext()
    {
        return (WindowsIdentity == null && (CompressedStack == null || CompressedStack.CompressedStackHandle == null));
    }
    [System.Security.SecurityCritical]  // auto-generated
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static internal bool CurrentlyInDefaultFTSecurityContext(ExecutionContext.Reader threadEC)
    {
        return (IsDefaultThreadSecurityInfo() && GetCurrentWI(threadEC) == null);
    }
#else
        
        internal bool IsDefaultFTSecurityContext()
        {
            return (CompressedStack == null || CompressedStack.CompressedStackHandle == null);
        }
        static internal bool CurrentlyInDefaultFTSecurityContext(ExecutionContext threadEC)
        {
            return (IsDefaultThreadSecurityInfo());
        }
#endif
#if FEATURE_IMPERSONATION
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal extern static WindowsImpersonationFlowMode GetImpersonationFlowMode();
#endif
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal extern static bool IsDefaultThreadSecurityInfo();
        
    }
#endif // FEATURE_COMPRESSEDSTACK
}
