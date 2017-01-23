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
    using System.Collections;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.ExceptionServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using System.Diagnostics;
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

#if FEATURE_COMPRESSEDSTACK
    internal struct SecurityContextSwitcher: IDisposable
    {
        internal SecurityContext.Reader prevSC; // prev SC that we restore on an Undo
        internal SecurityContext currSC; //current SC  - SetSecurityContext that created the switcher set this on the Thread
        internal ExecutionContext currEC; // current ExecutionContext on Thread
        internal CompressedStackSwitcher cssw;

        public void Dispose()
        {
            Undo();
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [HandleProcessCorruptedStateExceptions] 
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

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [HandleProcessCorruptedStateExceptions] 
        public void Undo()
        {        
            if (currSC == null) 
            {
                return; // mutiple Undo()s called on this switcher object
            }  

            if (currEC != null)
            {
                Debug.Assert(currEC == Thread.CurrentThread.GetMutableExecutionContext(), "SecurityContextSwitcher used from another thread");
                Debug.Assert(currSC == currEC.SecurityContext, "SecurityContextSwitcher context mismatch");
            
                // restore the saved security context 
                currEC.SecurityContext = prevSC.DangerousGetRawSecurityContext();
            }
            else
            {
                // caller must have already restored the ExecutionContext
                Debug.Assert(Thread.CurrentThread.GetExecutionContextReader().SecurityContext.IsSame(prevSC));
            }

            currSC = null; // this will prevent the switcher object being used again        

            bool bNoException = true;

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
        /*=========================================================================
        ** Data accessed from managed code that needs to be defined in 
        ** SecurityContextObject  to maintain alignment between the two classes.
        ** DON'T CHANGE THESE UNLESS YOU MODIFY SecurityContextObject in vm\object.h
        =========================================================================*/
        
        private ExecutionContext            _executionContext;
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
        }

        public static AsyncFlowControl SuppressFlow()
        {
            return SuppressFlow(SecurityContextDisableFlow.All);
        }
        
        public static AsyncFlowControl SuppressFlowWindowsIdentity()
        {
            return SuppressFlow(SecurityContextDisableFlow.WI);
        }

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

        internal static bool IsFlowSuppressed(SecurityContextDisableFlow flags)
        {           
            return Thread.CurrentThread.GetExecutionContextReader().SecurityContext.IsFlowSuppressed(flags);
        }

        // This method is special from a security perspective - the VM will not allow a stack walk to
        // continue past the call to SecurityContext.Run.  If you change the signature to this method, or
        // provide an alternate way to do a SecurityContext.Run make sure to update
        // SecurityStackWalk::IsSpecialRunFrame in the VM to search for the new method.
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
                    Debug.Assert(GetCurrentWI(Thread.CurrentThread.GetExecutionContextReader()) == null);
                }
            }
            else
            {
                RunInternal(securityContext, callback, state);
            }

        }
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

        static internal void runTryCode(Object userData)
        {
            SecurityContextRunData rData = (SecurityContextRunData) userData;
            rData.scsw = SetSecurityContext(rData.sc, Thread.CurrentThread.GetExecutionContextReader().SecurityContext, modifyCurrentExecutionContext: true);
            rData.callBack(rData.state);
            
        }

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
        [DynamicSecurityMethodAttribute()]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        internal static SecurityContextSwitcher SetSecurityContext(SecurityContext sc, SecurityContext.Reader prevSecurityContext, bool modifyCurrentExecutionContext)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return SetSecurityContext(sc, prevSecurityContext, modifyCurrentExecutionContext, ref stackMark);
        }

        [HandleProcessCorruptedStateExceptions] 
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
        public SecurityContext CreateCopy()
        {
            if (!isNewCapture)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotNewCaptureContext"));
            }                                

            SecurityContext sc = new SecurityContext();
            sc.isNewCapture = true;
            sc._disableFlow = _disableFlow;

            if (_compressedStack != null)
                sc._compressedStack = _compressedStack.CreateCopy();

            return sc;
        }

        /// <internalonly/>
        internal SecurityContext CreateMutableCopy()
        {
            Debug.Assert(!this.isNewCapture);

            SecurityContext sc = new SecurityContext();
            sc._disableFlow = this._disableFlow;

            if (this._compressedStack != null)
                sc._compressedStack = this._compressedStack.CreateCopy();

            return sc;
        }

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

        static private SecurityContext CaptureCore(ExecutionContext.Reader currThreadEC, ref StackCrawlMark stackMark)
        {
            SecurityContext sc = new SecurityContext();
            sc.isNewCapture = true;

            // Force create CompressedStack
            sc.CompressedStack = CompressedStack.GetCompressedStack(ref stackMark);
            return sc;
        }

        static internal SecurityContext CreateFullTrustSecurityContext()
        {
            SecurityContext sc = new SecurityContext();
            sc.isNewCapture = true;

            // Force create CompressedStack
            sc.CompressedStack = new CompressedStack(null);
            return sc;
        }

        internal bool IsDefaultFTSecurityContext()
        {
            return (CompressedStack == null || CompressedStack.CompressedStackHandle == null);
        }
        static internal bool CurrentlyInDefaultFTSecurityContext(ExecutionContext threadEC)
        {
            return (IsDefaultThreadSecurityInfo());
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal extern static bool IsDefaultThreadSecurityInfo();
    }
#endif // FEATURE_COMPRESSEDSTACK
}
