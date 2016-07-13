// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*============================================================
**
**
**
** Purpose: Capture execution  context for a thread
**
** 
===========================================================*/
namespace System.Threading
{    
    using System;
    using System.Security;
    using System.Runtime.Remoting;
#if FEATURE_IMPERSONATION
    using System.Security.Principal;
#endif
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.ExceptionServices;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
#if FEATURE_REMOTING
    using System.Runtime.Remoting.Messaging;
#endif // FEATURE_REMOTING
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Diagnostics.Contracts;
    using System.Diagnostics.CodeAnalysis;

#if FEATURE_CORECLR
    [System.Security.SecurityCritical] // auto-generated
#endif
    [System.Runtime.InteropServices.ComVisible(true)]
    public delegate void ContextCallback(Object state);

#if FEATURE_CORECLR

    [SecurityCritical]
    internal struct ExecutionContextSwitcher
    {
        internal ExecutionContext m_ec;
        internal SynchronizationContext m_sc;

        internal void Undo(Thread currentThread)
        {
            Contract.Assert(currentThread == Thread.CurrentThread);

            // The common case is that these have not changed, so avoid the cost of a write if not needed.
            if (currentThread.SynchronizationContext != m_sc)
            {
                currentThread.SynchronizationContext = m_sc;
            }
            
            if (currentThread.ExecutionContext != m_ec)
            {
                ExecutionContext.Restore(currentThread, m_ec);
            }
        }
    }

    public sealed class ExecutionContext : IDisposable
    {
        private static readonly ExecutionContext Default = new ExecutionContext();

        private readonly Dictionary<IAsyncLocal, object> m_localValues;
        private readonly IAsyncLocal[] m_localChangeNotifications;

        private ExecutionContext()
        {
            m_localValues = new Dictionary<IAsyncLocal, object>();
            m_localChangeNotifications = Array.Empty<IAsyncLocal>();
        }

        private ExecutionContext(Dictionary<IAsyncLocal, object> localValues, IAsyncLocal[] localChangeNotifications)
        {
            m_localValues = localValues;
            m_localChangeNotifications = localChangeNotifications;
        }

