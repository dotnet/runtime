// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Permissions;
using System.Threading;
using System.Transactions.Diagnostics;

namespace System.Transactions.Oletx
{

    internal sealed class OletxResourceManager
    {
        internal Guid resourceManagerIdentifier;

        internal IResourceManagerShim resourceManagerShim;
        internal Hashtable enlistmentHashtable;
        internal static Hashtable volatileEnlistmentHashtable = new Hashtable();
        internal OletxTransactionManager oletxTransactionManager;

        // reenlistList is a simple ArrayList of OletxEnlistment objects that are either in the
        // Preparing or Prepared state when we receive a TMDown notification or have had
        // ReenlistTransaction called for them.  The ReenlistThread is responsible for traversing this
        // list trying to obtain the outcome for the enlistments.  All access, read or write, to this
        // list should get a lock on the list.
        // Special Note: If you are going to lock both the OletxResourceManager object AND the
        // reenlistList, lock the reenlistList FIRST.
        internal ArrayList reenlistList;

        // reenlistPendingList is also a simple ArrayList of OletxEnlistment objects.  But for these
        // we have received the outcome from the proxy and have called into the RM to deliver the
        // notification, but the RM has not yet called EnlistmentDone to let us know that the outcome
        // has been processed.  This list must be empty, in addition to the reenlistList, in order for
        // the ReenlistThread to call RecoveryComplete and not be rescheduled.  Access to this list
        // should be protected by locking the reenlistList.  The lists are always accessed together,
        // so there is no reason to grab two locks.
        internal ArrayList reenlistPendingList;

        // This is where we keep the reenlistThread and thread timer values.  If there is a reenlist thread running,
        // reenlistThread will be non-null.  If reenlistThreadTimer is non-null, we have a timer scheduled which will
        // fire off a reenlist thread when it expires.  Only one or the other should be non-null at a time.  However, they
        // could both be null, which means that there is no reenlist thread running and there is no timer scheduled to
        // create one.  Access to these members should be done only after obtaining a lock on the OletxResourceManager object.
        internal Timer reenlistThreadTimer;
        internal Thread reenlistThread;

        // This boolean is set to true if the resource manager application has called RecoveryComplete.
        // A lock on the OletxResourceManager instance will be obtained when retrieving or modifying
        // this value.  Before calling ReenlistComplete on the DTC proxy, this value must be true.
        private bool recoveryCompleteCalledByApplication;

        internal OletxResourceManager(
            OletxTransactionManager transactionManager,
            Guid resourceManagerIdentifier
            )
        {
            Debug.Assert( null != transactionManager, "Argument is null" );

            // This will get set later, after the resource manager is created with the proxy.
            this.resourceManagerShim = null;
            this.oletxTransactionManager = transactionManager;
            this.resourceManagerIdentifier = resourceManagerIdentifier;

            this.enlistmentHashtable = new Hashtable();
            this.reenlistList = new ArrayList();
            this.reenlistPendingList = new ArrayList();

            reenlistThreadTimer = null;
            reenlistThread = null;
            recoveryCompleteCalledByApplication = false;
        }

        internal IResourceManagerShim ResourceManagerShim
        {
            get
            {
                IResourceManagerShim localResourceManagerShim = null;

                if ( null == this.resourceManagerShim )
                {
                    lock ( this )
                    {
                        if ( null == this.resourceManagerShim )
                        {
                            this.oletxTransactionManager.dtcTransactionManagerLock.AcquireReaderLock( -1 );
                            try
                            {
                                Guid rmGuid = this.resourceManagerIdentifier;
                                IntPtr handle = IntPtr.Zero;

                                RuntimeHelpers.PrepareConstrainedRegions();
                                try
                                {
                                    handle = HandleTable.AllocHandle( this );

                                    this.oletxTransactionManager.DtcTransactionManager.ProxyShimFactory.CreateResourceManager(
                                        rmGuid,
                                        handle,
                                        out localResourceManagerShim );
                                }
                                finally
                                {
                                    if ( null == localResourceManagerShim && handle != IntPtr.Zero )
                                    {
                                        HandleTable.FreeHandle( handle );
                                    }
                                }

                            }
                            catch ( COMException ex )
                            {
                                if ( ( NativeMethods.XACT_E_CONNECTION_DOWN == ex.ErrorCode ) ||
                                    ( NativeMethods.XACT_E_TMNOTAVAILABLE == ex.ErrorCode )
                                    )
                                {
                                    // Just to make sure...
                                    localResourceManagerShim = null;
                                    if ( DiagnosticTrace.Verbose )
                                    {
                                        ExceptionConsumedTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                                            ex );
                                    }
                                }
                                else
                                {
                                    throw;
                                }
                            }
                            catch ( TransactionException ex )
                            {

                                COMException comEx = ex.InnerException as COMException;
                                if ( null != comEx )
                                {
                                    // Tolerate TM down.
                                    if ( ( NativeMethods.XACT_E_CONNECTION_DOWN == comEx.ErrorCode ) ||
                                        ( NativeMethods.XACT_E_TMNOTAVAILABLE == comEx.ErrorCode )
                                        )
                                    {
                                        // Just to make sure...
                                        localResourceManagerShim = null;
                                        if ( DiagnosticTrace.Verbose )
                                        {
                                            ExceptionConsumedTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                                                ex );
                                        }
                                    }
                                    else
                                    {
                                        throw;
                                    }
                                }
                                else
                                {
                                    throw;
                                }
                            }
                            finally
                            {
                                this.oletxTransactionManager.dtcTransactionManagerLock.ReleaseReaderLock();
                            }
                            Thread.MemoryBarrier();
                            this.resourceManagerShim = localResourceManagerShim;
                        }
                    }
                }
                return this.resourceManagerShim;
            }

