// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Transactions.DtcProxyShim;

namespace System.Transactions.Oletx;

internal sealed class OletxResourceManager
{
    internal Guid ResourceManagerIdentifier;

    internal ResourceManagerShim? resourceManagerShim;
    internal Hashtable EnlistmentHashtable;
    internal static Hashtable VolatileEnlistmentHashtable = new Hashtable();
    internal OletxTransactionManager OletxTransactionManager;

    // reenlistList is a simple ArrayList of OletxEnlistment objects that are either in the
    // Preparing or Prepared state when we receive a TMDown notification or have had
    // ReenlistTransaction called for them.  The ReenlistThread is responsible for traversing this
    // list trying to obtain the outcome for the enlistments.  All access, read or write, to this
    // list should get a lock on the list.
    // Special Note: If you are going to lock both the OletxResourceManager object AND the
    // reenlistList, lock the reenlistList FIRST.
    internal ArrayList ReenlistList;

    // reenlistPendingList is also a simple ArrayList of OletxEnlistment objects.  But for these
    // we have received the outcome from the proxy and have called into the RM to deliver the
    // notification, but the RM has not yet called EnlistmentDone to let us know that the outcome
    // has been processed.  This list must be empty, in addition to the reenlistList, in order for
    // the ReenlistThread to call RecoveryComplete and not be rescheduled.  Access to this list
    // should be protected by locking the reenlistList.  The lists are always accessed together,
    // so there is no reason to grab two locks.
    internal ArrayList ReenlistPendingList;

    // This is where we keep the reenlistThread and thread timer values.  If there is a reenlist thread running,
    // reenlistThread will be non-null.  If reenlistThreadTimer is non-null, we have a timer scheduled which will
    // fire off a reenlist thread when it expires.  Only one or the other should be non-null at a time.  However, they
    // could both be null, which means that there is no reenlist thread running and there is no timer scheduled to
    // create one.  Access to these members should be done only after obtaining a lock on the OletxResourceManager object.
    internal Timer? ReenlistThreadTimer;
    internal Thread? reenlistThread;

    // This boolean is set to true if the resource manager application has called RecoveryComplete.
    // A lock on the OletxResourceManager instance will be obtained when retrieving or modifying
    // this value.  Before calling ReenlistComplete on the DTC proxy, this value must be true.
    internal bool RecoveryCompleteCalledByApplication { get; set; }

    internal OletxResourceManager(OletxTransactionManager transactionManager, Guid resourceManagerIdentifier)
    {
        Debug.Assert(transactionManager != null, "Argument is null");

        // This will get set later, after the resource manager is created with the proxy.
        resourceManagerShim = null;
        OletxTransactionManager = transactionManager;
        ResourceManagerIdentifier = resourceManagerIdentifier;

        EnlistmentHashtable = new Hashtable();
        ReenlistList = new ArrayList();
        ReenlistPendingList = new ArrayList();

        ReenlistThreadTimer = null;
        reenlistThread = null;
        RecoveryCompleteCalledByApplication = false;
    }