        [SecuritySafeCritical]
        public static ExecutionContext Capture()
        {
            return Thread.CurrentThread.ExecutionContext ?? ExecutionContext.Default;
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        public static void Run(ExecutionContext executionContext, ContextCallback callback, Object state)
        {
            if (executionContext == null)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NullContext"));

            Thread currentThread = Thread.CurrentThread;
            ExecutionContextSwitcher ecsw = default(ExecutionContextSwitcher);
            try
            {
                EstablishCopyOnWriteScope(currentThread, ref ecsw);
                ExecutionContext.Restore(currentThread, executionContext);
                callback(state);
            }
            catch
            {
                // Note: we have a "catch" rather than a "finally" because we want
                // to stop the first pass of EH here.  That way we can restore the previous
                // context before any of our callers' EH filters run.  That means we need to 
                // end the scope separately in the non-exceptional case below.
                ecsw.Undo(currentThread);
                throw;
            }
            ecsw.Undo(currentThread);
        }

        [SecurityCritical]
        internal static void Restore(Thread currentThread, ExecutionContext executionContext)
        {
            Contract.Assert(currentThread == Thread.CurrentThread);

            ExecutionContext previous = currentThread.ExecutionContext ?? Default;
            currentThread.ExecutionContext = executionContext;
            
            // New EC could be null if that's what ECS.Undo saved off.
            // For the purposes of dealing with context change, treat this as the default EC
            executionContext = executionContext ?? Default;
            
            if (previous != executionContext)
            {
                OnContextChanged(previous, executionContext);
            }
        }

        [SecurityCritical]
        static internal void EstablishCopyOnWriteScope(Thread currentThread, ref ExecutionContextSwitcher ecsw)
        {
            Contract.Assert(currentThread == Thread.CurrentThread);
            
            ecsw.m_ec = currentThread.ExecutionContext; 
            ecsw.m_sc = currentThread.SynchronizationContext;
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        private static void OnContextChanged(ExecutionContext previous, ExecutionContext current)
        {
            Contract.Assert(previous != null);
            Contract.Assert(current != null);
            Contract.Assert(previous != current);
            
            foreach (IAsyncLocal local in previous.m_localChangeNotifications)
            {
                object previousValue;
                object currentValue;
                previous.m_localValues.TryGetValue(local, out previousValue);
                current.m_localValues.TryGetValue(local, out currentValue);

                if (previousValue != currentValue)
                    local.OnValueChanged(previousValue, currentValue, true);
            }

            if (current.m_localChangeNotifications != previous.m_localChangeNotifications)
            {
                try
                {
                    foreach (IAsyncLocal local in current.m_localChangeNotifications)
                    {
                        // If the local has a value in the previous context, we already fired the event for that local
                        // in the code above.
                        object previousValue;
                        if (!previous.m_localValues.TryGetValue(local, out previousValue))
                        {
                            object currentValue;
                            current.m_localValues.TryGetValue(local, out currentValue);

                            if (previousValue != currentValue)
                                local.OnValueChanged(previousValue, currentValue, true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Environment.FailFast(
                        Environment.GetResourceString("ExecutionContext_ExceptionInAsyncLocalNotification"), 
                        ex);
                }
            }        
        }

        [SecurityCritical]
        internal static object GetLocalValue(IAsyncLocal local)
        {
            ExecutionContext current = Thread.CurrentThread.ExecutionContext;
            if (current == null)
                return null;

            object value;
            current.m_localValues.TryGetValue(local, out value);
            return value;
        }

        [SecurityCritical]
        internal static void SetLocalValue(IAsyncLocal local, object newValue, bool needChangeNotifications)
        {
            ExecutionContext current = Thread.CurrentThread.ExecutionContext ?? ExecutionContext.Default;

            object previousValue;
            bool hadPreviousValue = current.m_localValues.TryGetValue(local, out previousValue);

            if (previousValue == newValue)
                return;

            //
            // Allocate a new Dictionary containing a copy of the old values, plus the new value.  We have to do this manually to 
            // minimize allocations of IEnumerators, etc.
            //
            Dictionary<IAsyncLocal, object> newValues = new Dictionary<IAsyncLocal, object>(current.m_localValues.Count + (hadPreviousValue ? 0 : 1));

            foreach (KeyValuePair<IAsyncLocal, object> pair in current.m_localValues)
                newValues.Add(pair.Key, pair.Value);

            newValues[local] = newValue;

            //
            // Either copy the change notification array, or create a new one, depending on whether we need to add a new item.
            //
            IAsyncLocal[] newChangeNotifications = current.m_localChangeNotifications;
            if (needChangeNotifications)
            {
                if (hadPreviousValue)
                {
                    Contract.Assert(Array.IndexOf(newChangeNotifications, local) >= 0);
                }
                else
                {
                    int newNotificationIndex = newChangeNotifications.Length;
                    Array.Resize(ref newChangeNotifications, newNotificationIndex + 1);
                    newChangeNotifications[newNotificationIndex] = local;
                }
            }

            Thread.CurrentThread.ExecutionContext = new ExecutionContext(newValues, newChangeNotifications);

            if (needChangeNotifications)
            {
                local.OnValueChanged(previousValue, newValue, false);
            }
        }

    #region Wrappers for CLR compat, to avoid ifdefs all over the BCL

        [Flags]
        internal enum CaptureOptions
        {
            None = 0x00,
            IgnoreSyncCtx = 0x01,
            OptimizeDefaultCase = 0x02,
        }

        [SecurityCritical]
        internal static ExecutionContext Capture(ref StackCrawlMark stackMark, CaptureOptions captureOptions)
        {
            return Capture();
        }

        [SecuritySafeCritical]
        [FriendAccessAllowed]
        internal static ExecutionContext FastCapture()
        {
            return Capture();
        }

        [SecurityCritical]
        [FriendAccessAllowed]
        internal static void Run(ExecutionContext executionContext, ContextCallback callback, Object state, bool preserveSyncCtx)
        {
            Run(executionContext, callback, state);
        }

        [SecurityCritical]
        internal bool IsDefaultFTContext(bool ignoreSyncCtx)
        {
            return this == Default;
        }

        [SecuritySafeCritical]
        public ExecutionContext CreateCopy()
        {
            return this; // since CoreCLR's ExecutionContext is immutable, we don't need to create copies.
        }

        public void Dispose()
        {
            // For CLR compat only
        }

        public static bool IsFlowSuppressed()
        {
            return false;
        }

        internal static ExecutionContext PreAllocatedDefault
        {
            [SecuritySafeCritical]
            get { return ExecutionContext.Default; }
        }

        internal bool IsPreAllocatedDefault
        {
            get { return this == ExecutionContext.Default; }
        }

    #endregion
    }

#else // FEATURE_CORECLR

    // Legacy desktop ExecutionContext implementation

    internal struct ExecutionContextSwitcher
    {
        internal ExecutionContext.Reader outerEC; // previous EC we need to restore on Undo
        internal bool outerECBelongsToScope;
#if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK        
        internal SecurityContextSwitcher scsw;
#endif // #if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK
        internal Object hecsw;
#if FEATURE_IMPERSONATION
        internal WindowsIdentity wi;
        internal bool cachedAlwaysFlowImpersonationPolicy;
        internal bool wiIsValid;
#endif
        internal Thread thread;

        [System.Security.SecurityCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#if FEATURE_CORRUPTING_EXCEPTIONS
        [HandleProcessCorruptedStateExceptions]
#endif // FEATURE_CORRUPTING_EXCEPTIONS
        internal bool UndoNoThrow(Thread currentThread)
        {
            try
            {
                Undo(currentThread);
            }
            catch
            {
                return false;
            }
            return true;
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        internal void Undo(Thread currentThread)
        {
            //
            // Don't use an uninitialized switcher, or one that's already been used.
            //
            if (thread == null)
                return; // Don't do anything

            Contract.Assert(Thread.CurrentThread == this.thread);

            // 
            // Restore the HostExecutionContext before restoring the ExecutionContext.
            //
#if FEATURE_CAS_POLICY                
            if (hecsw != null)
                HostExecutionContextSwitcher.Undo(hecsw);
#endif // FEATURE_CAS_POLICY

            //
            // restore the saved Execution Context.  Note that this will also restore the 
            // SynchronizationContext, Logical/IllogicalCallContext, etc.
            //
            ExecutionContext.Reader innerEC = currentThread.GetExecutionContextReader();
            currentThread.SetExecutionContext(outerEC, outerECBelongsToScope);

#if DEBUG
            try
            {
                currentThread.ForbidExecutionContextMutation = true;
#endif

                //
                // Tell the SecurityContext to do the side-effects of restoration.
                //
#if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK
                if (scsw.currSC != null)
                {
                    // Any critical failure inside scsw will cause FailFast
                    scsw.Undo();
                }
#endif // #if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK

#if FEATURE_IMPERSONATION
                if (wiIsValid)
                    SecurityContext.RestoreCurrentWI(outerEC, innerEC, wi, cachedAlwaysFlowImpersonationPolicy);
#endif

                thread = null; // this will prevent the switcher object being used again
#if DEBUG
            }
            finally
            {
                currentThread.ForbidExecutionContextMutation = false;
            }
#endif
            ExecutionContext.OnAsyncLocalContextChanged(innerEC.DangerousGetRawExecutionContext(), outerEC.DangerousGetRawExecutionContext());
        }
    }


    public struct AsyncFlowControl: IDisposable
    {
        private bool useEC;
        private ExecutionContext _ec;
#if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK
        private SecurityContext _sc;
#endif // #if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK
        private Thread _thread;
#if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK
        [SecurityCritical]
        internal void Setup(SecurityContextDisableFlow flags)
        {
            useEC = false;
            Thread currentThread = Thread.CurrentThread;
            _sc = currentThread.GetMutableExecutionContext().SecurityContext;
            _sc._disableFlow = flags;
            _thread = currentThread;
        }
#endif // #if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK
        [SecurityCritical]
        internal void Setup()
        {
            useEC = true;
            Thread currentThread = Thread.CurrentThread;
            _ec = currentThread.GetMutableExecutionContext();
            _ec.isFlowSuppressed = true;
            _thread = currentThread;
        }
        
        public void Dispose()
        {
            Undo();
        }
        
        [SecuritySafeCritical]
        public void Undo()
        {
            if (_thread == null)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotUseAFCMultiple"));
            }  
            if (_thread != Thread.CurrentThread)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotUseAFCOtherThread"));
            }
            if (useEC) 
            {
                if (Thread.CurrentThread.GetMutableExecutionContext() != _ec)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_AsyncFlowCtrlCtxMismatch"));
                }      
                ExecutionContext.RestoreFlow();
            }
#if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK            
            else
            {
                if (!Thread.CurrentThread.GetExecutionContextReader().SecurityContext.IsSame(_sc))
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_AsyncFlowCtrlCtxMismatch"));
                }      
                SecurityContext.RestoreFlow();
            }
#endif // #if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK            
            _thread = null;
        }
        
        public override int GetHashCode()
        {
            return _thread == null ? ToString().GetHashCode() : _thread.GetHashCode();
        }
        
        public override bool Equals(Object obj)
        {
            if (obj is AsyncFlowControl)
                return Equals((AsyncFlowControl)obj);
            else
                return false;
        }
    
        public bool Equals(AsyncFlowControl obj)
        {
            return obj.useEC == useEC && obj._ec == _ec &&
#if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK                            
                obj._sc == _sc && 
#endif // #if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK
                obj._thread == _thread;
        }
    
        public static bool operator ==(AsyncFlowControl a, AsyncFlowControl b)
        {
            return a.Equals(b);
        }
        
        public static bool operator !=(AsyncFlowControl a, AsyncFlowControl b)
        {
            return !(a == b);
        }
        
    }
    

#if FEATURE_SERIALIZATION
    [Serializable]
#endif
    public sealed class ExecutionContext : IDisposable, ISerializable
    {
        /*=========================================================================
        ** Data accessed from managed code that needs to be defined in 
        ** ExecutionContextObject  to maintain alignment between the two classes.
        ** DON'T CHANGE THESE UNLESS YOU MODIFY ExecutionContextObject in vm\object.h
        =========================================================================*/
#if FEATURE_CAS_POLICY        
        private HostExecutionContext _hostExecutionContext;
#endif // FEATURE_CAS_POLICY
        private SynchronizationContext _syncContext;
        private SynchronizationContext _syncContextNoFlow;
#if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK
        private SecurityContext     _securityContext;
#endif // #if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK
#if FEATURE_REMOTING
        [System.Security.SecurityCritical] // auto-generated
        private LogicalCallContext  _logicalCallContext;
        private IllogicalCallContext _illogicalCallContext;  // this call context follows the physical thread
#endif // #if FEATURE_REMOTING

        enum Flags
        {
            None = 0x0,
            IsNewCapture = 0x1,
            IsFlowSuppressed = 0x2,
            IsPreAllocatedDefault = 0x4
        }
        private Flags _flags;

        private Dictionary<IAsyncLocal, object> _localValues;
        private List<IAsyncLocal> _localChangeNotifications;

        internal bool isNewCapture 
        { 
            get
            { 
                return (_flags & (Flags.IsNewCapture | Flags.IsPreAllocatedDefault)) != Flags.None; 
            }
            set
            {
                Contract.Assert(!IsPreAllocatedDefault);
                if (value)
                    _flags |= Flags.IsNewCapture;
                else
                    _flags &= ~Flags.IsNewCapture;
            }
        }
        internal bool isFlowSuppressed 
        { 
            get 
            { 
                return (_flags & Flags.IsFlowSuppressed) != Flags.None; 
            }
            set
            {
                Contract.Assert(!IsPreAllocatedDefault);
                if (value)
                    _flags |= Flags.IsFlowSuppressed;
                else
                    _flags &= ~Flags.IsFlowSuppressed;
            }
        }
       

        private static readonly ExecutionContext s_dummyDefaultEC = new ExecutionContext(isPreAllocatedDefault: true);

        static internal ExecutionContext PreAllocatedDefault
        {
            [SecuritySafeCritical]
            get { return s_dummyDefaultEC; }
        }

        internal bool IsPreAllocatedDefault
        {
            get
            {
                // we use _flags instead of a direct comparison w/ s_dummyDefaultEC to avoid the static access on 
                // hot code paths.
                if ((_flags & Flags.IsPreAllocatedDefault) != Flags.None)
                {
                    Contract.Assert(this == s_dummyDefaultEC);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal ExecutionContext()
        {            
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal ExecutionContext(bool isPreAllocatedDefault)
        {
            if (isPreAllocatedDefault)
                _flags = Flags.IsPreAllocatedDefault;
        }

        // Read-only wrapper around ExecutionContext.  This enables safe reading of an ExecutionContext without accidentally modifying it.
        internal struct Reader
        {
            ExecutionContext m_ec;

            public Reader(ExecutionContext ec) { m_ec = ec; }

            public ExecutionContext DangerousGetRawExecutionContext() { return m_ec; }

            public bool IsNull { get { return m_ec == null; } }
            [SecurityCritical]
            public bool IsDefaultFTContext(bool ignoreSyncCtx) { return m_ec.IsDefaultFTContext(ignoreSyncCtx); }
            public bool IsFlowSuppressed 
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return IsNull ? false : m_ec.isFlowSuppressed; } 
            }
            //public Thread Thread { get { return m_ec._thread; } }
            public bool IsSame(ExecutionContext.Reader other) { return m_ec == other.m_ec; }

            public SynchronizationContext SynchronizationContext { get { return IsNull ? null : m_ec.SynchronizationContext; } }
            public SynchronizationContext SynchronizationContextNoFlow { get { return IsNull ? null : m_ec.SynchronizationContextNoFlow; } }

#if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK
            public SecurityContext.Reader SecurityContext 
            {
                [SecurityCritical]
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return new SecurityContext.Reader(IsNull ? null : m_ec.SecurityContext); } 
            }
#endif

#if FEATURE_REMOTING
            public LogicalCallContext.Reader LogicalCallContext 
            {
                [SecurityCritical]
                get { return new LogicalCallContext.Reader(IsNull ? null : m_ec.LogicalCallContext); } 
            }

            public IllogicalCallContext.Reader IllogicalCallContext 
            {
                [SecurityCritical]
                get { return new IllogicalCallContext.Reader(IsNull ? null : m_ec.IllogicalCallContext); } 
            }
#endif

            [SecurityCritical]
            public object GetLocalValue(IAsyncLocal local)
            {
                if (IsNull)
                    return null;

                if (m_ec._localValues == null)
                    return null;

                object value;
                m_ec._localValues.TryGetValue(local, out value);
                return value;
            }

            [SecurityCritical]
            public bool HasSameLocalValues(ExecutionContext other)
            {
                var thisLocalValues = IsNull ? null : m_ec._localValues;
                var otherLocalValues = other == null ? null : other._localValues;
                return thisLocalValues == otherLocalValues;
            }

            [SecurityCritical]
            public bool HasLocalValues()
            {
                return !this.IsNull && m_ec._localValues != null;
            }
        }

        [SecurityCritical]
        internal static object GetLocalValue(IAsyncLocal local)
        {
            return Thread.CurrentThread.GetExecutionContextReader().GetLocalValue(local);
        }

        [SecurityCritical]
        internal static void SetLocalValue(IAsyncLocal local, object newValue, bool needChangeNotifications)
        {
            ExecutionContext current = Thread.CurrentThread.GetMutableExecutionContext();

            object previousValue = null;
            bool hadPreviousValue = current._localValues != null && current._localValues.TryGetValue(local, out previousValue);

            if (previousValue == newValue)
                return;

            if (current._localValues == null)
                current._localValues = new Dictionary<IAsyncLocal, object>();
            else
                current._localValues = new Dictionary<IAsyncLocal, object>(current._localValues);

            current._localValues[local] = newValue;

            if (needChangeNotifications)
            {
                if (hadPreviousValue)
                {
                    Contract.Assert(current._localChangeNotifications != null);
                    Contract.Assert(current._localChangeNotifications.Contains(local));
                }
                else
                {
                    if (current._localChangeNotifications == null)
                        current._localChangeNotifications = new List<IAsyncLocal>();
                    else
                        current._localChangeNotifications = new List<IAsyncLocal>(current._localChangeNotifications);

                    current._localChangeNotifications.Add(local);
                }

                local.OnValueChanged(previousValue, newValue, false);
            }
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        internal static void OnAsyncLocalContextChanged(ExecutionContext previous, ExecutionContext current)
        {
            List<IAsyncLocal> previousLocalChangeNotifications = (previous == null) ? null : previous._localChangeNotifications;
            if (previousLocalChangeNotifications != null)
            {
                foreach (IAsyncLocal local in previousLocalChangeNotifications)
                {
                    object previousValue = null;
                    if (previous != null && previous._localValues != null)
                        previous._localValues.TryGetValue(local, out previousValue);

                    object currentValue = null;
                    if (current != null && current._localValues != null)
                        current._localValues.TryGetValue(local, out currentValue);

                    if (previousValue != currentValue)
                        local.OnValueChanged(previousValue, currentValue, true);
                }
            }

            List<IAsyncLocal> currentLocalChangeNotifications = (current == null) ? null : current._localChangeNotifications;
            if (currentLocalChangeNotifications != null && currentLocalChangeNotifications != previousLocalChangeNotifications)
            {
                try
                {
                    foreach (IAsyncLocal local in currentLocalChangeNotifications)
                    {
                        // If the local has a value in the previous context, we already fired the event for that local
                        // in the code above.
                        object previousValue = null;
                        if (previous == null ||
                            previous._localValues == null ||
                            !previous._localValues.TryGetValue(local, out previousValue))
                        {
                            object currentValue = null;
                            if (current != null && current._localValues != null)
                                current._localValues.TryGetValue(local, out currentValue);

                            if (previousValue != currentValue)
                                local.OnValueChanged(previousValue, currentValue, true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Environment.FailFast(
                        Environment.GetResourceString("ExecutionContext_ExceptionInAsyncLocalNotification"),
                        ex);
                }
            }
        }


#if FEATURE_REMOTING
        internal LogicalCallContext LogicalCallContext
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (_logicalCallContext == null)
                {
                _logicalCallContext = new LogicalCallContext();
                }
                return _logicalCallContext;
            }
            [System.Security.SecurityCritical]  // auto-generated
            set
            {
                Contract.Assert(this != s_dummyDefaultEC);
                _logicalCallContext = value;
            }
        }

        internal IllogicalCallContext IllogicalCallContext
        {
            get
            {
                if (_illogicalCallContext == null)
                {
                _illogicalCallContext = new IllogicalCallContext();
                }
                return _illogicalCallContext;
            }
            set
            {
                Contract.Assert(this != s_dummyDefaultEC);
                _illogicalCallContext = value;
            }
        }
#endif // #if FEATURE_REMOTING

        internal SynchronizationContext SynchronizationContext
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get
            {
                return _syncContext;
            }
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            set
            {
                Contract.Assert(this != s_dummyDefaultEC);
                _syncContext = value;
            }
        }

        internal SynchronizationContext SynchronizationContextNoFlow
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get
            {
                return _syncContextNoFlow;
            }
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            set
            {
                Contract.Assert(this != s_dummyDefaultEC);
                _syncContextNoFlow = value;
            }
        }

#if FEATURE_CAS_POLICY
    internal HostExecutionContext HostExecutionContext
    {
            get 
            {
                return _hostExecutionContext;
            }
            set 
            {
                Contract.Assert(this != s_dummyDefaultEC);
                _hostExecutionContext = value;
            }
    }
#endif // FEATURE_CAS_POLICY
#if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK
        internal  SecurityContext SecurityContext
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get
            {
                return _securityContext;
            }
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            set
            {
                Contract.Assert(this != s_dummyDefaultEC);
                        // store the new security context 
                        _securityContext = value;
                        // perform the reverse link too
                        if (value != null)
                            _securityContext.ExecutionContext = this;
            }
        }
#endif // #if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK


        public void Dispose()
        {
            if(this.IsPreAllocatedDefault)
                return; //Do nothing if this is the default context
#if FEATURE_CAS_POLICY
            if (_hostExecutionContext != null)
                _hostExecutionContext.Dispose();
#endif // FEATURE_CAS_POLICY
#if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK
            if (_securityContext != null)
                _securityContext.Dispose();
#endif //FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK
        }
        
        [DynamicSecurityMethod]
        [System.Security.SecurityCritical]  // auto-generated_required
        public static void Run(ExecutionContext executionContext, ContextCallback callback, Object state)
        {
            if (executionContext == null)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NullContext"));
            if (!executionContext.isNewCapture)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotNewCaptureContext"));
            
            Run(executionContext, callback, state, false);
        }

        // This method is special from a security perspective - the VM will not allow a stack walk to
        // continue past the call to ExecutionContext.Run.  If you change the signature to this method, make
        // sure to update SecurityStackWalk::IsSpecialRunFrame in the VM to search for the new signature.
        [DynamicSecurityMethod]
        [SecurityCritical]
        [FriendAccessAllowed]
        internal static void Run(ExecutionContext executionContext, ContextCallback callback, Object state, bool preserveSyncCtx)
        {
            RunInternal(executionContext, callback, state, preserveSyncCtx);
        }

        // Actual implementation of Run is here, in a non-DynamicSecurityMethod, because the JIT seems to refuse to inline callees into
        // a DynamicSecurityMethod.
        [SecurityCritical]
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "Reviewed for thread safety")]
        [HandleProcessCorruptedStateExceptions]
        internal static void RunInternal(ExecutionContext executionContext, ContextCallback callback, Object state, bool preserveSyncCtx)
        {
            Contract.Assert(executionContext != null);
            if (executionContext.IsPreAllocatedDefault)
            {
                Contract.Assert(executionContext.IsDefaultFTContext(preserveSyncCtx));
            }
            else
            {
                Contract.Assert(executionContext.isNewCapture);
                executionContext.isNewCapture = false;
            }

            Thread currentThread = Thread.CurrentThread;
            ExecutionContextSwitcher ecsw = default(ExecutionContextSwitcher);

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                ExecutionContext.Reader ec = currentThread.GetExecutionContextReader();
                if ( (ec.IsNull || ec.IsDefaultFTContext(preserveSyncCtx)) && 
    #if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK                
                    SecurityContext.CurrentlyInDefaultFTSecurityContext(ec) && 
    #endif // #if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK                
                    executionContext.IsDefaultFTContext(preserveSyncCtx) &&
                    ec.HasSameLocalValues(executionContext)
                    )
                {
                    // Neither context is interesting, so we don't need to set the context.
                    // We do need to reset any changes made by the user's callback,
                    // so here we establish a "copy-on-write scope".  Any changes will
                    // result in a copy of the context being made, preserving the original
                    // context.
                    EstablishCopyOnWriteScope(currentThread, true, ref ecsw);
                }
                else
                {
                    if (executionContext.IsPreAllocatedDefault)
                        executionContext = new ExecutionContext();
                    ecsw = SetExecutionContext(executionContext, preserveSyncCtx);
                }

                //
                // Call the user's callback
                //
                callback(state);
            }
            finally
            {
                ecsw.Undo(currentThread);
            }
        }

