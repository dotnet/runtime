// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Transactions.DtcProxyShim;

namespace System.Transactions.Oletx;

internal sealed class OletxEnlistment : OletxBaseEnlistment, IPromotedEnlistment
{
    internal enum OletxEnlistmentState
    {
        Active,
        Phase0Preparing,
        Preparing,
        SinglePhaseCommitting,
        Prepared,
        Committing,
        Committed,
        Aborting,
        Aborted,
        InDoubt,
        Done
    }

    private Phase0EnlistmentShim? _phase0Shim;
    private readonly bool _canDoSinglePhase;
    private IEnlistmentNotificationInternal? _iEnlistmentNotification;
    // The information that comes from/goes to the proxy.
    private byte[]? _proxyPrepareInfoByteArray;

    private bool _isSinglePhase;
    private readonly Guid _transactionGuid = Guid.Empty;

    // Set to true if we receive an AbortRequest while we still have
    // another notification, like prepare, outstanding.  It indicates that
    // we need to fabricate a rollback to the app after it responds to Prepare.
    private bool _fabricateRollback;

    private bool _tmWentDown;
    private bool _aborting;

    private byte[]? _prepareInfoByteArray;

    internal Guid TransactionIdentifier => _transactionGuid;

    #region Constructor

    internal OletxEnlistment(
        bool canDoSinglePhase,
        IEnlistmentNotificationInternal enlistmentNotification,
        Guid transactionGuid,
        EnlistmentOptions enlistmentOptions,
        OletxResourceManager oletxResourceManager,
        OletxTransaction oletxTransaction)
        : base(oletxResourceManager, oletxTransaction)
    {
        // This will get set later by the creator of this object after it
        // has enlisted with the proxy.
        EnlistmentShim = null;
        _phase0Shim = null;

        _canDoSinglePhase = canDoSinglePhase;
        _iEnlistmentNotification = enlistmentNotification;
        State = OletxEnlistmentState.Active;
        _transactionGuid = transactionGuid;

        _proxyPrepareInfoByteArray = null;

        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        if (etwLog.IsEnabled())
        {
            etwLog.EnlistmentCreated(TraceSourceType.TraceSourceOleTx, InternalTraceIdentifier, EnlistmentType.Durable, enlistmentOptions);
        }

        // Always do this last in case anything earlier fails.
        AddToEnlistmentTable();
    }

    internal OletxEnlistment(
        IEnlistmentNotificationInternal enlistmentNotification,
        OletxTransactionStatus xactStatus,
        byte[] prepareInfoByteArray,
        OletxResourceManager oletxResourceManager)
        : base(oletxResourceManager, null)
    {
        // This will get set later by the creator of this object after it
        // has enlisted with the proxy.
        EnlistmentShim = null;
        _phase0Shim = null;

        _canDoSinglePhase = false;
        _iEnlistmentNotification = enlistmentNotification;
        State = OletxEnlistmentState.Active;

        // Do this before we do any tracing because it will affect the trace identifiers that we generate.
        Debug.Assert(prepareInfoByteArray != null,
            "OletxEnlistment.ctor - null oletxTransaction without a prepareInfoByteArray");

        int prepareInfoLength = prepareInfoByteArray.Length;
        _proxyPrepareInfoByteArray = new byte[prepareInfoLength];
        Array.Copy(prepareInfoByteArray, _proxyPrepareInfoByteArray, prepareInfoLength);

        _transactionGuid = new Guid(_proxyPrepareInfoByteArray.AsSpan(0, 16));
        TransactionGuidString = _transactionGuid.ToString();

        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;

        // If this is being created as part of a Reenlist and we already know the
        // outcome, then tell the application.
        switch (xactStatus)
        {
            case OletxTransactionStatus.OLETX_TRANSACTION_STATUS_ABORTED:
                {
                    State = OletxEnlistmentState.Aborting;
                    if (etwLog.IsEnabled())
                    {
                        etwLog.EnlistmentStatus(TraceSourceType.TraceSourceOleTx, InternalTraceIdentifier, NotificationCall.Rollback);
                    }

                    _iEnlistmentNotification.Rollback(this);
                    break;
                }

            case OletxTransactionStatus.OLETX_TRANSACTION_STATUS_COMMITTED:
                {
                    State = OletxEnlistmentState.Committing;
                    // We are going to send the notification to the RM.  We need to put the
                    // enlistment on the reenlistPendingList.  We lock the reenlistList because
                    // we have decided that is the lock that protects both lists.  The entry will
                    // be taken off the reenlistPendingList when the enlistment has
                    // EnlistmentDone called on it.  The enlistment will call
                    // RemoveFromReenlistPending.
                    lock (oletxResourceManager.ReenlistList)
                    {
                        oletxResourceManager.ReenlistPendingList.Add(this);
                    }

                    if (etwLog.IsEnabled())
                    {
                        etwLog.EnlistmentStatus(TraceSourceType.TraceSourceOleTx, InternalTraceIdentifier, NotificationCall.Commit);
                    }

                    _iEnlistmentNotification.Commit(this);
                    break;
                }

            case OletxTransactionStatus.OLETX_TRANSACTION_STATUS_PREPARED:
                {
                    State = OletxEnlistmentState.Prepared;
                    lock (oletxResourceManager.ReenlistList)
                    {
                        oletxResourceManager.ReenlistList.Add(this);
                        oletxResourceManager.StartReenlistThread();
                    }
                    break;
                }

            default:
                {
                    if (etwLog.IsEnabled())
                    {
                        etwLog.InternalError(SR.OletxEnlistmentUnexpectedTransactionStatus);
                    }

                    throw TransactionException.Create(
                        SR.OletxEnlistmentUnexpectedTransactionStatus, null, DistributedTxId);
                }
        }

        if (etwLog.IsEnabled())
        {
            etwLog.EnlistmentCreated(TraceSourceType.TraceSourceOleTx, InternalTraceIdentifier, EnlistmentType.Durable, EnlistmentOptions.None);
        }

        // Always do this last in case anything prior to this fails.
        AddToEnlistmentTable();
    }
    #endregion

