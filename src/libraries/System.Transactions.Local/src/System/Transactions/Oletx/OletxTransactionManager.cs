// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
// using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading;
using System.Transactions.Diagnostics;

#nullable disable

namespace System.Transactions.Oletx
{
    internal class OletxTransactionManager
    {
        private System.Transactions.IsolationLevel isolationLevelProperty;

        private TimeSpan timeoutProperty;

        private TransactionOptions configuredTransactionOptions = new TransactionOptions();

        // Object for synchronizing access to the entire class( avoiding lock( typeof( ... )) )
        private static object classSyncObject;

        // These have to be static because we can only add an RM with the proxy once, even if we
        // have multiple OletxTransactionManager instances.
        internal static Hashtable resourceManagerHashTable;
        public static System.Threading.ReaderWriterLock resourceManagerHashTableLock;

        internal static volatile bool processingTmDown = false;

        internal ReaderWriterLock dtcTransactionManagerLock;
        DtcTransactionManager dtcTransactionManager;
        internal OletxInternalResourceManager internalResourceManager;

        internal static IDtcProxyShimFactory proxyShimFactory = null;

        // Double-checked locking pattern requires volatile for read/write synchronization
        internal static volatile EventWaitHandle shimWaitHandle = null;
        internal static EventWaitHandle ShimWaitHandle
        {
            get
            {
                if ( null == shimWaitHandle )
                {
                    lock ( ClassSyncObject )
                    {
                        if ( null == shimWaitHandle )
                        {
                            shimWaitHandle = new EventWaitHandle( false, EventResetMode.AutoReset );
                        }
                    }
                }

                return shimWaitHandle;
            }
        }

        string nodeNameField;
//        byte[] propToken;