        [SecurityCritical]
        static internal void EstablishCopyOnWriteScope(Thread currentThread, ref ExecutionContextSwitcher ecsw)
        {
            EstablishCopyOnWriteScope(currentThread, false, ref ecsw);
        }

        [SecurityCritical]
        static private void EstablishCopyOnWriteScope(Thread currentThread, bool knownNullWindowsIdentity, ref ExecutionContextSwitcher ecsw)
        {
            Contract.Assert(currentThread == Thread.CurrentThread);

            ecsw.outerEC = currentThread.GetExecutionContextReader();
            ecsw.outerECBelongsToScope = currentThread.ExecutionContextBelongsToCurrentScope;

#if FEATURE_IMPERSONATION
            ecsw.cachedAlwaysFlowImpersonationPolicy = SecurityContext.AlwaysFlowImpersonationPolicy;
            if (knownNullWindowsIdentity)
                Contract.Assert(SecurityContext.GetCurrentWI(ecsw.outerEC, ecsw.cachedAlwaysFlowImpersonationPolicy) == null);
            else
                ecsw.wi = SecurityContext.GetCurrentWI(ecsw.outerEC, ecsw.cachedAlwaysFlowImpersonationPolicy);
            ecsw.wiIsValid = true;
#endif
            currentThread.ExecutionContextBelongsToCurrentScope = false;
            ecsw.thread = currentThread;
        }

            
        // Sets the given execution context object on the thread.
        // Returns the previous one.
        [System.Security.SecurityCritical]  // auto-generated
        [DynamicSecurityMethodAttribute()]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
#if FEATURE_CORRUPTING_EXCEPTIONS
        [HandleProcessCorruptedStateExceptions] 
#endif // FEATURE_CORRUPTING_EXCEPTIONS
        internal  static ExecutionContextSwitcher SetExecutionContext(ExecutionContext executionContext, bool preserveSyncCtx)
        {
#if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK                        
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
#endif // #if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK

            Contract.Assert(executionContext != null);
            Contract.Assert(executionContext != s_dummyDefaultEC);

            // Set up the switcher object to return;
            ExecutionContextSwitcher ecsw = new ExecutionContextSwitcher();
            
            Thread currentThread = Thread.CurrentThread;
            ExecutionContext.Reader outerEC = currentThread.GetExecutionContextReader();

            ecsw.thread = currentThread;
            ecsw.outerEC = outerEC;
            ecsw.outerECBelongsToScope = currentThread.ExecutionContextBelongsToCurrentScope;

            if (preserveSyncCtx)
                executionContext.SynchronizationContext = outerEC.SynchronizationContext;
            executionContext.SynchronizationContextNoFlow = outerEC.SynchronizationContextNoFlow;

            currentThread.SetExecutionContext(executionContext, belongsToCurrentScope: true);

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                OnAsyncLocalContextChanged(outerEC.DangerousGetRawExecutionContext(), executionContext);

#if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK                    
                //set the security context
                SecurityContext sc = executionContext.SecurityContext;
                if (sc != null)
                {
                    // non-null SC: needs to be set
                    SecurityContext.Reader prevSeC = outerEC.SecurityContext;
                    ecsw.scsw = SecurityContext.SetSecurityContext(sc, prevSeC, false, ref stackMark);
                }
                else if (!SecurityContext.CurrentlyInDefaultFTSecurityContext(ecsw.outerEC))
                {
                    // null incoming SC, but we're currently not in FT: use static FTSC to set
                    SecurityContext.Reader prevSeC = outerEC.SecurityContext;
                    ecsw.scsw = SecurityContext.SetSecurityContext(SecurityContext.FullTrustSecurityContext, prevSeC, false, ref stackMark);
                }
#endif // #if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK
#if FEATURE_CAS_POLICY                
                // set the Host Context
                HostExecutionContext hostContext = executionContext.HostExecutionContext;
                if (hostContext != null)
                {
                    ecsw.hecsw = HostExecutionContextManager.SetHostExecutionContextInternal(hostContext);
                } 
#endif // FEATURE_CAS_POLICY
            }
            catch
            {
                ecsw.UndoNoThrow(currentThread);
                throw;
            }
            return ecsw;    
        }