    internal IEnlistmentNotificationInternal? EnlistmentNotification => _iEnlistmentNotification;

    internal EnlistmentShim? EnlistmentShim { get; set; }

    internal Phase0EnlistmentShim? Phase0EnlistmentShim
    {
        get => _phase0Shim;
        set
        {
            lock (this)
            {
                // If this.aborting is set to true, then we must have already received a
                // Phase0Request.  This could happen if the transaction aborts after the
                // enlistment is made, but before we are given the shim.
                if (value != null && (_aborting || _tmWentDown))
                {
                    value.Phase0Done(false);
                }
                _phase0Shim = value;
            }
        }
    }

    internal OletxEnlistmentState State { get; set; } = OletxEnlistmentState.Active;

    internal byte[]? ProxyPrepareInfoByteArray => _proxyPrepareInfoByteArray;

    internal void FinishEnlistment()
    {
        lock (this)
        {
            // If we don't have a wrappedTransactionEnlistmentAsync, we may
            // need to remove ourselves from the reenlistPendingList in the
            // resource manager.
            if (EnlistmentShim == null)
            {
                OletxResourceManager.RemoveFromReenlistPending(this);
            }
            _iEnlistmentNotification = null;

            RemoveFromEnlistmentTable();
        }
    }

    internal void TMDownFromInternalRM(OletxTransactionManager oletxTm)
    {
        lock (this)
        {
            // If we don't have an oletxTransaction or the passed oletxTm matches that of my oletxTransaction, the TM went down.
            if (oletxTransaction == null || oletxTm == oletxTransaction.RealOletxTransaction.OletxTransactionManagerInstance)
            {
                _tmWentDown = true;
            }
        }
    }

    #region ITransactionResourceAsync methods