    internal ResourceManagerShim? ResourceManagerShim
    {
        get
        {
            ResourceManagerShim? localResourceManagerShim = null;

            if (resourceManagerShim == null)
            {
                lock (this)
                {
                    if (resourceManagerShim == null)
                    {
                        OletxTransactionManager.DtcTransactionManagerLock.AcquireReaderLock(-1);
                        try
                        {
                            Guid rmGuid = ResourceManagerIdentifier;

                            OletxTransactionManager.DtcTransactionManager.ProxyShimFactory.CreateResourceManager(
                                rmGuid,
                                this,
                                out localResourceManagerShim);
                        }
                        catch (COMException ex)
                        {
                            if (ex.ErrorCode == OletxHelper.XACT_E_CONNECTION_DOWN ||
                                ex.ErrorCode == OletxHelper.XACT_E_TMNOTAVAILABLE)
                            {
                                // Just to make sure...
                                localResourceManagerShim = null;

                                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                                if (etwLog.IsEnabled())
                                {
                                    etwLog.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
                                }
                            }
                            else
                            {
                                throw;
                            }
                        }
                        catch (TransactionException ex)
                        {
                            if (ex.InnerException is COMException comEx)
                            {
                                // Tolerate TM down.
                                if (comEx.ErrorCode == OletxHelper.XACT_E_CONNECTION_DOWN ||
                                    comEx.ErrorCode == OletxHelper.XACT_E_TMNOTAVAILABLE)
                                {
                                    // Just to make sure...
                                    localResourceManagerShim = null;

                                    TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                                    if (etwLog.IsEnabled())
                                    {
                                        etwLog.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
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
                            OletxTransactionManager.DtcTransactionManagerLock.ReleaseReaderLock();
                        }
                        Thread.MemoryBarrier();
                        resourceManagerShim = localResourceManagerShim;
                    }
                }
            }
            return resourceManagerShim;
        }

        set
        {
            Debug.Assert(value == null, "set_ResourceManagerShim, value not null");
            resourceManagerShim = value;
        }
    }

    internal bool CallProxyReenlistComplete()
    {
        bool success = false;
        if (RecoveryCompleteCalledByApplication)
        {
            ResourceManagerShim? localResourceManagerShim;
            try
            {
                localResourceManagerShim = ResourceManagerShim;
                if (localResourceManagerShim != null)
                {
                    localResourceManagerShim.ReenlistComplete();
                    success = true;
                }
                // If we don't have an iResourceManagerOletx, just tell the caller that
                // we weren't successful and it will schedule a retry.
            }
            catch (COMException ex)
            {
                // If we get a TMDown error, eat it and indicate that we were unsuccessful.
                if (ex.ErrorCode == OletxHelper.XACT_E_CONNECTION_DOWN ||
                    ex.ErrorCode == OletxHelper.XACT_E_TMNOTAVAILABLE)
                {
                    success = false;

                    TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                    if (etwLog.IsEnabled())
                    {
                        etwLog.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
                    }
                }

                // We might get an XACT_E_RECOVERYALREADYDONE if there are multiple OletxTransactionManager
                // objects for the same backend TM.  We can safely ignore this error.
                else if (ex.ErrorCode != OletxHelper.XACT_E_RECOVERYALREADYDONE)
                {
                    OletxTransactionManager.ProxyException(ex);
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

    // This is called by the internal RM when it gets a TM Down notification.  This routine will
    // tell the enlistments about the TMDown from the internal RM.  The enlistments will then
    // decide what to do, based on their state.  This is mainly to work around COMPlus bug 36760/36758,
    // where Phase0 enlistments get Phase0Request( abortHint = false ) when the TM goes down.  We want
    // to try to avoid telling the application to prepare when we know the transaction will abort.
    // We can't do this out of the normal TMDown notification to the RM because it is too late.  The
    // Phase0Request gets sent before the TMDown notification.
    internal void TMDownFromInternalRM(OletxTransactionManager oletxTM)
    {
        Hashtable localEnlistmentHashtable;
        IDictionaryEnumerator enlistEnum;
        OletxEnlistment? enlistment;

        // If the internal RM got a TMDown, we will shortly, so null out our ResourceManagerShim now.
        ResourceManagerShim = null;

        // Make our own copy of the hashtable of enlistments.
        lock (EnlistmentHashtable.SyncRoot)
        {
            localEnlistmentHashtable = (Hashtable)EnlistmentHashtable.Clone();
        }

        // Tell all of our enlistments that the TM went down.  The proxy only
        // tells enlistments that are in the Prepared state, but we want our Phase0
        // enlistments to know so they can avoid sending Prepare when they get a
        // Phase0Request - COMPlus bug 36760/36758.
        enlistEnum = localEnlistmentHashtable.GetEnumerator();
        while (enlistEnum.MoveNext())
        {
            enlistment = enlistEnum.Value as OletxEnlistment;
            enlistment?.TMDownFromInternalRM(oletxTM);
        }
    }

    public void TMDown()
    {
        // The ResourceManagerShim was already set to null by TMDownFromInternalRM, so we don't need to do it again here.
        // Just start the ReenlistThread.
        StartReenlistThread();
    }

    internal OletxEnlistment EnlistDurable(
        OletxTransaction oletxTransaction,
        bool canDoSinglePhase,
        IEnlistmentNotificationInternal enlistmentNotification,
        EnlistmentOptions enlistmentOptions)
    {
        ResourceManagerShim? localResourceManagerShim;

        Debug.Assert(oletxTransaction != null, "Argument is null");
        Debug.Assert(enlistmentNotification != null, "Argument is null");

        EnlistmentShim enlistmentShim;
        Phase0EnlistmentShim phase0Shim;
        Guid txUow = Guid.Empty;
        bool undecidedEnlistmentsIncremented = false;

        // Create our enlistment object.
        OletxEnlistment enlistment = new(
            canDoSinglePhase,
            enlistmentNotification,
            oletxTransaction.RealTransaction.TxGuid,
            enlistmentOptions,
            this,
            oletxTransaction);

        bool enlistmentSucceeded = false;

        try
        {
            if ((enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0)
            {
                oletxTransaction.RealTransaction.IncrementUndecidedEnlistments();
                undecidedEnlistmentsIncremented = true;
            }

            // This entire sequence needs to be executed before we can go on.
            lock (enlistment)
            {
                try
                {
                    // Do the enlistment on the proxy.
                    localResourceManagerShim = ResourceManagerShim;
                    if (localResourceManagerShim == null)
                    {
                        // The TM must be down.  Throw the appropriate exception.
                        throw TransactionManagerCommunicationException.Create(SR.TraceSourceOletx, null);
                    }

                    if ((enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0)
                    {
                        oletxTransaction.RealTransaction.TransactionShim.Phase0Enlist(enlistment, out phase0Shim);
                        enlistment.Phase0EnlistmentShim = phase0Shim;
                    }

                    localResourceManagerShim.Enlist(oletxTransaction.RealTransaction.TransactionShim, enlistment, out enlistmentShim);

                    enlistment.EnlistmentShim = enlistmentShim;
                }
                catch (COMException comException)
                {
                    // There is no string mapping for XACT_E_TOOMANY_ENLISTMENTS, so we need to do it here.
                    if (comException.ErrorCode == OletxHelper.XACT_E_TOOMANY_ENLISTMENTS)
                    {
                        throw TransactionException.Create(
                            SR.OletxTooManyEnlistments,
                            comException,
                            enlistment == null ? Guid.Empty : enlistment.DistributedTxId);
                    }

                    OletxTransactionManager.ProxyException(comException);

                    throw;
                }
            }

            enlistmentSucceeded = true;
        }
        finally
        {
            if (!enlistmentSucceeded &&
                (enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0 &&
                undecidedEnlistmentsIncremented)
            {
                oletxTransaction.RealTransaction.DecrementUndecidedEnlistments();
            }
        }

        return enlistment;
    }

    internal OletxEnlistment Reenlist(byte[] prepareInfo, IEnlistmentNotificationInternal enlistmentNotification)
    {
        OletxTransactionOutcome outcome = OletxTransactionOutcome.NotKnownYet;
        OletxTransactionStatus xactStatus = OletxTransactionStatus.OLETX_TRANSACTION_STATUS_NONE;

        if (prepareInfo == null)
        {
            throw new ArgumentException(SR.InvalidArgument, nameof(prepareInfo));
        }

        // Verify that the resource manager guid in the recovery info matches that of the calling resource manager.
        var rmGuid = new Guid(prepareInfo.AsSpan(16, 16));
        if (rmGuid != ResourceManagerIdentifier)
        {
            throw TransactionException.Create(SR.ResourceManagerIdDoesNotMatchRecoveryInformation, null);
        }

        // Ask the proxy resource manager to reenlist.
        ResourceManagerShim? localResourceManagerShim = null;
        try
        {
            localResourceManagerShim = ResourceManagerShim;
            if (localResourceManagerShim == null)
            {
                // The TM must be down.  Throw the exception that will get caught below and will cause
                // the enlistment to start the ReenlistThread.  The TMDown thread will be trying to reestablish
                // connection with the TM and will start the reenlist thread when it does.
                throw new COMException(SR.DtcTransactionManagerUnavailable, OletxHelper.XACT_E_CONNECTION_DOWN);
            }

            // Only wait for 5 milliseconds.  If the TM doesn't have the outcome now, we will
            // put the enlistment on the reenlistList for later processing.
            localResourceManagerShim.Reenlist(prepareInfo, out outcome);

            if (OletxTransactionOutcome.Committed == outcome)
            {
                xactStatus = OletxTransactionStatus.OLETX_TRANSACTION_STATUS_COMMITTED;
            }
            else if (OletxTransactionOutcome.Aborted == outcome)
            {
                xactStatus = OletxTransactionStatus.OLETX_TRANSACTION_STATUS_ABORTED;
            }
            else  // we must not know the outcome yet.
            {
                xactStatus = OletxTransactionStatus.OLETX_TRANSACTION_STATUS_PREPARED;
                StartReenlistThread();
            }
        }
        catch (COMException ex) when (ex.ErrorCode == OletxHelper.XACT_E_CONNECTION_DOWN)
        {
            xactStatus = OletxTransactionStatus.OLETX_TRANSACTION_STATUS_PREPARED;
            ResourceManagerShim = null;
            StartReenlistThread();

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
            }
        }
        finally
        {
            localResourceManagerShim = null;
        }

        // Now create our enlistment to tell the client the outcome.
        return new OletxEnlistment(enlistmentNotification, xactStatus, prepareInfo, this);
    }

    internal void RecoveryComplete()
    {
        Timer? localTimer = null;

        // Remember that the application has called RecoveryComplete.
        RecoveryCompleteCalledByApplication = true;

        try
        {
            // Remove the OletxEnlistment objects from the reenlist list because the RM says it doesn't
            // have any unresolved transactions, so we don't need to keep asking and the reenlist thread can exit.
            // Leave the reenlistPendingList alone.  If we have notifications outstanding, we still can't remove those.
            lock (ReenlistList)
            {
                // If the ReenlistThread is not running and there are no reenlistPendingList entries, we need to call ReenlistComplete ourself.
                lock (this)
                {
                    if (ReenlistList.Count == 0 && ReenlistPendingList.Count == 0)
                    {
                        if (ReenlistThreadTimer != null)
                        {
                            // If we have a pending reenlistThreadTimer, cancel it.  We do the cancel
                            // in the finally block to satisfy FXCop.
                            localTimer = ReenlistThreadTimer;
                            ReenlistThreadTimer = null;
                        }

                        // Try to tell the proxy ReenlistmentComplete.
                        bool success = CallProxyReenlistComplete();
                        if (!success)
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
            localTimer?.Dispose();
        }
    }

    internal void StartReenlistThread()
    {
        // We are not going to check the reenlistList.Count.  Just always start the thread.  We do this because
        // if we get a COMException from calling ReenlistComplete, we start the reenlistThreadTimer to retry it for us
        // in the background.
        lock (this)
        {
            // We don't need a MemoryBarrier here because all access to the reenlistThreadTimer member is done while
            // holding a lock on the OletxResourceManager object.
            if (ReenlistThreadTimer == null && reenlistThread == null)
            {
                ReenlistThreadTimer = new Timer(ReenlistThread, this, 10, Timeout.Infinite);
            }
        }
    }

    // This routine searches the reenlistPendingList for the specified enlistment and if it finds
    // it, removes it from the list.  An enlistment calls this routine when it is "finishing" because
    // the RM has called EnlistmentDone or it was InDoubt.  But it only calls it if the enlistment does NOT
    // have a WrappedTransactionEnlistmentAsync value, indicating that it is a recovery enlistment.
    internal void RemoveFromReenlistPending(OletxEnlistment enlistment)
    {
        // We lock the reenlistList because we have decided to lock that list when accessing either
        // the reenlistList or the reenlistPendingList.
        lock (ReenlistList)
        {
            // This will do a linear search of the list, but that is what we need to do because
            // the enlistments may change indices while notifications are outstanding.  Also,
            // this does not throw if the enlistment isn't on the list.
            ReenlistPendingList.Remove(enlistment);

            lock (this)
            {
                // If we have a ReenlistThread timer and both the reenlistList and the reenlistPendingList
                // are empty, kick the ReenlistThread now.
                if (ReenlistThreadTimer != null && ReenlistList.Count == 0 && ReenlistPendingList.Count == 0)
                {
                    if (!ReenlistThreadTimer.Change(0, Timeout.Infinite))
                    {
                        throw TransactionException.CreateInvalidOperationException(
                            TraceSourceType.TraceSourceOleTx,
                            SR.UnexpectedTimerFailure,
                            null);
                    }
                }
            }
        }
    }

    internal void ReenlistThread(object? state)
    {
        int localLoopCount;
        bool done;
        OletxEnlistment? localEnlistment;
        ResourceManagerShim? localResourceManagerShim;
        bool success;
        Timer? localTimer = null;
        bool disposeLocalTimer = false;

        OletxResourceManager resourceManager = (OletxResourceManager)state!;

        try
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, this, $"{nameof(OletxResourceManager)}.{nameof(ReenlistThread)}");
            }

            lock (resourceManager)
            {
                localResourceManagerShim = resourceManager.ResourceManagerShim;
                localTimer = resourceManager.ReenlistThreadTimer;
                resourceManager.ReenlistThreadTimer = null;
                resourceManager.reenlistThread = Thread.CurrentThread;
            }

            // We only want to do work if we have a resourceManagerShim.
            if (localResourceManagerShim != null)
            {
                lock (resourceManager.ReenlistList)
                {
                    // Get the current count on the list.
                    localLoopCount = resourceManager.ReenlistList.Count;
                }

                done = false;
                while (!done && localLoopCount > 0 && localResourceManagerShim != null)
                {
                    lock (resourceManager.ReenlistList)
                    {
                        localEnlistment = null;
                        localLoopCount--;
                        if (resourceManager.ReenlistList.Count == 0)
                        {
                            done = true;
                        }
                        else
                        {
                            localEnlistment = resourceManager.ReenlistList[0] as OletxEnlistment;
                            if (localEnlistment == null)
                            {
                                if (etwLog.IsEnabled())
                                {
                                    etwLog.InternalError();
                                }

                                throw TransactionException.Create(SR.InternalError, null);
                            }

                            resourceManager.ReenlistList.RemoveAt(0);
                            object syncRoot = localEnlistment;
                            lock (syncRoot)
                            {
                                if (OletxEnlistment.OletxEnlistmentState.Done == localEnlistment.State)
                                {
                                    // We may be racing with a RecoveryComplete here.  Just forget about this
                                    // enlistment.
                                    localEnlistment = null;
                                }

                                else if (OletxEnlistment.OletxEnlistmentState.Prepared != localEnlistment.State)
                                {
                                    // The app hasn't yet responded to Prepare, so we don't know
                                    // if it is indoubt or not yet.  So just re-add it to the end
                                    // of the list.
                                    resourceManager.ReenlistList.Add(localEnlistment);
                                    localEnlistment = null;
                                }
                            }
                        }
                    }

                    if (localEnlistment != null)
                    {
                        OletxTransactionOutcome localOutcome = OletxTransactionOutcome.NotKnownYet;
                        try
                        {
                            Debug.Assert(localResourceManagerShim != null, "ReenlistThread - localResourceManagerShim is null");

                            // Make sure we have a prepare info.
                            if (localEnlistment.ProxyPrepareInfoByteArray == null)
                            {
                                Debug.Fail("this.prepareInfoByteArray == null in RecoveryInformation()");
                                if (etwLog.IsEnabled())
                                {
                                    etwLog.InternalError();
                                }

                                throw TransactionException.Create(SR.InternalError, null);
                            }

                            localResourceManagerShim.Reenlist(localEnlistment.ProxyPrepareInfoByteArray, out localOutcome);

                            if (localOutcome == OletxTransactionOutcome.NotKnownYet)
                            {
                                object syncRoot = localEnlistment;
                                lock (syncRoot)
                                {
                                    if (OletxEnlistment.OletxEnlistmentState.Done == localEnlistment.State)
                                    {
                                        // We may be racing with a RecoveryComplete here.  Just forget about this
                                        // enlistment.
                                        localEnlistment = null;
                                    }
                                    else
                                    {
                                        // Put the enlistment back on the end of the list for retry later.
                                        lock (resourceManager.ReenlistList)
                                        {
                                            resourceManager.ReenlistList.Add(localEnlistment);
                                            localEnlistment = null;
                                        }
                                    }
                                }
                            }
                        }
                        catch (COMException ex) when (ex.ErrorCode == OletxHelper.XACT_E_CONNECTION_DOWN)
                        {
                            if (etwLog.IsEnabled())
                            {
                                etwLog.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
                            }

                            // Release the resource manager so we can create a new one.
                            resourceManager.ResourceManagerShim = null;

                            // Now create a new resource manager with the proxy.
                            localResourceManagerShim = resourceManager.ResourceManagerShim;
                        }

                        // If we get here and we still have localEnlistment, then we got the outcome.
                        if (localEnlistment != null)
                        {
                            object syncRoot = localEnlistment;
                            lock (syncRoot)
                            {
                                if (OletxEnlistment.OletxEnlistmentState.Done == localEnlistment.State)
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
                                    lock (resourceManager.ReenlistList)
                                    {
                                        resourceManager.ReenlistPendingList.Add(localEnlistment);
                                    }

                                    if (localOutcome == OletxTransactionOutcome.Committed)
                                    {
                                        localEnlistment.State = OletxEnlistment.OletxEnlistmentState.Committing;

                                        if (etwLog.IsEnabled())
                                        {
                                            etwLog.EnlistmentStatus(TraceSourceType.TraceSourceOleTx, localEnlistment.EnlistmentTraceId, NotificationCall.Commit);
                                        }

                                        localEnlistment.EnlistmentNotification!.Commit(localEnlistment);
                                    }
                                    else if (localOutcome == OletxTransactionOutcome.Aborted)
                                    {
                                        localEnlistment.State = OletxEnlistment.OletxEnlistmentState.Aborting;

                                        if (etwLog.IsEnabled())
                                        {
                                            etwLog.EnlistmentStatus(TraceSourceType.TraceSourceOleTx, localEnlistment.EnlistmentTraceId, NotificationCall.Rollback);
                                        }

                                        localEnlistment.EnlistmentNotification!.Rollback(localEnlistment);
                                    }
                                    else
                                    {
                                        if (etwLog.IsEnabled())
                                        {
                                            etwLog.InternalError();
                                        }

                                        throw TransactionException.Create(SR.InternalError, null);
                                    }
                                }
                            }
                        } // end of if null != localEnlistment
                    }  // end of if null != localEnlistment
                }
            }

            localResourceManagerShim = null;

            // Check to see if there is more work to do.
            lock (resourceManager.ReenlistList)
            {
                lock (resourceManager)
                {
                    // Get the current count on the list.
                    localLoopCount = resourceManager.ReenlistList.Count;
                    if (localLoopCount <= 0 && resourceManager.ReenlistPendingList.Count <= 0)
                    {
                        // No more entries on the list.  Try calling ReenlistComplete on the proxy, if
                        // appropriate.
                        // If the application has called RecoveryComplete,
                        // we are responsible for calling ReenlistComplete on the
                        // proxy.
                        success = resourceManager.CallProxyReenlistComplete();
                        if (success)
                        {
                            // Okay, the reenlist thread is done and we don't need to schedule another one.
                            disposeLocalTimer = true;
                        }
                        else
                        {
                            // We couldn't talk to the proxy to do ReenlistComplete, so schedule
                            // the thread again for 10 seconds from now.
                            resourceManager.ReenlistThreadTimer = localTimer;
                            if (!localTimer!.Change(10000, Timeout.Infinite))
                            {
                                throw TransactionException.CreateInvalidOperationException(
                                    TraceSourceType.TraceSourceLtm,
                                    SR.UnexpectedTimerFailure,
                                    null);
                            }
                        }
                    }
                    else
                    {
                        // There are still entries on the list, so they must not be
                        // resovled, yet.  Schedule the thread again in 10 seconds.
                        resourceManager.ReenlistThreadTimer = localTimer;
                        if (!localTimer!.Change(10000, Timeout.Infinite))
                        {
                            throw TransactionException.CreateInvalidOperationException(
                                TraceSourceType.TraceSourceLtm,
                                SR.UnexpectedTimerFailure,
                                null);
                        }
                    }

                    resourceManager.reenlistThread = null;
                }

                if (etwLog.IsEnabled())
                {
                    etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, $"{nameof(OletxResourceManager)}.{nameof(ReenlistThread)}");
                }
            }
        }  // end of outer-most try
        finally
        {
            localResourceManagerShim = null;
            if (disposeLocalTimer && localTimer != null)
            {
                localTimer.Dispose();
            }
        }
    }  // end of ReenlistThread method;
}

// This is the base class for all enlistment objects.  The enlistment objects provide the callback
// that is made from the application and pass it through to the proxy.
internal abstract class OletxBaseEnlistment
{
    protected Guid EnlistmentGuid;
    protected OletxResourceManager OletxResourceManager;
    protected OletxTransaction? oletxTransaction;
    internal OletxTransaction? OletxTransaction => oletxTransaction;

    internal Guid DistributedTxId
    {
        get
        {
            Guid returnValue = Guid.Empty;

            if (OletxTransaction != null)
            {
                returnValue = OletxTransaction.DistributedTxId;
            }
            return returnValue;
        }
    }

    protected string TransactionGuidString;
    protected int EnlistmentId;
    // this needs to be internal so it can be set from the recovery information during Reenlist.
    internal EnlistmentTraceIdentifier TraceIdentifier;

    // Owning public Enlistment object
    protected InternalEnlistment? InternalEnlistment;

    public OletxBaseEnlistment(OletxResourceManager oletxResourceManager, OletxTransaction? oletxTransaction)
    {
        Guid resourceManagerId = Guid.Empty;

        EnlistmentGuid = Guid.NewGuid();
        OletxResourceManager = oletxResourceManager;
        this.oletxTransaction = oletxTransaction;
        if (oletxTransaction != null)
        {
            EnlistmentId = oletxTransaction.RealOletxTransaction._enlistmentCount++;
            TransactionGuidString = oletxTransaction.RealOletxTransaction.TxGuid.ToString();
        }
        else
        {
            TransactionGuidString = Guid.Empty.ToString();
        }
        TraceIdentifier = EnlistmentTraceIdentifier.Empty;
    }

    protected EnlistmentTraceIdentifier InternalTraceIdentifier
    {
        get
        {
            if (EnlistmentTraceIdentifier.Empty == TraceIdentifier)
            {
                lock (this)
                {
                    if (EnlistmentTraceIdentifier.Empty == TraceIdentifier)
                    {
                        Guid rmId = Guid.Empty;
                        if (OletxResourceManager != null)
                        {
                            rmId = OletxResourceManager.ResourceManagerIdentifier;
                        }
                        EnlistmentTraceIdentifier temp;
                        if (oletxTransaction != null)
                        {
                            temp = new EnlistmentTraceIdentifier(rmId, oletxTransaction.TransactionTraceId, EnlistmentId);
                        }
                        else
                        {
                            TransactionTraceIdentifier txTraceId = new(TransactionGuidString, 0);
                            temp = new EnlistmentTraceIdentifier(rmId, txTraceId, EnlistmentId);
                        }
                        Thread.MemoryBarrier();
                        TraceIdentifier = temp;
                    }
                }
            }

            return TraceIdentifier;
        }
    }

    protected void AddToEnlistmentTable()
    {
        lock (OletxResourceManager.EnlistmentHashtable.SyncRoot)
        {
            OletxResourceManager.EnlistmentHashtable.Add(EnlistmentGuid, this);
        }
    }

    protected void RemoveFromEnlistmentTable()
    {
        lock (OletxResourceManager.EnlistmentHashtable.SyncRoot)
        {
            OletxResourceManager.EnlistmentHashtable.Remove(EnlistmentGuid);
        }
    }
}