        //
        // Public CreateCopy.  Used to copy captured ExecutionContexts so they can be reused multiple times.
        // This should only copy the portion of the context that we actually capture.
        //
        [SecuritySafeCritical]
        public ExecutionContext CreateCopy()
        {
            if (!isNewCapture)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotCopyUsedContext"));
            }
            ExecutionContext ec = new ExecutionContext();
            ec.isNewCapture = true;
            ec._syncContext = _syncContext == null ? null : _syncContext.CreateCopy();
            ec._localValues = _localValues;
            ec._localChangeNotifications = _localChangeNotifications;
#if FEATURE_CAS_POLICY
            // capture the host execution context
            ec._hostExecutionContext = _hostExecutionContext == null ? null : _hostExecutionContext.CreateCopy();
#endif // FEATURE_CAS_POLICY
#if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK
            if (_securityContext != null)
            {
                ec._securityContext = _securityContext.CreateCopy();
                ec._securityContext.ExecutionContext = ec;
            }
#endif // #if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK

#if FEATURE_REMOTING
            if (this._logicalCallContext != null)
                ec.LogicalCallContext = (LogicalCallContext)this.LogicalCallContext.Clone();

            Contract.Assert(this._illogicalCallContext == null);
#endif // #if FEATURE_REMOTING