    // ITranactionResourceAsync.PrepareRequest
    public bool PrepareRequest(bool singlePhase, byte[] prepareInfo)
    {
        EnlistmentShim? localEnlistmentShim;
        OletxEnlistmentState localState = OletxEnlistmentState.Active;
        IEnlistmentNotificationInternal localEnlistmentNotification;
        bool enlistmentDone;

        lock (this)
        {
            if (OletxEnlistmentState.Active == State)
            {
                localState = State = OletxEnlistmentState.Preparing;
            }
            else
            {
                // We must have done the prepare work in Phase0, so just remember what state we are
                // in now.
                localState = State;
            }

            localEnlistmentNotification = _iEnlistmentNotification!;

            localEnlistmentShim = EnlistmentShim;

            oletxTransaction!.RealOletxTransaction.TooLateForEnlistments = true;
        }

        // If we went to Preparing state, send the app
        // a prepare request.
        if (OletxEnlistmentState.Preparing == localState)
        {
            _isSinglePhase = singlePhase;

            // Store the prepare info we are given.
            Debug.Assert(_proxyPrepareInfoByteArray == null, "Unexpected value in this.proxyPrepareInfoByteArray");
            long arrayLength = prepareInfo.Length;
            _proxyPrepareInfoByteArray = new byte[arrayLength];
            Array.Copy(prepareInfo, _proxyPrepareInfoByteArray, arrayLength);

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;

            if (_isSinglePhase && _canDoSinglePhase)
            {
                ISinglePhaseNotificationInternal singlePhaseNotification = (ISinglePhaseNotificationInternal)localEnlistmentNotification;
                State = OletxEnlistmentState.SinglePhaseCommitting;
                // We don't call DecrementUndecidedEnlistments for Phase1 enlistments.
                if (etwLog.IsEnabled())
                {
                    etwLog.EnlistmentStatus(TraceSourceType.TraceSourceOleTx, InternalTraceIdentifier, NotificationCall.SinglePhaseCommit);
                }

                singlePhaseNotification.SinglePhaseCommit(this);
                enlistmentDone = true;
            }
            else
            {
                State = OletxEnlistmentState.Preparing;

                _prepareInfoByteArray = TransactionManager.GetRecoveryInformation(
                    OletxResourceManager.OletxTransactionManager.CreationNodeName,
                    prepareInfo);

                if (etwLog.IsEnabled())
                {
                    etwLog.EnlistmentStatus(TraceSourceType.TraceSourceOleTx, InternalTraceIdentifier, NotificationCall.Prepare);
                }

                localEnlistmentNotification.Prepare(this);
                enlistmentDone = false;
            }
        }
        else if (OletxEnlistmentState.Prepared == localState)
        {
            // We must have done our prepare work during Phase0 so just vote Yes.
            try
            {
                localEnlistmentShim!.PrepareRequestDone(OletxPrepareVoteType.Prepared);
                enlistmentDone = false;
            }
            catch (COMException comException)
            {
                OletxTransactionManager.ProxyException(comException);
                throw;
            }
        }
        else if (OletxEnlistmentState.Done == localState)
        {
            try
            {
                // This was an early vote.  Respond ReadOnly
                try
                {
                    localEnlistmentShim!.PrepareRequestDone(OletxPrepareVoteType.ReadOnly);
                    enlistmentDone = true;
                }
                finally
                {
                    FinishEnlistment();
                }
            }
            catch (COMException comException)
            {
                OletxTransactionManager.ProxyException(comException);
                throw;
            }
        }
        else
        {
            // Any other state means we should vote NO to the proxy.
            try
            {
                localEnlistmentShim!.PrepareRequestDone(OletxPrepareVoteType.Failed);
            }
            catch (COMException ex)
            {
                // No point in rethrowing this.  We are not on an app thread and we have already told
                // the app that the transaction is aborting.  When the app calls EnlistmentDone, we will
                // do the final release of the ITransactionEnlistmentAsync.
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
                }
            }

            enlistmentDone = true;
        }