        // Method that is used within SQLCLR as the WaitOrTimerCallback for the call to
        // ThreadPool.RegisterWaitForSingleObject.
        // This is here for the DangerousGetHandle call.  We need to do it.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods")]
        internal static void ShimNotificationCallback( object state, bool timeout )
        {
            // First we need to get the notification from the shim factory.
            IntPtr enlistmentHandleIntPtr = IntPtr.Zero;
            ShimNotificationType shimNotificationType = ShimNotificationType.None;
            bool isSinglePhase = false;
            bool abortingHint = false;

            UInt32 prepareInfoSize = 0;
            CoTaskMemHandle prepareInfoBuffer = null;

            bool holdingNotificationLock = false;
            bool cleanExit = false;

            IDtcProxyShimFactory localProxyShimFactory = null;

            if ( DiagnosticTrace.Verbose )
            {
                MethodEnteredTraceRecord.Trace( SR.TraceSourceOletx,
                    "OletxTransactionManager.ShimNotificationCallback"
                    );
            }

            // This lock doesn't really protect any of our data.  It is here so that if an exception occurs
            // while calling out to the app, we get an escalation to AppDomainUnload.
            Thread.BeginCriticalRegion();
            try
            {
                do
                {
                    // Take a local copy of the proxyShimFactory because if we get an RM TMDown notification,
                    // we will still hold the critical section in that factory, but processing of the TMDown will
                    // cause replacement of the OletxTransactionManager.proxyShimFactory.
                    localProxyShimFactory = OletxTransactionManager.proxyShimFactory;
                    try
                    {
                        Thread.BeginThreadAffinity();
                        RuntimeHelpers.PrepareConstrainedRegions();
                        try
                        {
                            localProxyShimFactory.GetNotification(
                                out enlistmentHandleIntPtr,
                                out shimNotificationType,
                                out isSinglePhase,
                                out abortingHint,
                                out holdingNotificationLock,
                                out prepareInfoSize,
                                out prepareInfoBuffer
                                );
                        }
                        finally
                        {
                            if ( holdingNotificationLock )
                            {
                                if ( (HandleTable.FindHandle(enlistmentHandleIntPtr)) is OletxInternalResourceManager )
                                {
                                    // In this case we know that the TM has gone down and we need to exchange
                                    // the native lock for a managed lock.
                                    processingTmDown = true;
#pragma warning disable 0618
                                    //@TODO: This overload of Monitor.Enter is obsolete.  Please change this to use Monitor.Enter(ref bool), and remove the pragmas   -- ericeil
                                    System.Threading.Monitor.Enter(OletxTransactionManager.proxyShimFactory);
#pragma warning restore 0618
                                }
                                else
                                {
                                    holdingNotificationLock = false;
                                }
                                localProxyShimFactory.ReleaseNotificationLock();
                            }
                            Thread.EndThreadAffinity();
                        }

                        // If a TM down is being processed it is possible that the native lock
                        // has been exchanged for a managed lock.  In that case we need to attempt
                        // to take a lock to hold up processing more events until the TM down
                        // processing is complete.
                        if ( processingTmDown )
                        {
                            lock (OletxTransactionManager.proxyShimFactory)
                            {
                                // We don't do any work under this lock just make sure that we
                                // can take it.
                            }
                        }

                        if ( ShimNotificationType.None != shimNotificationType )
                        {
                            Object target = HandleTable.FindHandle(enlistmentHandleIntPtr);

                            // Next, based on the notification type, cast the Handle accordingly and make
                            // the appropriate call on the enlistment.
                            switch ( shimNotificationType )
                            {
                                case ShimNotificationType.Phase0RequestNotify:
                                {
                                    try
                                    {
                                        OletxPhase0VolatileEnlistmentContainer ph0VolEnlistContainer = target as OletxPhase0VolatileEnlistmentContainer;
                                        if ( null != ph0VolEnlistContainer )
                                        {
                                            DiagnosticTrace.SetActivityId(
                                                ph0VolEnlistContainer.TransactionIdentifier);
                                            //CSDMain 91509 - We now synchronize this call with the AddDependentClone call in RealOleTxTransaction
                                            ph0VolEnlistContainer.Phase0Request( abortingHint );
                                        }
                                        else
                                        {
                                            OletxEnlistment enlistment = target as OletxEnlistment;
                                            if ( null != enlistment )
                                            {
                                                DiagnosticTrace.SetActivityId(
                                                    enlistment.TransactionIdentifier);
                                                enlistment.Phase0Request( abortingHint );
                                            }
                                            else
                                            {
                                                Environment.FailFast( SR.InternalError);
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        // We aren't going to get any more notifications on this.
                                        HandleTable.FreeHandle(enlistmentHandleIntPtr);
                                    }
                                    break;
                                }

                                case ShimNotificationType.VoteRequestNotify:
                                {
                                    OletxPhase1VolatileEnlistmentContainer ph1VolEnlistContainer = target as OletxPhase1VolatileEnlistmentContainer;
                                    if ( null != ph1VolEnlistContainer )
                                    {
                                        DiagnosticTrace.SetActivityId(
                                            ph1VolEnlistContainer.TransactionIdentifier);
                                        ph1VolEnlistContainer.VoteRequest();
                                    }
                                    else
                                    {
                                        Environment.FailFast( SR.InternalError);
                                    }

                                    break;
                                }

                                case ShimNotificationType.CommittedNotify:
                                {
                                    try
                                    {
                                        OutcomeEnlistment outcomeEnlistment = target as OutcomeEnlistment;
                                        if ( null != outcomeEnlistment )
                                        {
                                            DiagnosticTrace.SetActivityId(
                                                outcomeEnlistment.TransactionIdentifier);
                                            outcomeEnlistment.Committed();
                                        }
                                        else
                                        {
                                            OletxPhase1VolatileEnlistmentContainer ph1VolEnlistContainer = target as OletxPhase1VolatileEnlistmentContainer;
                                            if ( null != ph1VolEnlistContainer )
                                            {
                                                DiagnosticTrace.SetActivityId(
                                                    ph1VolEnlistContainer.TransactionIdentifier);
                                                ph1VolEnlistContainer.Committed();
                                            }
                                            else
                                            {
                                                Environment.FailFast( SR.InternalError);
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        // We aren't going to get any more notifications on this.
                                        HandleTable.FreeHandle(enlistmentHandleIntPtr);
                                    }
                                    break;
                                }
                                case ShimNotificationType.AbortedNotify:
                                {
                                    try
                                    {
                                        OutcomeEnlistment outcomeEnlistment = target as OutcomeEnlistment;
                                        if ( null != outcomeEnlistment )
                                        {
                                            DiagnosticTrace.SetActivityId(
                                                outcomeEnlistment.TransactionIdentifier);
                                            outcomeEnlistment.Aborted();
                                        }
                                        else
                                        {
                                            OletxPhase1VolatileEnlistmentContainer ph1VolEnlistContainer = target as OletxPhase1VolatileEnlistmentContainer;
                                            if ( null != ph1VolEnlistContainer )
                                            {
                                                DiagnosticTrace.SetActivityId(
                                                    ph1VolEnlistContainer.TransactionIdentifier);
                                                ph1VolEnlistContainer.Aborted();
                                            }
                                            // else
                                                // Voters may receive notifications even
                                                // in cases where they therwise respond
                                                // negatively to the vote request.  It is
                                                // also not guaranteed that we will get a
                                                // notification if we do respond negatively.
                                                // The only safe thing to do is to free the
                                                // Handle when we abort the transaction
                                                // with a voter.  These two things together
                                                // mean that we cannot guarantee that this
                                                // Handle will be alive when we get this
                                                // notification.
                                        }
                                    }
                                    finally
                                    {
                                        // We aren't going to get any more notifications on this.
                                        HandleTable.FreeHandle(enlistmentHandleIntPtr);
                                    }
                                    break;
                                }
                                case ShimNotificationType.InDoubtNotify:
                                {
                                    try
                                    {
                                        OutcomeEnlistment outcomeEnlistment = target as OutcomeEnlistment;
                                        if ( null != outcomeEnlistment )
                                        {
                                            DiagnosticTrace.SetActivityId(
                                                outcomeEnlistment.TransactionIdentifier);
                                            outcomeEnlistment.InDoubt();
                                        }
                                        else
                                        {
                                            OletxPhase1VolatileEnlistmentContainer ph1VolEnlistContainer = target as OletxPhase1VolatileEnlistmentContainer;
                                            if ( null != ph1VolEnlistContainer )
                                            {
                                                DiagnosticTrace.SetActivityId(
                                                    ph1VolEnlistContainer.TransactionIdentifier);
                                                ph1VolEnlistContainer.InDoubt();
                                            }
                                            else
                                            {
                                                Environment.FailFast( SR.InternalError);
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        // We aren't going to get any more notifications on this.
                                        HandleTable.FreeHandle(enlistmentHandleIntPtr);
                                    }
                                    break;
                                }

                                case ShimNotificationType.PrepareRequestNotify:
                                {
                                    byte[] prepareInfo = new byte[prepareInfoSize];
                                    Marshal.Copy( prepareInfoBuffer.DangerousGetHandle(), prepareInfo, 0, Convert.ToInt32(prepareInfoSize) );
                                    bool enlistmentDone = true;

                                    try
                                    {
                                        OletxEnlistment enlistment = target as OletxEnlistment;
                                        if ( null != enlistment )
                                        {
                                            DiagnosticTrace.SetActivityId(
                                                enlistment.TransactionIdentifier);
                                            enlistmentDone = enlistment.PrepareRequest(
                                                                isSinglePhase,
                                                                prepareInfo
                                                                );
                                        }
                                        else
                                        {
                                            Environment.FailFast( SR.InternalError);
                                        }
                                    }
                                    finally
                                    {
                                        if (enlistmentDone)
                                        {
                                            HandleTable.FreeHandle(enlistmentHandleIntPtr);
                                        }
                                    }

                                    break;
                                }

                                case ShimNotificationType.CommitRequestNotify:
                                {
                                    try
                                    {
                                        OletxEnlistment enlistment = target as OletxEnlistment;
                                        if ( null != enlistment )
                                        {
                                            DiagnosticTrace.SetActivityId(
                                                enlistment.TransactionIdentifier);
                                            enlistment.CommitRequest();
                                        }
                                        else
                                        {
                                            Environment.FailFast( SR.InternalError);
                                        }
                                    }
                                    finally
                                    {
                                        // We aren't going to get any more notifications on this.
                                        HandleTable.FreeHandle(enlistmentHandleIntPtr);
                                    }

                                    break;
                                }

                                case ShimNotificationType.AbortRequestNotify:
                                {
                                    try
                                    {
                                        OletxEnlistment enlistment = target as OletxEnlistment;
                                        if ( null != enlistment )
                                        {
                                            DiagnosticTrace.SetActivityId(
                                                enlistment.TransactionIdentifier);
                                            enlistment.AbortRequest();
                                        }
                                        else
                                        {
                                            Environment.FailFast( SR.InternalError);
                                        }
                                    }
                                    finally
                                    {
                                        // We aren't going to get any more notifications on this.
                                        HandleTable.FreeHandle(enlistmentHandleIntPtr);
                                    }

                                    break;
                                }

                                case ShimNotificationType.EnlistmentTmDownNotify:
                                {
                                    try
                                    {
                                        OletxEnlistment enlistment = target as OletxEnlistment;
                                        if ( null != enlistment )
                                        {
                                            DiagnosticTrace.SetActivityId(
                                                enlistment.TransactionIdentifier);
                                            enlistment.TMDown();
                                        }
                                        else
                                        {
                                            Environment.FailFast( SR.InternalError);
                                        }
                                    }
                                    finally
                                    {
                                        // We aren't going to get any more notifications on this.
                                        HandleTable.FreeHandle(enlistmentHandleIntPtr);
                                    }

                                    break;
                                }


                                case ShimNotificationType.ResourceManagerTmDownNotify:
                                {
                                    OletxResourceManager resourceManager = target as OletxResourceManager;
                                    try
                                    {
                                        if ( null != resourceManager )
                                        {
                                            resourceManager.TMDown();
                                        }
                                        else
                                        {
                                            OletxInternalResourceManager internalResourceManager = target as OletxInternalResourceManager;
                                            if ( null != internalResourceManager )
                                            {
                                                internalResourceManager.TMDown();
                                            }
                                            else
                                            {
                                                Environment.FailFast(SR.InternalError);
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        HandleTable.FreeHandle(enlistmentHandleIntPtr);
                                    }

                                    // Note that we don't free the gchandle on the OletxResourceManager.  These objects
                                    // are not going to go away.
                                    break;
                                }

                                default:
                                {
                                    Environment.FailFast(SR.InternalError);
                                    break;
                                }
                            }
                        }
                    }
                    finally
                    {
                        if ( null != prepareInfoBuffer )
                        {
                            prepareInfoBuffer.Close();
                        }

                        if ( holdingNotificationLock )
                        {
                            holdingNotificationLock = false;
                            processingTmDown = false;
                            System.Threading.Monitor.Exit(OletxTransactionManager.proxyShimFactory);
                        }
                    }
                }
                while ( ShimNotificationType.None != shimNotificationType );

                cleanExit = true;
            }
            finally
            {
                if ( holdingNotificationLock )
                {
                    holdingNotificationLock = false;
                    processingTmDown = false;
                    System.Threading.Monitor.Exit(OletxTransactionManager.proxyShimFactory);
                }

                if ( !cleanExit && enlistmentHandleIntPtr != IntPtr.Zero )
                {
                    HandleTable.FreeHandle(enlistmentHandleIntPtr);
                }

                Thread.EndCriticalRegion();
            }

            if ( DiagnosticTrace.Verbose )
            {
                MethodExitedTraceRecord.Trace( SR.TraceSourceOletx,
                    "OletxTransactionManager.ShimNotificationCallback"
                    );
            }

        }


        internal OletxTransactionManager(
            string nodeName
            )
        {
            lock ( ClassSyncObject )
            {
                // If we have not already initialized the shim factory and started the notification
                // thread, do so now.
                if (null == OletxTransactionManager.proxyShimFactory )
                {
                    Int32 error = NativeMethods.GetNotificationFactory(
                        OletxTransactionManager.ShimWaitHandle.SafeWaitHandle,
                        out OletxTransactionManager.proxyShimFactory
                        );

                    if ( 0 != error )
                    {
                        throw TransactionException.Create( SR.UnableToGetNotificationShimFactory, null );
                    }

                        ThreadPool.UnsafeRegisterWaitForSingleObject(
                            OletxTransactionManager.ShimWaitHandle,
                            new WaitOrTimerCallback( OletxTransactionManager.ShimNotificationCallback ),
                            null,
                            -1,
                            false
                            );
                }
            }

            this.dtcTransactionManagerLock = new ReaderWriterLock();

            this.nodeNameField = nodeName;

            // The DTC proxy doesn't like an empty string for node name on 64-bit platforms when
            // running as WOW64.  It treats any non-null node name as a "remote" node and turns off
            // the WOW64 bit, causing problems when reading the registry.  So if we got on empty
            // string for the node name, just treat it as null.
            if (( null != this.nodeNameField ) && ( 0 == this.nodeNameField.Length ))
            {
                this.nodeNameField = null;
            }

            if ( DiagnosticTrace.Verbose )
            {
                DistributedTransactionManagerCreatedTraceRecord.Trace( SR.TraceSourceOletx,
                    this.GetType(),
                    this.nodeNameField
                    );
            }

            // Initialize the properties from config.
            configuredTransactionOptions.IsolationLevel = isolationLevelProperty = TransactionManager.DefaultIsolationLevel;
            configuredTransactionOptions.Timeout = timeoutProperty = TransactionManager.DefaultTimeout;

            this.internalResourceManager = new OletxInternalResourceManager( this );

            dtcTransactionManagerLock.AcquireWriterLock( -1 );
            try
            {
                this.dtcTransactionManager = new DtcTransactionManager( this.nodeNameField, this );
            }
            finally
            {
                dtcTransactionManagerLock.ReleaseWriterLock();
            }

            if (resourceManagerHashTable == null)
            {
                resourceManagerHashTable = new Hashtable(2);
                resourceManagerHashTableLock = new System.Threading.ReaderWriterLock();
            }

        }


        internal OletxCommittableTransaction CreateTransaction(
            TransactionOptions properties
            )
        {
            OletxCommittableTransaction tx = null;
            RealOletxTransaction realTransaction = null;
            ITransactionShim transactionShim = null;
            Guid txIdentifier = Guid.Empty;
            OutcomeEnlistment outcomeEnlistment = null;

            TransactionManager.ValidateIsolationLevel( properties.IsolationLevel );

            // Never create a transaction with an IsolationLevel of Unspecified.
            if ( IsolationLevel.Unspecified == properties.IsolationLevel )
            {
                properties.IsolationLevel = configuredTransactionOptions.IsolationLevel;
            }

            properties.Timeout = TransactionManager.ValidateTimeout( properties.Timeout );

            this.dtcTransactionManagerLock.AcquireReaderLock( -1 );
            try
            {
                // TODO: Make Sys.Tx isolation level values the same as DTC isolation level values and use the sys.tx value here.
                OletxTransactionIsolationLevel oletxIsoLevel = OletxTransactionManager.ConvertIsolationLevel( properties.IsolationLevel );
                UInt32 oletxTimeout = DtcTransactionManager.AdjustTimeout( properties.Timeout );

                outcomeEnlistment = new OutcomeEnlistment();
                IntPtr outcomeEnlistmentHandle = IntPtr.Zero;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    outcomeEnlistmentHandle = HandleTable.AllocHandle( outcomeEnlistment );

                    dtcTransactionManager.ProxyShimFactory.BeginTransaction(
                        oletxTimeout,
                        oletxIsoLevel,
                        outcomeEnlistmentHandle,
                        out txIdentifier,
                        out transactionShim
                        );
                }
                catch ( COMException ex )
                {
                    OletxTransactionManager.ProxyException( ex );
                    throw;
                }
                finally
                {
                    if ( transactionShim == null && outcomeEnlistmentHandle != IntPtr.Zero )
                    {
                        HandleTable.FreeHandle( outcomeEnlistmentHandle );
                    }
                }

                realTransaction = new RealOletxTransaction(
                    this,
                    transactionShim,
                    outcomeEnlistment,
                    txIdentifier,
                    oletxIsoLevel,
                    true
                    );
                tx = new OletxCommittableTransaction( realTransaction );
                if ( DiagnosticTrace.Information )
                {
                    TransactionCreatedTraceRecord.Trace( SR.TraceSourceOletx,
                        tx.TransactionTraceId
                        );
                }
            }
            finally
            {
                this.dtcTransactionManagerLock.ReleaseReaderLock();
            }

            return tx;

        }


        internal OletxEnlistment ReenlistTransaction(
            Guid resourceManagerIdentifier,
            byte[] recoveryInformation,
            IEnlistmentNotificationInternal enlistmentNotification
            )
        {
            if ( null == recoveryInformation )
            {
                throw new ArgumentNullException( "recoveryInformation" );
            }

            if ( null == enlistmentNotification )
            {
                throw new ArgumentNullException( "enlistmentNotification" );
            }

            // Now go find the resource manager in the collection.
            OletxResourceManager oletxResourceManager = RegisterResourceManager( resourceManagerIdentifier );
            if ( null == oletxResourceManager )
            {
                throw new ArgumentException( SR.InvalidArgument, "resourceManagerIdentifier" );
            }

            if ( oletxResourceManager.RecoveryCompleteCalledByApplication )
            {
                throw new InvalidOperationException( SR.ReenlistAfterRecoveryComplete);
            }

            // Now ask the resource manager to reenlist.
            OletxEnlistment returnValue = oletxResourceManager.Reenlist(
                recoveryInformation.Length,
                recoveryInformation,
                enlistmentNotification
                );


            return returnValue;
        }

        internal void ResourceManagerRecoveryComplete(
            Guid resourceManagerIdentifier
            )
        {
            OletxResourceManager oletxRm = RegisterResourceManager(
                resourceManagerIdentifier
                );

            if ( oletxRm.RecoveryCompleteCalledByApplication )
            {
                throw new InvalidOperationException( SR.DuplicateRecoveryComplete);
            }

            oletxRm.RecoveryComplete();

        }

        internal OletxResourceManager RegisterResourceManager(
            Guid resourceManagerIdentifier
            )
        {
            OletxResourceManager oletxResourceManager = null;

            resourceManagerHashTableLock.AcquireWriterLock(-1);

            try
            {
                // If this resource manager has already been registered, don't register it again.
                oletxResourceManager = resourceManagerHashTable[resourceManagerIdentifier] as OletxResourceManager;
                if ( null != oletxResourceManager )
                {
                    return oletxResourceManager;
                }

                oletxResourceManager = new OletxResourceManager(
                    this,
                    resourceManagerIdentifier
                    );

                resourceManagerHashTable.Add(
                    resourceManagerIdentifier,
                    oletxResourceManager
                    );
            }
            finally
            {
                resourceManagerHashTableLock.ReleaseWriterLock();
            }


            return oletxResourceManager;
        }

        internal string CreationNodeName
        {
            get { return nodeNameField; }
        }

        internal OletxResourceManager FindOrRegisterResourceManager(
            Guid resourceManagerIdentifier
            )
        {
            if ( resourceManagerIdentifier == Guid.Empty )
            {
                throw new ArgumentException( SR.BadResourceManagerId, "resourceManagerIdentifier" );
            }

            OletxResourceManager oletxResourceManager = null;

            resourceManagerHashTableLock.AcquireReaderLock(-1);
            try
            {
                oletxResourceManager = resourceManagerHashTable[resourceManagerIdentifier] as OletxResourceManager;
            }
            finally
            {
                resourceManagerHashTableLock.ReleaseReaderLock();
            }

            if ( null == oletxResourceManager )
            {
                return RegisterResourceManager( resourceManagerIdentifier);
            }

            return oletxResourceManager;
        }

        internal DtcTransactionManager DtcTransactionManager
        {
            get
            {
                if ( ( this.dtcTransactionManagerLock.IsReaderLockHeld ) ||
                    ( this.dtcTransactionManagerLock.IsWriterLockHeld ) )
                {
                    if ( null == this.dtcTransactionManager )
                    {
                        throw TransactionException.Create(
                            SR.DtcTransactionManagerUnavailable,
                            null );
                    }
                    return this.dtcTransactionManager;
                }
                else
                {
                    // Internal programming error.  A reader or writer lock should be held when this property is invoked.
                    throw TransactionException.Create ( SR.InternalError, null );
                }
            }
        }

        internal string NodeName
        {
            get { return this.nodeNameField; }
        }

        internal static void ProxyException(
            COMException comException
            )
        {
            if ( ( NativeMethods.XACT_E_CONNECTION_DOWN == comException.ErrorCode ) ||
                ( NativeMethods.XACT_E_TMNOTAVAILABLE == comException.ErrorCode )
                )
            {
                throw TransactionManagerCommunicationException.Create(
                    SR.TransactionManagerCommunicationException,
                    comException
                    );
            }
            if (( NativeMethods.XACT_E_NETWORK_TX_DISABLED == comException.ErrorCode ))
            {
                throw TransactionManagerCommunicationException.Create(
                    SR.NetworkTransactionsDisabled,
                    comException
                    );
            }
            // Else if the error is a transaction oriented error, throw a TransactionException
            else if ( ( NativeMethods.XACT_E_FIRST <= comException.ErrorCode ) &&
                      ( NativeMethods.XACT_E_LAST >= comException.ErrorCode ) )
            {
                // Special casing XACT_E_NOTRANSACTION
                if ( NativeMethods.XACT_E_NOTRANSACTION == comException.ErrorCode )
                {
                    throw TransactionException.Create(
                        SR.TransactionAlreadyOver,
                        comException
                        );
                }

                throw TransactionException.Create(
                    comException.Message,
                    comException
                    );
            }
        }

        internal void ReinitializeProxy()
        {
            // This is created by the static constructor.
            dtcTransactionManagerLock.AcquireWriterLock( -1 );
            try
            {
                if ( null != dtcTransactionManager )
                {
                    dtcTransactionManager.ReleaseProxy();
                }
            }
            finally
            {
                dtcTransactionManagerLock.ReleaseWriterLock();
            }
        }

        internal static OletxTransactionIsolationLevel ConvertIsolationLevel( IsolationLevel isolationLevel )
        {
            OletxTransactionIsolationLevel retVal;
            switch (isolationLevel)
            {
                case IsolationLevel.Serializable:
                    retVal = OletxTransactionIsolationLevel.ISOLATIONLEVEL_SERIALIZABLE;
                    break;
                case IsolationLevel.RepeatableRead:
                    retVal = OletxTransactionIsolationLevel.ISOLATIONLEVEL_REPEATABLEREAD;
                    break;
                case IsolationLevel.ReadCommitted:
                    retVal = OletxTransactionIsolationLevel.ISOLATIONLEVEL_READCOMMITTED;
                    break;
                case IsolationLevel.ReadUncommitted:
                    retVal = OletxTransactionIsolationLevel.ISOLATIONLEVEL_READUNCOMMITTED;
                    break;
                case IsolationLevel.Chaos:
                    retVal = OletxTransactionIsolationLevel.ISOLATIONLEVEL_CHAOS;
                    break;
                case IsolationLevel.Unspecified:
                    retVal = OletxTransactionIsolationLevel.ISOLATIONLEVEL_UNSPECIFIED;
                    break;
                default:
                    retVal = OletxTransactionIsolationLevel.ISOLATIONLEVEL_SERIALIZABLE;
                    break;
            }
            return retVal;
        }

        internal static IsolationLevel ConvertIsolationLevelFromProxyValue( OletxTransactionIsolationLevel proxyIsolationLevel )
        {
            IsolationLevel retVal;
            switch (proxyIsolationLevel)
            {
                case OletxTransactionIsolationLevel.ISOLATIONLEVEL_SERIALIZABLE:
                    retVal = IsolationLevel.Serializable;
                    break;
                case OletxTransactionIsolationLevel.ISOLATIONLEVEL_REPEATABLEREAD:
                    retVal = IsolationLevel.RepeatableRead;
                    break;
                case OletxTransactionIsolationLevel.ISOLATIONLEVEL_READCOMMITTED:
                    retVal = IsolationLevel.ReadCommitted;
                    break;
                case OletxTransactionIsolationLevel.ISOLATIONLEVEL_READUNCOMMITTED:
                    retVal = IsolationLevel.ReadUncommitted;
                    break;
                case OletxTransactionIsolationLevel.ISOLATIONLEVEL_UNSPECIFIED:
                    retVal = IsolationLevel.Unspecified;
                    break;
                case OletxTransactionIsolationLevel.ISOLATIONLEVEL_CHAOS:
                    retVal = IsolationLevel.Chaos;
                    break;
                default:
                    retVal = IsolationLevel.Serializable;
                    break;
            }
            return retVal;
        }

        // Helper object for static synchronization
        internal static object ClassSyncObject
        {
            get
            {
                if ( classSyncObject == null )
                {
                    object o = new object();
                    Interlocked.CompareExchange( ref classSyncObject, o, null );
                }
                return classSyncObject;
            }
        }

    }

    internal class OletxInternalResourceManager
    {
        OletxTransactionManager oletxTm;
        Guid myGuid;
        internal IResourceManagerShim resourceManagerShim = null;


        internal OletxInternalResourceManager( OletxTransactionManager oletxTm )
        {
            this.oletxTm = oletxTm;
            this.myGuid = Guid.NewGuid();

        }

        public void TMDown()
        {
            // Let's set ourselves up for reinitialization with the proxy by releasing our
            // reference to the resource manager shim, which will release its reference
            // to the proxy when it destructs.
            this.resourceManagerShim = null;

            // We need to look through all the transactions and tell them about
            // the TMDown so they can tell their Phase0VolatileEnlistmentContainers.
            Transaction tx = null;
            RealOletxTransaction realTx = null;
            IDictionaryEnumerator tableEnum = null;

            if ( DiagnosticTrace.Verbose )
            {
                MethodEnteredTraceRecord.Trace( SR.TraceSourceOletx,
                    "OletxInternalResourceManager.TMDown"
                    );
            }

            // make a local copy of the hash table to avoid possible deadlocks when we lock both the global hash table
            // and the transaction object.
            Hashtable txHashTable = null;
            lock ( TransactionManager.PromotedTransactionTable.SyncRoot )
            {
                txHashTable = (Hashtable) TransactionManager.PromotedTransactionTable.Clone();
            }

            // No need to lock my hashtable, nobody is going to change it.
            tableEnum = txHashTable.GetEnumerator();
            while ( tableEnum.MoveNext() )
            {
                WeakReference txWeakRef = (WeakReference) tableEnum.Value;
                if ( null != txWeakRef )
                {
                    tx = (Transaction)txWeakRef.Target;
                    if ( null != tx )
                    {
                        realTx = tx._internalTransaction.PromotedTransaction.realOletxTransaction;
                        // Only deal with transactions owned by my OletxTm.
                        if ( realTx.OletxTransactionManagerInstance == this.oletxTm )
                        {
                            realTx.TMDown();
                        }
                    }
                }
            }

            // Now make a local copy of the hash table of resource managers and tell each of them.  This is to
            // deal with Durable EDPR=true (phase0) enlistments.  Each RM will also get a TMDown, but it will
            // come AFTER the "buggy" Phase0Request with abortHint=true - COMPlus bug 36760/36758.
            Hashtable rmHashTable = null;
            if ( null != OletxTransactionManager.resourceManagerHashTable )
            {
                OletxTransactionManager.resourceManagerHashTableLock.AcquireReaderLock( Timeout.Infinite );
                try
                {
                    rmHashTable = (Hashtable) OletxTransactionManager.resourceManagerHashTable.Clone();
                }
                finally
                {
                    OletxTransactionManager.resourceManagerHashTableLock.ReleaseReaderLock();
                }
            }

            if ( null != rmHashTable )
            {
                // No need to lock my hashtable, nobody is going to change it.
                tableEnum = rmHashTable.GetEnumerator();
                while ( tableEnum.MoveNext() )
                {
                    OletxResourceManager oletxRM = (OletxResourceManager) tableEnum.Value;
                    if ( null != oletxRM )
                    {
                        // When the RM spins through its enlistments, it will need to make sure that
                        // the enlistment is for this particular TM.
                        oletxRM.TMDownFromInternalRM( this.oletxTm );
                    }
                }
            }

            // Now let's reinitialize the shim.
            this.oletxTm.dtcTransactionManagerLock.AcquireWriterLock( -1 );
            try
            {
                this.oletxTm.ReinitializeProxy();
            }
            finally
            {
                this.oletxTm.dtcTransactionManagerLock.ReleaseWriterLock();
            }

            if ( DiagnosticTrace.Verbose )
            {
                MethodExitedTraceRecord.Trace( SR.TraceSourceOletx,
                    "OletxInternalResourceManager.TMDown"
                    );
            }

        }

        internal Guid Identifier
        {
            get { return this.myGuid; }
        }

        internal void CallReenlistComplete()
        {
            this.resourceManagerShim.ReenlistComplete();
        }

    }



}