            return ec;
        }

        //
        // Creates a complete copy, used for copy-on-write.
        //
        [SecuritySafeCritical]
        internal ExecutionContext CreateMutableCopy()
        {
            Contract.Assert(!this.isNewCapture);

            ExecutionContext ec = new ExecutionContext();

            // We don't deep-copy the SyncCtx, since we're still in the same context after copy-on-write.
            ec._syncContext = this._syncContext;
            ec._syncContextNoFlow = this._syncContextNoFlow;

#if FEATURE_CAS_POLICY
            // capture the host execution context
            ec._hostExecutionContext = this._hostExecutionContext == null ? null : _hostExecutionContext.CreateCopy();
#endif // FEATURE_CAS_POLICY

#if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK
            if (_securityContext != null)
            {
                ec._securityContext = this._securityContext.CreateMutableCopy();
                ec._securityContext.ExecutionContext = ec;
            }
#endif // #if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK

#if FEATURE_REMOTING
            if (this._logicalCallContext != null)
                ec.LogicalCallContext = (LogicalCallContext)this.LogicalCallContext.Clone();

            if (this._illogicalCallContext != null)
                ec.IllogicalCallContext = (IllogicalCallContext)this.IllogicalCallContext.CreateCopy();
#endif // #if FEATURE_REMOTING

            ec._localValues = this._localValues;
            ec._localChangeNotifications = this._localChangeNotifications;
            ec.isFlowSuppressed = this.isFlowSuppressed;

            return ec;
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public static AsyncFlowControl SuppressFlow()
        {
            if (IsFlowSuppressed())
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotSupressFlowMultipleTimes"));
            }
            Contract.EndContractBlock();
            AsyncFlowControl afc = new AsyncFlowControl();
            afc.Setup();
            return afc;
        }

        [SecuritySafeCritical]
        public static void RestoreFlow()
        {
            ExecutionContext ec = Thread.CurrentThread.GetMutableExecutionContext();
            if (!ec.isFlowSuppressed)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotRestoreUnsupressedFlow"));
            }
            ec.isFlowSuppressed = false;
        }

        [Pure]
        public static bool IsFlowSuppressed()
        {
            return Thread.CurrentThread.GetExecutionContextReader().IsFlowSuppressed;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static ExecutionContext Capture()
        {
            // set up a stack mark for finding the caller
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ExecutionContext.Capture(ref stackMark, CaptureOptions.None);            
        }

        //
        // Captures an ExecutionContext with optimization for the "default" case, and captures a "null" synchronization context.
        // When calling ExecutionContext.Run on the returned context, specify ignoreSyncCtx = true
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        [FriendAccessAllowed]
        internal static ExecutionContext FastCapture()
        {
            // set up a stack mark for finding the caller
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ExecutionContext.Capture(ref stackMark, CaptureOptions.IgnoreSyncCtx | CaptureOptions.OptimizeDefaultCase);
        }


        [Flags]
        internal enum CaptureOptions
        {
            None = 0x00,

            IgnoreSyncCtx = 0x01,       //Don't flow SynchronizationContext

            OptimizeDefaultCase = 0x02, //Faster in the typical case, but can't show the result to users
                                        // because they could modify the shared default EC.
                                        // Use this only if you won't be exposing the captured EC to users.
        }

    // internal helper to capture the current execution context using a passed in stack mark
        [System.Security.SecurityCritical]  // auto-generated
        static internal ExecutionContext Capture(ref StackCrawlMark stackMark, CaptureOptions options)
        {
            ExecutionContext.Reader ecCurrent = Thread.CurrentThread.GetExecutionContextReader();

            // check to see if Flow is suppressed
            if (ecCurrent.IsFlowSuppressed) 
                return null;

            //
            // Attempt to capture context.  There may be nothing to capture...
            //

#if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK            
            // capture the security context
            SecurityContext secCtxNew = SecurityContext.Capture(ecCurrent, ref stackMark);
#endif // #if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK
#if FEATURE_CAS_POLICY
             // capture the host execution context
            HostExecutionContext hostCtxNew = HostExecutionContextManager.CaptureHostExecutionContext();    		 
#endif // FEATURE_CAS_POLICY

            SynchronizationContext syncCtxNew = null;

#if FEATURE_REMOTING
            LogicalCallContext logCtxNew = null;
#endif

            if (!ecCurrent.IsNull)
            {
                // capture the sync context
                if (0 == (options & CaptureOptions.IgnoreSyncCtx))
                    syncCtxNew = (ecCurrent.SynchronizationContext == null) ? null : ecCurrent.SynchronizationContext.CreateCopy();

#if FEATURE_REMOTING
                // copy over the Logical Call Context
                if (ecCurrent.LogicalCallContext.HasInfo)
                    logCtxNew = ecCurrent.LogicalCallContext.Clone();
#endif // #if FEATURE_REMOTING
            }

            Dictionary<IAsyncLocal, object> localValues = null;
            List<IAsyncLocal> localChangeNotifications = null;
            if (!ecCurrent.IsNull)
            {
                localValues = ecCurrent.DangerousGetRawExecutionContext()._localValues;
                localChangeNotifications = ecCurrent.DangerousGetRawExecutionContext()._localChangeNotifications;
            }

            //
            // If we didn't get anything but defaults, and we're allowed to return the 
            // dummy default EC, don't bother allocating a new context.
            //
            if (0 != (options & CaptureOptions.OptimizeDefaultCase) &&
#if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK            
                secCtxNew == null &&
#endif
#if FEATURE_CAS_POLICY
                hostCtxNew == null &&
#endif // FEATURE_CAS_POLICY
                syncCtxNew == null &&
#if FEATURE_REMOTING
                (logCtxNew == null || !logCtxNew.HasInfo) &&
#endif // #if FEATURE_REMOTING
                localValues == null &&
                localChangeNotifications == null
                )
            {
                return s_dummyDefaultEC;
            }

            //
            // Allocate the new context, and fill it in.
            //
            ExecutionContext ecNew = new ExecutionContext();
#if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK            
            ecNew.SecurityContext = secCtxNew;
            if (ecNew.SecurityContext != null)
                ecNew.SecurityContext.ExecutionContext = ecNew;
#endif
#if FEATURE_CAS_POLICY
            ecNew._hostExecutionContext = hostCtxNew;
#endif // FEATURE_CAS_POLICY
            ecNew._syncContext = syncCtxNew;
#if FEATURE_REMOTING
            ecNew.LogicalCallContext = logCtxNew;
#endif // #if FEATURE_REMOTING
            ecNew._localValues = localValues;
            ecNew._localChangeNotifications = localChangeNotifications;
            ecNew.isNewCapture = true;

            return ecNew;
        }

        //
        // Implementation of ISerializable
        //

        [System.Security.SecurityCritical]  // auto-generated_required
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info==null) 
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();