        return enlistmentDone;
    }


    public void CommitRequest()
    {
        OletxEnlistmentState localState = OletxEnlistmentState.Active;
        IEnlistmentNotificationInternal? localEnlistmentNotification = null;
        EnlistmentShim? localEnlistmentShim = null;
        bool finishEnlistment = false;

        lock (this)
        {
            if (OletxEnlistmentState.Prepared == State)
            {
                localState = State = OletxEnlistmentState.Committing;
                localEnlistmentNotification = _iEnlistmentNotification;
            }
            else
            {
                // We must have received an EnlistmentDone already.
                localState = State;
                localEnlistmentShim = EnlistmentShim;
                finishEnlistment = true;
            }
        }

        if (localEnlistmentNotification != null)
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.EnlistmentStatus(TraceSourceType.TraceSourceOleTx, InternalTraceIdentifier, NotificationCall.Commit);
            }

            localEnlistmentNotification.Commit(this);
        }
        else if (localEnlistmentShim != null)
        {
            // We need to respond to the proxy now.
            try
            {
                localEnlistmentShim.CommitRequestDone();
            }
            catch (COMException ex)
            {
                // If the TM went down during our call, there is nothing special we have to do because
                // the App doesn't expect any more notifications. We do want to mark the enlistment
                // to finish, however.
                if (ex.ErrorCode == OletxHelper.XACT_E_CONNECTION_DOWN ||
                    ex.ErrorCode == OletxHelper.XACT_E_TMNOTAVAILABLE)
                {
                    finishEnlistment = true;
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
            finally
            {
                if (finishEnlistment)
                {
                    FinishEnlistment();
                }
            }
        }
    }

    public void AbortRequest()
    {
        OletxEnlistmentState localState = OletxEnlistmentState.Active;
        IEnlistmentNotificationInternal? localEnlistmentNotification = null;
        EnlistmentShim? localEnlistmentShim = null;
        bool finishEnlistment = false;

        lock (this)
        {
            if (State is OletxEnlistmentState.Active or OletxEnlistmentState.Prepared)
            {
                localState = State = OletxEnlistmentState.Aborting;
                localEnlistmentNotification = _iEnlistmentNotification;
            }
            else
            {
                // We must have received an EnlistmentDone already or we have
                // a notification outstanding (Phase0 prepare).
                localState = State;
                if (OletxEnlistmentState.Phase0Preparing == State)
                {
                    _fabricateRollback = true;
                }
                else
                {
                    finishEnlistment = true;
                }

                localEnlistmentShim = EnlistmentShim;
            }
        }

        if (localEnlistmentNotification != null)
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.EnlistmentStatus(TraceSourceType.TraceSourceOleTx, InternalTraceIdentifier, NotificationCall.Rollback);
            }

            localEnlistmentNotification.Rollback(this);
        }
        else if (localEnlistmentShim != null)
        {
            // We need to respond to the proxy now.
            try
            {
                localEnlistmentShim.AbortRequestDone();
            }
            catch (COMException ex)
            {
                // If the TM went down during our call, there is nothing special we have to do because
                // the App doesn't expect any more notifications.  We do want to mark the enlistment
                // to finish, however.
                if (ex.ErrorCode == OletxHelper.XACT_E_CONNECTION_DOWN ||
                    ex.ErrorCode == OletxHelper.XACT_E_TMNOTAVAILABLE)
                {
                    finishEnlistment = true;
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
            finally
            {
                if (finishEnlistment)
                {
                    FinishEnlistment();
                }
            }
        }
    }

    public void TMDown()
    {
        // We aren't telling our enlistments about TMDown, only
        // resource managers.
        // Put this enlistment on the Reenlist list.  The Reenlist thread will get
        // started when the RMSink gets the TMDown notification.
        lock (OletxResourceManager.ReenlistList)
        {
            lock (this)
            {
                // Remember that we got the TMDown in case we get a Phase0Request after so we
                // can avoid doing a Prepare to the app.
                _tmWentDown = true;

                // Only move Prepared and Committing enlistments to the ReenlistList.  All others
                // do not require a Reenlist to figure out what to do.  We save off Committing
                // enlistments because the RM has not acknowledged the commit, so we can't
                // call RecoveryComplete on the proxy until that has happened.  The Reenlist thread
                // will loop until the reenlist list is empty and it will leave a Committing
                // enlistment on the list until it is done, but will NOT call Reenlist on the proxy.
                if (State is OletxEnlistmentState.Prepared or OletxEnlistmentState.Committing)
                {
                    OletxResourceManager.ReenlistList.Add(this);
                }
            }
        }
    }

    #endregion

    #region ITransactionPhase0NotifyAsync methods

    // ITransactionPhase0NotifyAsync
    public void Phase0Request(bool abortingHint)
    {
        IEnlistmentNotificationInternal? localEnlistmentNotification = null;
        OletxEnlistmentState localState = OletxEnlistmentState.Active;
        OletxCommittableTransaction? committableTx;
        bool commitNotYetCalled = false;

        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        if (etwLog.IsEnabled())
        {
            etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, this, $"{nameof(OletxEnlistment)}.{nameof(Phase0Request)}");
        }

        committableTx = oletxTransaction!.RealOletxTransaction.CommittableTransaction;
        if (committableTx != null)
        {
            // We are dealing with the committable transaction.  If Commit or BeginCommit has NOT been
            // called, then we are dealing with a situation where the TM went down and we are getting
            // a bogus Phase0Request with abortHint = false (COMPlus bug 36760/36758).  This is an attempt
            // to not send the app a Prepare request when we know the transaction is going to abort.
            if (!committableTx.CommitCalled)
            {
                commitNotYetCalled = true;
            }
        }

        lock (this)
        {
            _aborting = abortingHint;

            // The app may have already called EnlistmentDone.  If this occurs, don't bother sending
            // the notification to the app and we don't need to tell the proxy.
            if (OletxEnlistmentState.Active == State)
            {
                // If we got an abort hint or we are the committable transaction and Commit has not yet been called or the TM went down,
                // we don't want to do any more work on the transaction.  The abort notifications will be sent by the phase 1
                // enlistment
                if (_aborting || commitNotYetCalled || _tmWentDown)
                {
                    // There is a possible race where we could get the Phase0Request before we are given the
                    // shim.  In that case, we will vote "no" when we are given the shim.
                    if (_phase0Shim != null)
                    {
                        try
                        {
                            _phase0Shim.Phase0Done(false);
                        }
                        // I am not going to check for XACT_E_PROTOCOL here because that check is a workaround for a bug
                        // that only shows up if abortingHint is false.
                        catch (COMException ex)
                        {
                            if (etwLog.IsEnabled())
                            {
                                etwLog.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
                            }
                        }
                    }
                }
                else
                {
                    localState = State = OletxEnlistmentState.Phase0Preparing;
                    localEnlistmentNotification = _iEnlistmentNotification;
                }
            }
        }

        // Tell the application to do the work.
        if (localEnlistmentNotification != null)
        {
            if (OletxEnlistmentState.Phase0Preparing == localState)
            {
                byte[] txGuidArray = _transactionGuid.ToByteArray();
                byte[] rmGuidArray = OletxResourceManager.ResourceManagerIdentifier.ToByteArray();

                byte[] temp = new byte[txGuidArray.Length + rmGuidArray.Length];
                Thread.MemoryBarrier();
                _proxyPrepareInfoByteArray = temp;
                for (int index = 0; index < txGuidArray.Length; index++)
                {
                    _proxyPrepareInfoByteArray[index] =
                        txGuidArray[index];
                }

                for (int index = 0; index < rmGuidArray.Length; index++)
                {
                    _proxyPrepareInfoByteArray[txGuidArray.Length + index] =
                        rmGuidArray[index];
                }

                _prepareInfoByteArray = TransactionManager.GetRecoveryInformation(
                    OletxResourceManager.OletxTransactionManager.CreationNodeName,
                    _proxyPrepareInfoByteArray);

                if (etwLog.IsEnabled())
                {
                    etwLog.EnlistmentStatus(TraceSourceType.TraceSourceOleTx, InternalTraceIdentifier, NotificationCall.Prepare);
                }

                localEnlistmentNotification.Prepare(this);
            }
            else
            {
                // We must have had a race between EnlistmentDone and the proxy telling
                // us Phase0Request.  Just return.
                if (etwLog.IsEnabled())
                {
                    etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, $"{nameof(OletxEnlistment)}.{nameof(Phase0Request)}");
                }

                return;
            }

        }

        if (etwLog.IsEnabled())
        {
            etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, $"{nameof(OletxEnlistment)}.{nameof(Phase0Request)}");
        }
    }

    #endregion

    public void EnlistmentDone()
    {
        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        if (etwLog.IsEnabled())
        {
            etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, this, $"{nameof(OletxEnlistment)}.{nameof(EnlistmentDone)}");
            etwLog.EnlistmentCallbackPositive(InternalTraceIdentifier, EnlistmentCallback.Done);
        }

        EnlistmentShim? localEnlistmentShim = null;
        Phase0EnlistmentShim? localPhase0Shim = null;
        OletxEnlistmentState localState = OletxEnlistmentState.Active;
        bool finishEnlistment;
        bool localFabricateRollback;

        lock (this)
        {
            localState = State;
            if (OletxEnlistmentState.Active == State)
            {
                // Early vote.  If we are doing Phase0, we need to unenlist.  Otherwise, just
                // remember.
                localPhase0Shim = Phase0EnlistmentShim;
                if (localPhase0Shim != null)
                {
                    // We are a Phase0 enlistment and we have a vote - decrement the undecided enlistment count.
                    // We only do this for Phase0 because we don't count Phase1 durable enlistments.
                    oletxTransaction!.RealOletxTransaction.DecrementUndecidedEnlistments();
                }
                finishEnlistment = false;
            }
            else if (OletxEnlistmentState.Preparing == State)
            {
                // Read only vote.  Tell the proxy and go to the Done state.
                localEnlistmentShim = EnlistmentShim;
                // We don't decrement the undecided enlistment count for Preparing because we only count
                // Phase0 enlistments and we are in Phase1 in Preparing state.
                finishEnlistment = true;
            }
            else if (OletxEnlistmentState.Phase0Preparing == State)
            {
                // Read only vote to Phase0.  Tell the proxy okay and go to the Done state.
                localPhase0Shim = Phase0EnlistmentShim;
                // We are a Phase0 enlistment and we have a vote - decrement the undecided enlistment count.
                // We only do this for Phase0 because we don't count Phase1 durable enlistments.
                oletxTransaction!.RealOletxTransaction.DecrementUndecidedEnlistments();

                // If we would have fabricated a rollback then we have already received an abort request
                // from proxy and will not receive any more notifications.  Otherwise more notifications
                // will be coming.
                if (_fabricateRollback)
                {
                    finishEnlistment = true;
                }
                else
                {
                    finishEnlistment = false;
                }
            }
            else if (State is OletxEnlistmentState.Committing
                     or OletxEnlistmentState.Aborting
                     or OletxEnlistmentState.SinglePhaseCommitting)
            {
                localEnlistmentShim = EnlistmentShim;
                finishEnlistment = true;
                // We don't decrement the undecided enlistment count for SinglePhaseCommitting because we only
                // do it for Phase0 enlistments.
            }
            else
            {
                throw TransactionException.CreateEnlistmentStateException(null, DistributedTxId);
            }

            // If this.fabricateRollback is true, it means that we are fabricating this
            // AbortRequest, rather than having the proxy tell us.  So we don't need
            // to respond to the proxy with AbortRequestDone.
            localFabricateRollback = _fabricateRollback;

            State = OletxEnlistmentState.Done;
        }

        try
        {
            if (localEnlistmentShim != null)
            {
                if (OletxEnlistmentState.Preparing == localState)
                {
                    localEnlistmentShim.PrepareRequestDone(OletxPrepareVoteType.ReadOnly);
                }
                else if (OletxEnlistmentState.Committing == localState)
                {
                    localEnlistmentShim.CommitRequestDone();
                }
                else if (OletxEnlistmentState.Aborting == localState)
                {
                    // If localFabricatRollback is true, it means that we are fabricating this
                    // AbortRequest, rather than having the proxy tell us.  So we don't need
                    // to respond to the proxy with AbortRequestDone.
                    if (!localFabricateRollback)
                    {
                        localEnlistmentShim.AbortRequestDone();
                    }
                }
                else if (OletxEnlistmentState.SinglePhaseCommitting == localState)
                {
                    localEnlistmentShim.PrepareRequestDone(OletxPrepareVoteType.SinglePhase);
                }
                else
                {
                    throw TransactionException.CreateEnlistmentStateException(null, DistributedTxId);
                }
            }
            else if (localPhase0Shim != null)
            {
                if (localState == OletxEnlistmentState.Active)
                {
                    localPhase0Shim.Unenlist();
                }
                else if (localState == OletxEnlistmentState.Phase0Preparing)
                {
                    localPhase0Shim.Phase0Done(true);
                }
                else
                {
                    throw TransactionException.CreateEnlistmentStateException(null, DistributedTxId);
                }
            }
        }
        catch (COMException ex)
        {
            // If we get an error talking to the proxy, there is nothing special we have to do because
            // the App doesn't expect any more notifications.  We do want to mark the enlistment
            // to finish, however.
            finishEnlistment = true;

            if (etwLog.IsEnabled())
            {
                etwLog.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
            }
        }
        finally
        {
            if (finishEnlistment)
            {
                FinishEnlistment();
            }
        }

        if (etwLog.IsEnabled())
        {
            etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, $"{nameof(OletxEnlistment)}.{nameof(EnlistmentDone)}");
        }
    }

    public EnlistmentTraceIdentifier EnlistmentTraceId
    {
        get
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, this, $"{nameof(OletxEnlistment)}.{nameof(EnlistmentTraceId)}");
                etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, $"{nameof(OletxEnlistment)}.{nameof(EnlistmentTraceId)}");
            }

            return InternalTraceIdentifier;
        }
    }

    public void Prepared()
    {
        int hrResult = OletxHelper.S_OK;
        EnlistmentShim? localEnlistmentShim = null;
        Phase0EnlistmentShim? localPhase0Shim = null;
        bool localFabricateRollback = false;

        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        if (etwLog.IsEnabled())
        {
            etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, this, $"OletxPreparingEnlistment.{nameof(Prepared)}");
            etwLog.EnlistmentCallbackPositive(InternalTraceIdentifier, EnlistmentCallback.Prepared);
        }

        lock (this)
        {
            if (State == OletxEnlistmentState.Preparing)
            {
                localEnlistmentShim = EnlistmentShim;
            }
            else if (OletxEnlistmentState.Phase0Preparing == State)
            {
                // If the transaction is doomed or we have fabricateRollback is true because the
                // transaction aborted while the Phase0 Prepare request was outstanding,
                // release the WrappedTransactionPhase0EnlistmentAsync and remember that
                // we have a pending rollback.
                localPhase0Shim = Phase0EnlistmentShim;
                if (oletxTransaction!.RealOletxTransaction.Doomed || _fabricateRollback)
                {
                    // Set fabricateRollback in case we got here because the transaction is doomed.
                    _fabricateRollback = true;
                    localFabricateRollback = _fabricateRollback;
                }
            }
            else
            {
                throw TransactionException.CreateEnlistmentStateException(null, DistributedTxId);
            }

            State = OletxEnlistmentState.Prepared;
        }

        try
        {
            if (localEnlistmentShim != null)
            {
                localEnlistmentShim.PrepareRequestDone(OletxPrepareVoteType.Prepared);
            }
            else if (localPhase0Shim != null)
            {
                // We have a vote - decrement the undecided enlistment count.  We do
                // this after checking Doomed because ForceRollback will decrement also.
                // We also do this only for Phase0 enlistments.
                oletxTransaction!.RealOletxTransaction.DecrementUndecidedEnlistments();

                localPhase0Shim.Phase0Done(!localFabricateRollback);
            }
            else
            {
                // The TM must have gone down, thus causing our interface pointer to be
                // invalidated.  So we need to drive abort of the enlistment as if we
                // received an AbortRequest.
                localFabricateRollback = true;
            }

            if (localFabricateRollback)
            {
                AbortRequest();
            }
        }
        catch (COMException ex)
        {
            // If the TM went down during our call, the TMDown notification to the enlistment
            // and RM will put this enlistment on the ReenlistList, if appropriate.  The outcome
            // will be obtained by the ReenlistThread.
            if ((ex.ErrorCode == OletxHelper.XACT_E_CONNECTION_DOWN || ex.ErrorCode == OletxHelper.XACT_E_TMNOTAVAILABLE) && etwLog.IsEnabled())
            {
                etwLog.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
            }
            // In the case of Phase0, there is a bug in the proxy that causes an XACT_E_PROTOCOL
            // error if the TM goes down while the enlistment is still active.  The Phase0Request is
            // sent out with abortHint false, but the state of the proxy object is not changed, causing
            // Phase0Done request to fail with XACT_E_PROTOCOL.
            // For Prepared, we want to make sure the proxy aborts the transaction.  We don't need
            // to drive the abort to the application here because the Phase1 enlistment will do that.
            // In other words, treat this as if the proxy said Phase0Request( abortingHint = true ).
            else if (ex.ErrorCode == OletxHelper.XACT_E_PROTOCOL)
            {
                Phase0EnlistmentShim = null;

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

        if (etwLog.IsEnabled())
        {
            etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, $"OletxPreparingEnlistment.{nameof(Prepared)}");
        }
    }

    public void ForceRollback()
        => ForceRollback(null);

    public void ForceRollback(Exception? e)
    {
        EnlistmentShim? localEnlistmentShim = null;
        Phase0EnlistmentShim? localPhase0Shim = null;

        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        if (etwLog.IsEnabled())
        {
            etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, this, $"OletxPreparingEnlistment.{nameof(ForceRollback)}");
            etwLog.EnlistmentCallbackNegative(InternalTraceIdentifier, EnlistmentCallback.ForceRollback);
        }

        lock (this)
        {
            if (OletxEnlistmentState.Preparing == State)
            {
                localEnlistmentShim = EnlistmentShim;
            }
            else if (OletxEnlistmentState.Phase0Preparing == State)
            {
                localPhase0Shim = Phase0EnlistmentShim;
                if (localPhase0Shim != null)
                {
                    // We have a vote - decrement the undecided enlistment count.  We only do this
                    // if we are Phase0 enlistment.
                    oletxTransaction!.RealOletxTransaction.DecrementUndecidedEnlistments();
                }
            }
            else
            {
                throw TransactionException.CreateEnlistmentStateException(null, DistributedTxId);
            }

            State = OletxEnlistmentState.Aborted;
        }

        Interlocked.CompareExchange(ref oletxTransaction!.RealOletxTransaction.InnerException, e, null);

        try
        {
            localEnlistmentShim?.PrepareRequestDone(OletxPrepareVoteType.Failed);
        }
        catch (COMException ex)
        {
            // If the TM went down during our call, there is nothing special we have to do because
            // the App doesn't expect any more notifications.
            if (ex.ErrorCode == OletxHelper.XACT_E_CONNECTION_DOWN ||
                ex.ErrorCode == OletxHelper.XACT_E_TMNOTAVAILABLE)
            {
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
        finally
        {
            FinishEnlistment();
        }

        if (etwLog.IsEnabled())
        {
            etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, $"OletxPreparingEnlistment.{nameof(ForceRollback)}");
        }
    }

    public void Committed()
    {
        EnlistmentShim? localEnlistmentShim = null;

        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        if (etwLog.IsEnabled())
        {
            etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, this, $"OletxSinglePhaseEnlistment.{nameof(Committed)}");
            etwLog.EnlistmentCallbackPositive(InternalTraceIdentifier, EnlistmentCallback.Committed);
        }

        lock (this)
        {
            if (!_isSinglePhase || OletxEnlistmentState.SinglePhaseCommitting != State)
            {
                throw TransactionException.CreateEnlistmentStateException(null, DistributedTxId);
            }
            State = OletxEnlistmentState.Committed;
            localEnlistmentShim = EnlistmentShim;
        }

        try
        {
            // This may be the result of a reenlist, which means we don't have a
            // reference to the proxy.
            localEnlistmentShim?.PrepareRequestDone(OletxPrepareVoteType.SinglePhase);
        }
        catch (COMException ex)
        {
            // If the TM went down during our call, there is nothing special we have to do because
            // the App doesn't expect any more notifications.
            if (ex.ErrorCode == OletxHelper.XACT_E_CONNECTION_DOWN ||
                ex.ErrorCode == OletxHelper.XACT_E_TMNOTAVAILABLE)
            {
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
        finally
        {
            FinishEnlistment();
        }

        if (etwLog.IsEnabled())
        {
            etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, $"OletxSinglePhaseEnlistment.{nameof(Committed)}");
        }
    }

    public void Aborted()
        => Aborted(null);

    public void Aborted(Exception? e)
    {
        EnlistmentShim? localEnlistmentShim = null;

        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        if (etwLog.IsEnabled())
        {
            etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, this, $"OletxSinglePhaseEnlistment.{nameof(Aborted)}");
            etwLog.EnlistmentCallbackNegative(InternalTraceIdentifier, EnlistmentCallback.Aborted);
        }

        lock (this)
        {
            if (!_isSinglePhase || OletxEnlistmentState.SinglePhaseCommitting != State)
            {
                throw TransactionException.CreateEnlistmentStateException(null, DistributedTxId);
            }
            State = OletxEnlistmentState.Aborted;

            localEnlistmentShim = EnlistmentShim;
        }

        Interlocked.CompareExchange(ref oletxTransaction!.RealOletxTransaction.InnerException, e, null);

        try
        {
            localEnlistmentShim?.PrepareRequestDone(OletxPrepareVoteType.Failed);
        }
        // If the TM went down during our call, there is nothing special we have to do because
        // the App doesn't expect any more notifications.
        catch (COMException ex) when (
            (ex.ErrorCode == OletxHelper.XACT_E_CONNECTION_DOWN || ex.ErrorCode == OletxHelper.XACT_E_TMNOTAVAILABLE) && etwLog.IsEnabled())
        {
            etwLog.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
        }
        finally
        {
            FinishEnlistment();
        }

        if (etwLog.IsEnabled())
        {
            etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, $"OletxSinglePhaseEnlistment.{nameof(Aborted)}");
        }
    }

    public void InDoubt()
        => InDoubt(null);

    public void InDoubt(Exception? e)
    {
        EnlistmentShim? localEnlistmentShim = null;
        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        if (etwLog.IsEnabled())
        {
            etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, this, $"OletxSinglePhaseEnlistment.{nameof(InDoubt)}");
            etwLog.EnlistmentCallbackNegative(InternalTraceIdentifier, EnlistmentCallback.InDoubt);
        }

        lock (this)
        {
            if (!_isSinglePhase || OletxEnlistmentState.SinglePhaseCommitting != State)
            {
                throw TransactionException.CreateEnlistmentStateException(null, DistributedTxId);
            }
            State = OletxEnlistmentState.InDoubt;
            localEnlistmentShim = EnlistmentShim;
        }

        lock (oletxTransaction!.RealOletxTransaction)
        {
            oletxTransaction.RealOletxTransaction.InnerException ??= e;
        }

        try
        {
            localEnlistmentShim?.PrepareRequestDone(OletxPrepareVoteType.InDoubt);
        }
        // If the TM went down during our call, there is nothing special we have to do because
        // the App doesn't expect any more notifications.
        catch (COMException ex) when (
            (ex.ErrorCode == OletxHelper.XACT_E_CONNECTION_DOWN || ex.ErrorCode == OletxHelper.XACT_E_TMNOTAVAILABLE) && etwLog.IsEnabled())
        {
            etwLog.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
        }
        finally
        {
            FinishEnlistment();
        }

        if (etwLog.IsEnabled())
        {
            etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, $"OletxSinglePhaseEnlistment.{nameof(InDoubt)}");
        }
    }

    public byte[] GetRecoveryInformation()
    {
        if (_prepareInfoByteArray == null)
        {
            Debug.Fail("this.prepareInfoByteArray == null in RecoveryInformation()");
            throw TransactionException.CreateEnlistmentStateException(null, DistributedTxId);
        }

        return _prepareInfoByteArray;
    }

    InternalEnlistment? IPromotedEnlistment.InternalEnlistment
    {
        get => base.InternalEnlistment;
        set => base.InternalEnlistment = value;
    }
}