            set
            {
                Debug.Assert( null == value, "set_ResourceManagerShim, value not null" );
                this.resourceManagerShim = value;
            }
        }

        internal bool CallProxyReenlistComplete()
        {
            bool success = false;
            if ( RecoveryCompleteCalledByApplication )
            {
                IResourceManagerShim localResourceManagerShim = null;
                try
                {
                    localResourceManagerShim = this.ResourceManagerShim;
                    if ( null != localResourceManagerShim )
                    {
                        localResourceManagerShim.ReenlistComplete();
                        success = true;
                    }
                    // If we don't have an iResourceManagerOletx, just tell the caller that
                    // we weren't successful and it will schedule a retry.
                }
                catch ( COMException ex )
                {
                    // If we get a TMDown error, eat it and indicate that we were unsuccessful.
                    if ( ( NativeMethods.XACT_E_CONNECTION_DOWN == ex.ErrorCode ) ||
                        ( NativeMethods.XACT_E_TMNOTAVAILABLE == ex.ErrorCode )
                        )
                    {
                        success = false;
                        if ( DiagnosticTrace.Verbose )
                        {
                            ExceptionConsumedTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                                ex );
                        }
                    }

                    // We might get an XACT_E_RECOVERYALREADYDONE if there are multiple OletxTransactionManager
                    // objects for the same backend TM.  We can safely ignore this error.
                    else if ( NativeMethods.XACT_E_RECOVERYALREADYDONE != ex.ErrorCode )
                    {
                        OletxTransactionManager.ProxyException( ex );
                        throw;
                    }
                    // Getting XACT_E_RECOVERYALREADYDONE is considered success.
                    else
                    {
                        success = true;
                    }
                }
                finally
                {
                    localResourceManagerShim = null;
                }
            }
            else  // The application has not yet called RecoveryComplete, so lie just a little.
            {
                success = true;
            }

            return success;
        }

        internal bool RecoveryCompleteCalledByApplication
        {
            get
            {
                return this.recoveryCompleteCalledByApplication;
            }

            set
            {
                this.recoveryCompleteCalledByApplication = value;
            }
        }

        // This is called by the internal RM when it gets a TM Down notification.  This routine will
        // tell the enlistments about the TMDown from the internal RM.  The enlistments will then
        // decide what to do, based on their state.  This is mainly to work around COMPlus bug 36760/36758,
        // where Phase0 enlistments get Phase0Request( abortHint = false ) when the TM goes down.  We want
        // to try to avoid telling the application to prepare when we know the transaction will abort.
        // We can't do this out of the normal TMDown notification to the RM because it is too late.  The
        // Phase0Request gets sent before the TMDown notification.
        internal void TMDownFromInternalRM( OletxTransactionManager oletxTM )
        {
            Hashtable localEnlistmentHashtable = null;
            IDictionaryEnumerator enlistEnum = null;
            OletxEnlistment enlistment = null;

            // If the internal RM got a TMDown, we will shortly, so null out our ResourceManagerShim now.
            this.ResourceManagerShim = null;

            // Make our own copy of the hashtable of enlistments.
            lock ( enlistmentHashtable.SyncRoot )
            {
                localEnlistmentHashtable = (Hashtable) this.enlistmentHashtable.Clone();
            }

            // Tell all of our enlistments that the TM went down.  The proxy only
            // tells enlistments that are in the Prepared state, but we want our Phase0
            // enlistments to know so they can avoid sending Prepare when they get a
            // Phase0Request - COMPlus bug 36760/36758.
            enlistEnum = localEnlistmentHashtable.GetEnumerator();
            while ( enlistEnum.MoveNext() )
            {
                enlistment = enlistEnum.Value as OletxEnlistment;
                if ( null != enlistment )
                {
                    enlistment.TMDownFromInternalRM( oletxTM );
                }
            }

        }

        #region IResourceManagerSink
        public void TMDown()
        {
            // The ResourceManagerShim was already set to null by TMDownFromInternalRM, so we don't need to do it again here.
            // Just start the ReenlistThread.
            StartReenlistThread();

            return;
        }

        #endregion

        internal OletxEnlistment EnlistDurable(
            OletxTransaction oletxTransaction,
            bool canDoSinglePhase,
            IEnlistmentNotificationInternal enlistmentNotification,
            EnlistmentOptions enlistmentOptions
            )
        {
            IResourceManagerShim localResourceManagerShim = null;

            Debug.Assert( null != oletxTransaction, "Argument is null" );
            Debug.Assert( null != enlistmentNotification, "Argument is null" );

            IEnlistmentShim enlistmentShim = null;
            IPhase0EnlistmentShim phase0Shim = null;
            Guid txUow = Guid.Empty;
            IntPtr handlePhase0 = IntPtr.Zero;
            bool phase0EnlistSucceeded = false;
            bool undecidedEnlistmentsIncremented = false;

            // Create our enlistment object.
            OletxEnlistment enlistment = new OletxEnlistment(
                canDoSinglePhase,
                enlistmentNotification,
                oletxTransaction.RealTransaction.TxGuid,
                enlistmentOptions,
                this,
                oletxTransaction
                );

            bool enlistmentSucceeded = false;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                if ( (enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0 )
                {
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try { }
                    finally
                    {
                        oletxTransaction.RealTransaction.IncrementUndecidedEnlistments();
                        undecidedEnlistmentsIncremented = true;
                    }
                }

                // This entire sequense needs to be executed before we can go on.
                lock ( enlistment )
                {
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                        // Do the enlistment on the proxy.
                        localResourceManagerShim = this.ResourceManagerShim;
                        if ( null == localResourceManagerShim )
                        {
                            // The TM must be down.  Throw the appropriate exception.
                            throw TransactionManagerCommunicationException.Create( SR.GetString( SR.TraceSourceOletx),  null );
                        }

                        if ( (enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0 )
                        {
                            // We need to create an EnlistmentNotifyShim if native threads are not allowed to enter managed code.
                            handlePhase0 = HandleTable.AllocHandle( enlistment );

                            RuntimeHelpers.PrepareConstrainedRegions();
                            try { }
                            finally
                            {
                                oletxTransaction.RealTransaction.TransactionShim.Phase0Enlist(
                                    handlePhase0,
                                    out phase0Shim );
                                phase0EnlistSucceeded = true;
                            }
                            enlistment.Phase0EnlistmentShim = phase0Shim;
                        }

                        enlistment.phase1Handle = HandleTable.AllocHandle( enlistment );
                        localResourceManagerShim.Enlist(
                            oletxTransaction.RealTransaction.TransactionShim,
                            enlistment.phase1Handle,
                            out enlistmentShim );

                        enlistment.EnlistmentShim = enlistmentShim;
                    }
                    catch (COMException comException)
                    {
                        // There is no string mapping for XACT_E_TOOMANY_ENLISTMENTS, so we need to do it here.
                        if ( NativeMethods.XACT_E_TOOMANY_ENLISTMENTS == comException.ErrorCode )
                        {
                            throw TransactionException.Create(
                                SR.GetString( SR.TraceSourceOletx ),
                                SR.GetString( SR.OletxTooManyEnlistments ),
                                comException, enlistment == null ? Guid.Empty : enlistment.DistributedTxId );
                        }

                        OletxTransactionManager.ProxyException( comException );

                        throw;
                    }
                    finally
                    {
                        if ( enlistment.EnlistmentShim == null )
                        {
                            // If the enlistment shim was never assigned then something blew up.
                            // Perform some cleanup.
                            if ( handlePhase0 != IntPtr.Zero && !phase0EnlistSucceeded )
                            {
                                // Only clean up the phase0 handle if the phase 0 enlistment did not succeed.
                                // This is because the notification processing code expects it to exist.
                                HandleTable.FreeHandle( handlePhase0 );
                            }

                            if ( enlistment.phase1Handle != IntPtr.Zero )
                            {
                                HandleTable.FreeHandle( enlistment.phase1Handle );
                            }

                            // Note this code used to call unenlist however this allows race conditions where
                            // it is unclear if the handlePhase0 should be freed or not.  The notification
                            // thread should get a phase0Request and it will free the Handle at that point.
                        }
                    }
                }

                enlistmentSucceeded = true;
            }
            finally
            {
                if ( !enlistmentSucceeded &&
                    ((enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0) &&
                    undecidedEnlistmentsIncremented )
                {
                    oletxTransaction.RealTransaction.DecrementUndecidedEnlistments();
                }
            }

            return enlistment;
        }

        internal OletxEnlistment Reenlist(
            int prepareInfoLength,
            byte[] prepareInfo,
            IEnlistmentNotificationInternal enlistmentNotification
            )
        {
            OletxTransactionOutcome outcome = OletxTransactionOutcome.NotKnownYet;
            OletxTransactionStatus xactStatus = OletxTransactionStatus.OLETX_TRANSACTION_STATUS_NONE;

            // Put the recovery information into a stream.
            MemoryStream stream = new MemoryStream( prepareInfo );

            // First extract the OletxRecoveryInformation from the stream.
            IFormatter formatter = new BinaryFormatter();
            OletxRecoveryInformation oletxRecoveryInformation;
            try
            {
                oletxRecoveryInformation = formatter.Deserialize( stream ) as OletxRecoveryInformation;
            }
            catch (SerializationException se)
            {
                throw new ArgumentException( SR.GetString( SR.InvalidArgument ), "prepareInfo", se );
            }

            if ( null == oletxRecoveryInformation )
            {
                throw new ArgumentException( SR.GetString( SR.InvalidArgument ), "prepareInfo" );
            }

            // Verify that the resource manager guid in the recovery info matches that of the calling resource manager.
            byte[] rmGuidArray = new byte[16];
            for ( int i = 0; i < 16; i++ )
            {
                rmGuidArray[i] = oletxRecoveryInformation.proxyRecoveryInformation[i + 16];
            }
            Guid rmGuid = new Guid( rmGuidArray );
            if ( rmGuid != this.resourceManagerIdentifier )
            {
                throw TransactionException.Create( SR.GetString( SR.TraceSourceOletx ), SR.GetString( SR.ResourceManagerIdDoesNotMatchRecoveryInformation ), null );
            }

            // Ask the proxy resource manager to reenlist.
            IResourceManagerShim localResourceManagerShim = null;
            try
            {
                localResourceManagerShim = this.ResourceManagerShim;
                if ( null == localResourceManagerShim )
                {
                    // The TM must be down.  Throw the exception that will get caught below and will cause
                    // the enlistment to start the ReenlistThread.  The TMDown thread will be trying to reestablish
                    // connection with the TM and will start the reenlist thread when it does.
                    throw new COMException( SR.GetString( SR.DtcTransactionManagerUnavailable ), NativeMethods.XACT_E_CONNECTION_DOWN );
                }

                // Only wait for 5 milliseconds.  If the TM doesn't have the outcome now, we will
                // put the enlistment on the reenlistList for later processing.
                localResourceManagerShim.Reenlist(
                    Convert.ToUInt32( oletxRecoveryInformation.proxyRecoveryInformation.Length, CultureInfo.InvariantCulture ),
                    oletxRecoveryInformation.proxyRecoveryInformation,
                    out outcome
                    );

                if ( OletxTransactionOutcome.Committed == outcome )
                {
                    xactStatus = OletxTransactionStatus.OLETX_TRANSACTION_STATUS_COMMITTED;
                }
                else if ( OletxTransactionOutcome.Aborted == outcome )
                {
                    xactStatus = OletxTransactionStatus.OLETX_TRANSACTION_STATUS_ABORTED;
                }
                else  // we must not know the outcome yet.
                {
                    xactStatus = OletxTransactionStatus.OLETX_TRANSACTION_STATUS_PREPARED;
                    StartReenlistThread();
                }
            }
            catch ( COMException ex )
            {
                if ( NativeMethods.XACT_E_CONNECTION_DOWN == ex.ErrorCode )
                {
                    xactStatus = OletxTransactionStatus.OLETX_TRANSACTION_STATUS_PREPARED;
                    this.ResourceManagerShim = null;
                    StartReenlistThread();
                    if ( DiagnosticTrace.Verbose )
                    {
                        ExceptionConsumedTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                            ex );
                    }

                }
                else
                {
                    throw;
                }
            }
            finally
            {
                localResourceManagerShim = null;
            }

            // Now create our enlistment to tell the client the outcome.
            OletxEnlistment enlistment = new OletxEnlistment(
                enlistmentNotification,
                xactStatus,
                oletxRecoveryInformation.proxyRecoveryInformation,
                this
                );

            return enlistment;
        }

        internal void RecoveryComplete()
        {
            Timer localTimer = null;

            // Remember that the application has called RecoveryComplete.
            RecoveryCompleteCalledByApplication = true;

            try
            {
                // Remove the OletxEnlistment objects from the reenlist list because the RM says it doesn't
                // have any unresolved transactions, so we don't need to keep asking and the reenlist thread can exit.
                // Leave the reenlistPendingList alone.  If we have notifications outstanding, we still can't remove those.
                lock ( this.reenlistList )
                {
                    // If the ReenlistThread is not running and there are no reenlistPendingList entries, we need to call ReenlistComplete ourself.
                    lock ( this )
                    {
                        if ( ( 0 == this.reenlistList.Count ) && ( 0 == this.reenlistPendingList.Count ) )
                        {
                            if ( null != this.reenlistThreadTimer )
                            {
                                // If we have a pending reenlistThreadTimer, cancel it.  We do the cancel
                                // in the finally block to satisfy FXCop.
                                localTimer = this.reenlistThreadTimer;
                                this.reenlistThreadTimer = null;
                            }

                            // Try to tell the proxy RenlistmentComplete.
                            bool success = CallProxyReenlistComplete();
                            if ( !success )
                            {
                                // We are now responsible for calling RecoveryComplete. Fire up the ReenlistThread
                                // to do it for us.
                                StartReenlistThread();
                            }
                        }
                        else
                        {
                            StartReenlistThread();
                        }
                    }
                }
            }
            finally
            {
                if ( null != localTimer )
                {
                    localTimer.Dispose();
                }
            }


            return;

        }

        internal void StartReenlistThread()
        {
            // We are not going to check the reenlistList.Count.  Just always start the thread.  We do this because
            // if we get a COMException from calling ReenlistComplete, we start the reenlistThreadTimer to retry it for us
            // in the background.
            lock ( this )
            {
                // We don't need a MemoryBarrier here because all access to the reenlistThreadTimer member is done while
                // holding a lock on the OletxResourceManager object.
                if ( ( null == this.reenlistThreadTimer ) && ( null == this.reenlistThread ) )
                {
                    this.reenlistThreadTimer = new Timer( this.ReenlistThread,
                        this,
                        10,
                        Timeout.Infinite
                        );
                }
            }
        }

        // This routine searches the reenlistPendingList for the specified enlistment and if it finds
        // it, removes it from the list.  An enlistment calls this routine when it is "finishing" because
        // the RM has called EnlistmentDone or it was InDoubt.  But it only calls it if the enlistment does NOT
        // have a WrappedTransactionEnlistmentAsync value, indicating that it is a recovery enlistment.
        internal void RemoveFromReenlistPending( OletxEnlistment enlistment )
        {
            // We lock the reenlistList because we have decided to lock that list when accessing either
            // the reenlistList or the reenlistPendingList.
            lock ( reenlistList )
            {
                // This will do a linear search of the list, but that is what we need to do because
                // the enlistments may change indicies while notifications are outstanding.  Also,
                // this does not throw if the enlistment isn't on the list.
                reenlistPendingList.Remove( enlistment );

                lock ( this )
                {
                    // If we have a ReenlistThread timer and both the reenlistList and the reenlistPendingList
                    // are empty, kick the ReenlistThread now.
                    if ( ( null != this.reenlistThreadTimer ) &&
                        ( 0 == this.reenlistList.Count ) &&
                        ( 0 == this.reenlistPendingList.Count )
                        )
                    {
                        if ( !this.reenlistThreadTimer.Change( 0, Timeout.Infinite ))
                        {
                            throw TransactionException.CreateInvalidOperationException(
                                SR.GetString( SR.TraceSourceLtm ),
                                SR.GetString(SR.UnexpectedTimerFailure),
                                null
                                );
                        }
                    }
                }
            }
        }

        internal void ReenlistThread( object state )
        {
            int localLoopCount = 0;
            bool done = false;
            OletxEnlistment localEnlistment = null;
            IResourceManagerShim localResourceManagerShim = null;
            bool success = false;
            Timer localTimer = null;
            bool disposeLocalTimer = false;

            OletxResourceManager resourceManager = (OletxResourceManager) state;

            try
            {
                if ( DiagnosticTrace.Information )
                {
                    MethodEnteredTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                        "OletxResourceManager.ReenlistThread"
                        );
                }

                lock ( resourceManager )
                {
                    localResourceManagerShim = resourceManager.ResourceManagerShim;
                    localTimer = resourceManager.reenlistThreadTimer;
                    resourceManager.reenlistThreadTimer = null;
                    resourceManager.reenlistThread = Thread.CurrentThread;
                }

                // We only want to do work if we have a resourceManagerShim.
                if ( null != localResourceManagerShim )
                {
                    lock ( resourceManager.reenlistList )
                    {
                        // Get the current count on the list.
                        localLoopCount = resourceManager.reenlistList.Count;
                    }

                    done = false;
                    while ( !done && ( localLoopCount > 0 ) && ( null != localResourceManagerShim ) )
                    {
                        lock ( resourceManager.reenlistList )
                        {
                            localEnlistment = null;
                            localLoopCount--;
                            if ( 0 == resourceManager.reenlistList.Count )
                            {
                                done = true;
                            }
                            else
                            {
                                localEnlistment = resourceManager.reenlistList[0] as OletxEnlistment;
                                if ( null == localEnlistment )
                                {
                                    //TODO need resource string for this exception.
                                    if ( DiagnosticTrace.Critical )
                                    {
                                        InternalErrorTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                                            ""
                                            );
                                    }

                                    throw TransactionException.Create( SR.GetString( SR.TraceSourceOletx), SR.GetString( SR.InternalError ), null );
                                }

                                resourceManager.reenlistList.RemoveAt( 0 );
                                Object syncRoot = localEnlistment;
                                lock ( syncRoot )
                                {
                                    if ( OletxEnlistment.OletxEnlistmentState.Done == localEnlistment.State )
                                    {
                                        // We may be racing with a RecoveryComplete here.  Just forget about this
                                        // enlistment.
                                        localEnlistment = null;
                                    }

                                    else if ( OletxEnlistment.OletxEnlistmentState.Prepared != localEnlistment.State )
                                    {
                                        // The app hasn't yet responded to Prepare, so we don't know
                                        // if it is indoubt or not yet.  So just re-add it to the end
                                        // of the list.
                                        resourceManager.reenlistList.Add(
                                            localEnlistment
                                            );
                                        localEnlistment = null;
                                    }
                                }
                            }
                        }

                        if ( null != localEnlistment )
                        {
                            OletxTransactionOutcome localOutcome = OletxTransactionOutcome.NotKnownYet;
                            try
                            {
                                Debug.Assert( null != localResourceManagerShim, "ReenlistThread - localResourceManagerShim is null" );

                                // Make sure we have a prepare info.
                                if ( null == localEnlistment.ProxyPrepareInfoByteArray )
                                {
                                    Debug.Assert( false, string.Format( null, "this.prepareInfoByteArray == null in RecoveryInformation()" ));
                                    if ( DiagnosticTrace.Critical )
                                    {
                                        InternalErrorTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                                            ""
                                            );
                                    }

                                    throw TransactionException.Create( SR.GetString( SR.TraceSourceOletx), SR.GetString( SR.InternalError ), null );
                                }
                                localResourceManagerShim.Reenlist(
                                    (UInt32) localEnlistment.ProxyPrepareInfoByteArray.Length,
                                    localEnlistment.ProxyPrepareInfoByteArray,
                                    out localOutcome );

                                if ( OletxTransactionOutcome.NotKnownYet == localOutcome )
                                {
                                    Object syncRoot = localEnlistment;
                                    lock ( syncRoot )
                                    {
                                        if ( OletxEnlistment.OletxEnlistmentState.Done == localEnlistment.State )
                                        {
                                            // We may be racing with a RecoveryComplete here.  Just forget about this
                                            // enlistment.
                                            localEnlistment = null;
                                        }
                                        else
                                        {
                                            // Put the enlistment back on the end of the list for retry later.
                                            lock ( resourceManager.reenlistList )
                                            {
                                                resourceManager.reenlistList.Add(
                                                    localEnlistment
                                                    );
                                                localEnlistment = null;
                                            }
                                        }
                                    }
                                }

                            }
                            catch ( COMException ex ) // or whatever exception gets thrown if we get a bad hr.
                            {
                                if ( NativeMethods.XACT_E_CONNECTION_DOWN == ex.ErrorCode )
                                {
                                    if ( DiagnosticTrace.Verbose )
                                    {
                                        ExceptionConsumedTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                                            ex );
                                    }
                                    if ( NativeMethods.XACT_E_CONNECTION_DOWN == ex.ErrorCode )
                                    {
                                        // Release the resource manager so we can create a new one.
                                        resourceManager.ResourceManagerShim = null;

                                        // Now create a new resource manager with the proxy.
                                        localResourceManagerShim = resourceManager.ResourceManagerShim;
                                    }

                                }
                                else
                                {
                                    // Unexpected exception, rethrow it.
                                    throw;
                                }
                            }

                            // If we get here and we still have localEnlistment, then we got the outcome.
                            if ( null != localEnlistment )
                            {
                                Object syncRoot = localEnlistment;
                                lock ( syncRoot )
                                {
                                    if ( OletxEnlistment.OletxEnlistmentState.Done == localEnlistment.State )
                                    {
                                        // We may be racing with a RecoveryComplete here.  Just forget about this
                                        // enlistment.
                                        localEnlistment = null;
                                    }
                                    else
                                    {
                                        // We are going to send the notification to the RM.  We need to put the
                                        // enlistment on the reenlistPendingList.  We lock the reenlistList because
                                        // we have decided that is the lock that protects both lists.  The entry will
                                        // be taken off the reenlistPendingList when the enlistment has
                                        // EnlistmentDone called on it.  The enlistment will call
                                        // RemoveFromReenlistPending.
                                        lock ( resourceManager.reenlistList )
                                        {
                                            resourceManager.reenlistPendingList.Add( localEnlistment );
                                        }

                                        if ( OletxTransactionOutcome.Committed == localOutcome )
                                        {
                                            localEnlistment.State = OletxEnlistment.OletxEnlistmentState.Committing;
                                            if ( DiagnosticTrace.Verbose )
                                            {
                                                EnlistmentNotificationCallTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                                                    localEnlistment.EnlistmentTraceId,
                                                    NotificationCall.Commit
                                                    );
                                            }

                                            localEnlistment.EnlistmentNotification.Commit( localEnlistment );
                                        }
                                        else if ( OletxTransactionOutcome.Aborted == localOutcome )
                                        {
                                            localEnlistment.State = OletxEnlistment.OletxEnlistmentState.Aborting;
                                            if ( DiagnosticTrace.Verbose )
                                            {
                                                EnlistmentNotificationCallTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                                                    localEnlistment.EnlistmentTraceId,
                                                    NotificationCall.Rollback
                                                    );
                                            }

                                            localEnlistment.EnlistmentNotification.Rollback( localEnlistment );
                                        }
                                        else
                                        {
                                            if ( DiagnosticTrace.Critical )
                                            {
                                                InternalErrorTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                                                    ""
                                                    );
                                            }

                                            throw TransactionException.Create( SR.GetString( SR.TraceSourceOletx ), SR.GetString( SR.InternalError ), null );
                                        }
                                    }
                                }
                            } // end of if null != localEnlistment
                        }  // end of if null != localEnlistment
                    }
                }

                localResourceManagerShim = null;

                // Check to see if there is more work to do.
                lock ( resourceManager.reenlistList )
                {
                    lock ( resourceManager )
                    {
                        // Get the current count on the list.
                        localLoopCount = resourceManager.reenlistList.Count;
                        if ( ( 0 >= localLoopCount ) && ( 0 >= resourceManager.reenlistPendingList.Count ) )
                        {
                            // No more entries on the list.  Try calling ReenlistComplete on the proxy, if
                            // appropriate.
                            // If the application has called RecoveryComplete,
                            // we are responsible for calling ReenlistComplete on the
                            // proxy.
                            success = resourceManager.CallProxyReenlistComplete();
                            if ( success )
                            {
                                // Okay, the reenlist thread is done and we don't need to schedule another one.
                                disposeLocalTimer = true;
                            }
                            else
                            {
                                // We couldn't talk to the proxy to do ReenlistComplete, so schedule
                                // the thread again for 10 seconds from now.
                                resourceManager.reenlistThreadTimer = localTimer;
                                if ( !localTimer.Change( 10000, Timeout.Infinite ))
                                {
                                    throw TransactionException.CreateInvalidOperationException(
                                        SR.GetString( SR.TraceSourceLtm ),
                                        SR.GetString(SR.UnexpectedTimerFailure),
                                        null
                                        );
                                }
                            }
                        }
                        else
                        {
                            // There are still entries on the list, so they must not be
                            // resovled, yet.  Schedule the thread again in 10 seconds.
                            resourceManager.reenlistThreadTimer = localTimer;
                            if ( !localTimer.Change( 10000, Timeout.Infinite ))
                            {
                                throw TransactionException.CreateInvalidOperationException(
                                    SR.GetString( SR.TraceSourceLtm ),
                                    SR.GetString(SR.UnexpectedTimerFailure),
                                    null
                                    );
                            }

                        }

                        resourceManager.reenlistThread = null;
                    }
                    if ( DiagnosticTrace.Information )
                    {
                        MethodExitedTraceRecord.Trace( SR.GetString( SR.TraceSourceOletx ),
                            "OletxResourceManager.ReenlistThread"
                            );
                    }
                    return;
                }

            }  // end of outer-most try
            finally
            {
                localResourceManagerShim = null;
                if ( ( disposeLocalTimer ) && ( null != localTimer ) )
                {
                    localTimer.Dispose();
                }
            }
        }  // end of ReenlistThread method;
    }

    // This is the base class for all enlistment objects.  The enlistment objects provide the callback
    // that is made from the application and pass it through to the proxy.
    abstract class OletxBaseEnlistment
    {
        protected Guid enlistmentGuid;
        protected OletxResourceManager oletxResourceManager;
        protected OletxTransaction oletxTransaction;
        internal OletxTransaction OletxTransaction
        {
            get
            {
                return this.oletxTransaction;
            }
        }

        internal Guid DistributedTxId
        {
            get
            {
                Guid returnValue = Guid.Empty;

                if (this.OletxTransaction != null)
                {
                    returnValue = this.OletxTransaction.DistributedTxId;
                }
                return returnValue;
            }
        }

        protected string transactionGuidString;
        protected int enlistmentId;
        // this needs to be internal so it can be set from the recovery information during Reenlist.
        internal EnlistmentTraceIdentifier traceIdentifier;

        // Owning public Enlistment object
        protected InternalEnlistment internalEnlistment;

        public OletxBaseEnlistment(
            OletxResourceManager oletxResourceManager,
            OletxTransaction oletxTransaction
            )
        {
            Guid resourceManagerId = Guid.Empty;

            enlistmentGuid = Guid.NewGuid();
            this.oletxResourceManager = oletxResourceManager;
            this.oletxTransaction = oletxTransaction;
            if ( null != oletxTransaction )
            {
                this.enlistmentId = oletxTransaction.realOletxTransaction.enlistmentCount++;
                this.transactionGuidString = oletxTransaction.realOletxTransaction.TxGuid.ToString();
            }
            else
            {
                this.transactionGuidString = Guid.Empty.ToString();
            }
            this.traceIdentifier = EnlistmentTraceIdentifier.Empty;
        }

        protected EnlistmentTraceIdentifier InternalTraceIdentifier
        {
            get
            {
                if ( EnlistmentTraceIdentifier.Empty == this.traceIdentifier )
                {
                    lock ( this )
                    {
                        if ( EnlistmentTraceIdentifier.Empty == this.traceIdentifier )
                        {
                            Guid rmId = Guid.Empty;
                            if ( null != oletxResourceManager )
                            {
                                rmId = this.oletxResourceManager.resourceManagerIdentifier;
                            }
                            EnlistmentTraceIdentifier temp;
                            if ( null != this.oletxTransaction )
                            {
                                temp = new EnlistmentTraceIdentifier( rmId, oletxTransaction.TransactionTraceId, this.enlistmentId );
                            }
                            else
                            {
                                TransactionTraceIdentifier txTraceId = new TransactionTraceIdentifier( this.transactionGuidString, 0 );
                                temp = new EnlistmentTraceIdentifier( rmId, txTraceId, this.enlistmentId );
                            }
                            Thread.MemoryBarrier();
                            this.traceIdentifier = temp;
                        }
                    }
                }

                return this.traceIdentifier;
            }
        }

        protected void AddToEnlistmentTable()
        {
            lock ( oletxResourceManager.enlistmentHashtable.SyncRoot )
            {
                oletxResourceManager.enlistmentHashtable.Add( enlistmentGuid, this );
            }
        }

        protected void RemoveFromEnlistmentTable()
        {
            lock ( oletxResourceManager.enlistmentHashtable.SyncRoot )
            {
                oletxResourceManager.enlistmentHashtable.Remove( enlistmentGuid );
            }
        }

    }


}  // end of namespace