#if FEATURE_REMOTING
            if (_logicalCallContext != null)
            {
                info.AddValue("LogicalCallContext", _logicalCallContext, typeof(LogicalCallContext));
            }
#endif // #if FEATURE_REMOTING
        }

        [System.Security.SecurityCritical]  // auto-generated
        private ExecutionContext(SerializationInfo info, StreamingContext context) 
        {
            SerializationInfoEnumerator e = info.GetEnumerator();
            while (e.MoveNext())
            {
#if FEATURE_REMOTING
                if (e.Name.Equals("LogicalCallContext"))
                {
                    _logicalCallContext = (LogicalCallContext) e.Value;
                }
#endif // #if FEATURE_REMOTING
            }
        } // ObjRef .ctor
     

        [System.Security.SecurityCritical]  // auto-generated
        internal bool IsDefaultFTContext(bool ignoreSyncCtx)
        {
#if FEATURE_CAS_POLICY
            if (_hostExecutionContext != null)
                return false;
#endif // FEATURE_CAS_POLICY            
            if (!ignoreSyncCtx && _syncContext != null)
                return false;
#if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK            
            if (_securityContext != null && !_securityContext.IsDefaultFTSecurityContext())
                return false;
#endif //#if FEATURE_IMPERSONATION || FEATURE_COMPRESSEDSTACK
#if FEATURE_REMOTING
            if (_logicalCallContext != null && _logicalCallContext.HasInfo)
                return false;
            if (_illogicalCallContext != null && _illogicalCallContext.HasUserData)
                return false;
#endif //#if FEATURE_REMOTING
            return true;
        }
    } // class ExecutionContext

#endif //FEATURE_CORECLR
}


