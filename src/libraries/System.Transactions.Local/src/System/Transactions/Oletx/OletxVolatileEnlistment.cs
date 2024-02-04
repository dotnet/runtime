// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Transactions.DtcProxyShim;

namespace System.Transactions.Oletx;

internal abstract class OletxVolatileEnlistmentContainer
{
    protected OletxVolatileEnlistmentContainer(RealOletxTransaction realOletxTransaction)
    {
        Debug.Assert(realOletxTransaction != null, "Argument is null");

        RealOletxTransaction = realOletxTransaction;
    }

    protected RealOletxTransaction RealOletxTransaction;
    protected ArrayList EnlistmentList = new();
    protected int Phase;
    protected int OutstandingNotifications;
    protected bool CollectedVoteYes;
    protected int IncompleteDependentClones;
    protected bool AlreadyVoted;

    internal abstract void DecrementOutstandingNotifications(bool voteYes);

    internal abstract void AddDependentClone();

    internal abstract void DependentCloneCompleted();

    internal abstract void RollbackFromTransaction();

    internal abstract void OutcomeFromTransaction(TransactionStatus outcome);

    internal abstract void Committed();

    internal abstract void Aborted();

    internal abstract void InDoubt();

    internal Guid TransactionIdentifier
        => RealOletxTransaction.Identifier;
}

internal sealed class OletxPhase0VolatileEnlistmentContainer : OletxVolatileEnlistmentContainer
{
    private Phase0EnlistmentShim? _phase0EnlistmentShim;
    private bool _aborting;
    private bool _tmWentDown;

    internal OletxPhase0VolatileEnlistmentContainer(RealOletxTransaction realOletxTransaction)
        : base(realOletxTransaction)
    {
        // This will be set later, after the caller creates the enlistment with the proxy.
        _phase0EnlistmentShim = null;

        Phase = -1;
        _aborting = false;
        _tmWentDown = false;
        OutstandingNotifications = 0;
        IncompleteDependentClones = 0;
        AlreadyVoted = false;
        // If anybody votes false, this will get set to false.
        CollectedVoteYes = true;

        // This is a new undecided enlistment on the transaction.  Do this last since it has side affects.
        realOletxTransaction.IncrementUndecidedEnlistments();
    }

    internal void TMDown()
    {
        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        if (etwLog.IsEnabled())
        {
            etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, this, $"{nameof(OletxPhase0VolatileEnlistmentContainer)}.{nameof(TMDown)}");
        }

        _tmWentDown = true;

        if (etwLog.IsEnabled())
        {
            etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, $"{nameof(OletxPhase0VolatileEnlistmentContainer)}.{nameof(TMDown)}");
        }
    }

    // Be sure to lock this object before calling this.
    internal bool NewEnlistmentsAllowed
        => Phase == -1;

    internal void AddEnlistment(OletxVolatileEnlistment enlistment)
    {
        Debug.Assert(enlistment != null, "Argument is null");

        lock (this)
        {
            if (Phase != -1)
            {
                throw TransactionException.Create(SR.TooLate, null);
            }

            EnlistmentList.Add(enlistment);
        }
    }

    internal override void AddDependentClone()
    {
        lock (this)
        {
            if (Phase != -1)
            {
                throw TransactionException.CreateTransactionStateException(null);
            }

            IncompleteDependentClones++;
        }
    }

    internal override void DependentCloneCompleted()
    {
        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;

        bool doDecrement = false;
        lock (this)
        {
            if (etwLog.IsEnabled())
            {
                string description = "OletxPhase0VolatileEnlistmentContainer.DependentCloneCompleted, outstandingNotifications = " +
                    OutstandingNotifications.ToString(CultureInfo.CurrentCulture) +
                    ", incompleteDependentClones = " +
                    IncompleteDependentClones.ToString(CultureInfo.CurrentCulture) +
                    ", phase = " + Phase.ToString(CultureInfo.CurrentCulture);

                etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, this, description);
            }

            IncompleteDependentClones--;
            Debug.Assert(IncompleteDependentClones >= 0, "OletxPhase0VolatileEnlistmentContainer.DependentCloneCompleted - incompleteDependentClones < 0");

            // If we have not more incomplete dependent clones and we are in Phase 0, we need to "fake out" a notification completion.
            if (IncompleteDependentClones == 0 && Phase == 0)
            {
                OutstandingNotifications++;
                doDecrement = true;
            }
        }
        if (doDecrement)
        {
            DecrementOutstandingNotifications(true);
        }

        if (etwLog.IsEnabled())
        {
            string description = "OletxPhase0VolatileEnlistmentContainer.DependentCloneCompleted";
            etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, description);
        }
    }

    internal override void RollbackFromTransaction()
    {
        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;

        lock (this)
        {
            if (etwLog.IsEnabled())
            {
                string description = "OletxPhase0VolatileEnlistmentContainer.RollbackFromTransaction, outstandingNotifications = " +
                    OutstandingNotifications.ToString(CultureInfo.CurrentCulture) +
                    ", incompleteDependentClones = " + IncompleteDependentClones.ToString(CultureInfo.CurrentCulture);

                etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, this, description);
            }

            if (Phase == 0 && (OutstandingNotifications > 0 || IncompleteDependentClones > 0))
            {
                AlreadyVoted = true;
                // All we are going to do is release the Phase0Enlistment interface because there
                // is no negative vote to Phase0Request.
                Phase0EnlistmentShim?.Phase0Done(false);
            }
        }

        if (etwLog.IsEnabled())
        {
            string description = "OletxPhase0VolatileEnlistmentContainer.RollbackFromTransaction";
            etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, description);
        }
    }


    internal Phase0EnlistmentShim? Phase0EnlistmentShim
    {
        get
        {
            lock (this)
            {
                return _phase0EnlistmentShim;
            }
        }
        set
        {
            lock (this)
            {
                // If this.aborting is set to true, then we must have already received a
                // Phase0Request.  This could happen if the transaction aborts after the
                // enlistment is made, but before we are given the shim.
                if (_aborting || _tmWentDown)
                {
                    value!.Phase0Done(false);
                }
                _phase0EnlistmentShim = value;
            }
        }
    }

    internal override void DecrementOutstandingNotifications(bool voteYes)
    {
        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        bool respondToProxy = false;
        Phase0EnlistmentShim? localPhase0Shim = null;

        lock (this)
        {
            if (etwLog.IsEnabled())
            {
                string description = "OletxPhase0VolatileEnlistmentContainer.DecrementOutstandingNotifications, outstandingNotifications = " +
                    OutstandingNotifications.ToString(CultureInfo.CurrentCulture) +
                    ", incompleteDependentClones = " +
                    IncompleteDependentClones.ToString(CultureInfo.CurrentCulture);

                etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, this, description);
            }
            OutstandingNotifications--;
            Debug.Assert(OutstandingNotifications >= 0, "OletxPhase0VolatileEnlistmentContainer.DecrementOutstandingNotifications - outstandingNotifications < 0");

            CollectedVoteYes = CollectedVoteYes && voteYes;
            if (OutstandingNotifications == 0 && IncompleteDependentClones == 0)
            {
                if (Phase == 0 && !AlreadyVoted)
                {
                    respondToProxy = true;
                    AlreadyVoted = true;
                    localPhase0Shim = _phase0EnlistmentShim;
                }
                RealOletxTransaction.DecrementUndecidedEnlistments();
            }
        }

        try
        {
            if (respondToProxy)
            {
                localPhase0Shim?.Phase0Done(CollectedVoteYes && !RealOletxTransaction.Doomed);
            }
        }
        catch (COMException ex)
        {
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
            else if (OletxHelper.XACT_E_PROTOCOL == ex.ErrorCode)
            {
                _phase0EnlistmentShim = null;

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
            string description = "OletxPhase0VolatileEnlistmentContainer.DecrementOutstandingNotifications";

            etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, description);
        }
    }

    internal override void OutcomeFromTransaction(TransactionStatus outcome)
    {
        switch (outcome)
        {
            case TransactionStatus.Committed:
                Committed();
                break;
            case TransactionStatus.Aborted:
                Aborted();
                break;
            case TransactionStatus.InDoubt:
                InDoubt();
                break;
            default:
                Debug.Assert(false, "OletxPhase0VolatileEnlistmentContainer.OutcomeFromTransaction, outcome is not Commited or Aborted or InDoubt");
                break;
        }
    }

    internal override void Committed()
    {
        OletxVolatileEnlistment? enlistment;
        int localCount;

        lock (this)
        {
            Debug.Assert(Phase == 0 && OutstandingNotifications == 0);
            Phase = 2;
            localCount = EnlistmentList.Count;
        }

        for (int i = 0; i < localCount; i++)
        {
            enlistment = EnlistmentList[i] as OletxVolatileEnlistment;
            if (enlistment == null)
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.InternalError();
                }

                Debug.Assert(false, "OletxPhase1VolatileEnlistmentContainer.Committed, enlistmentList element is not an OletxVolatileEnlistment.");
                throw new InvalidOperationException(SR.InternalError);
            }

            enlistment.Commit();
        }
    }

    internal override void Aborted()
    {
        OletxVolatileEnlistment? enlistment;
        int localCount;

        lock (this)
        {
            // Tell all the enlistments that the transaction aborted and let the enlistment
            // state determine if the notification should be delivered.
            Phase = 2;
            localCount = EnlistmentList.Count;
        }

        for (int i = 0; i < localCount; i++)
        {
            enlistment = EnlistmentList[i] as OletxVolatileEnlistment;
            if (enlistment == null)
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.InternalError();
                }

                Debug.Assert(false, "OletxPhase1VolatileEnlistmentContainer.Aborted, enlistmentList element is not an OletxVolatileEnlistment.");
                throw new InvalidOperationException(SR.InternalError);
            }

            enlistment.Rollback();
        }
    }

    internal override void InDoubt()
    {
        OletxVolatileEnlistment? enlistment;
        int localCount;

        lock (this)
        {
            // Tell all the enlistments that the transaction is InDoubt and let the enlistment
            // state determine if the notification should be delivered.
            Phase = 2;
            localCount = EnlistmentList.Count;
        }

        for (int i = 0; i < localCount; i++)
        {
            enlistment = EnlistmentList[i] as OletxVolatileEnlistment;
            if (enlistment == null)
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.InternalError();
                }

                Debug.Fail("OletxPhase1VolatileEnlistmentContainer.InDoubt, enlistmentList element is not an OletxVolatileEnlistment.");
                throw new InvalidOperationException(SR.InternalError);
            }

            enlistment.InDoubt();
        }
    }

    internal void Phase0Request(bool abortHint)
    {
        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        OletxVolatileEnlistment? enlistment;
        int localCount;
        OletxCommittableTransaction? committableTx;
        bool commitNotYetCalled = false;

        lock (this)
        {
            if (etwLog.IsEnabled())
            {
                string description = "OletxPhase0VolatileEnlistmentContainer.Phase0Request, abortHint = " +
                    abortHint.ToString(CultureInfo.CurrentCulture) +
                    ", phase = " + Phase.ToString(CultureInfo.CurrentCulture);

                etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, this, description);
            }

            _aborting = abortHint;
            committableTx = RealOletxTransaction.CommittableTransaction;
            if (committableTx != null)
            {
                // We are dealing with the committable transaction.  If Commit or BeginCommit has NOT been
                // called, then we are dealing with a situation where the TM went down and we are getting
                // a bogus Phase0Request with abortHint = false (COMPlus bug 36760/36758).  This is an attempt
                // to not send the app a Prepare request when we know the transaction is going to abort.
                if (!committableTx.CommitCalled)
                {
                    commitNotYetCalled = true;
                    _aborting = true;
                }
            }
            // It's possible that we are in phase 2 if we got an Aborted outcome from the transaction before we got the
            // Phase0Request.  In both cases, we just respond to the proxy and don't bother telling the enlistments.
            // They have either already heard about the abort or will soon.
            if (Phase == 2 || Phase == -1)
            {
                if (Phase == -1)
                {
                    Phase = 0;
                }

                // If we got an abort hint or we are the committable transaction and Commit has not yet been called or the TM went down,
                // we don't want to do any more work on the transaction.  The abort notifications will be sent by the phase 1
                // enlistment
                if (_aborting || _tmWentDown || commitNotYetCalled || Phase == 2)
                {
                    // There is a possible race where we could get the Phase0Request before we are given the
                    // shim.  In that case, we will vote "no" when we are given the shim.
                    if (_phase0EnlistmentShim != null)
                    {
                        try
                        {
                            _phase0EnlistmentShim.Phase0Done(false);
                            // We need to set the alreadyVoted flag to true once we successfully voted, so later we don't vote again when OletxDependentTransaction::Complete is called
                            // Otherwise, in OletxPhase0VolatileEnlistmentContainer::DecrementOutstandingNotifications code path, we are going to call Phase0Done( true ) again
                            // and result in an access violation while accessing the pPhase0EnlistmentAsync member variable of the Phase0Shim object.
                            AlreadyVoted = true;
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
                    return;
                }
                OutstandingNotifications = EnlistmentList.Count;
                localCount = EnlistmentList.Count;
                // If we don't have any enlistments, then we must have created this container for
                // delay commit dependent clones only.  So we need to fake a notification.
                if (localCount == 0)
                {
                    OutstandingNotifications = 1;
                }
            }
            else  // any other phase is bad news.
            {
                if (etwLog.IsEnabled())
                {
                    etwLog.InternalError("OletxPhase0VolatileEnlistmentContainer.Phase0Request, phase != -1");
                }

                Debug.Fail("OletxPhase0VolatileEnlistmentContainer.Phase0Request, phase != -1");
                throw new InvalidOperationException(SR.InternalError);
            }
        }

        // We may not have any Phase0 volatile enlistments, which means that this container
        // got created solely for delay commit dependent transactions.  We need to fake out a
        // notification completion.
        if (localCount == 0)
        {
            DecrementOutstandingNotifications(true);
        }
        else
        {
            for (int i = 0; i < localCount; i++)
            {
                enlistment = EnlistmentList[i] as OletxVolatileEnlistment;
                if (enlistment == null)
                {
                    if (etwLog.IsEnabled())
                    {
                        etwLog.InternalError();
                    }

                    Debug.Fail("OletxPhase0VolatileEnlistmentContainer.Phase0Request, enlistmentList element is not an OletxVolatileEnlistment.");
                    throw new InvalidOperationException(SR.InternalError);
                }

                // Do the notification outside any locks.
                Debug.Assert(enlistment.EnlistDuringPrepareRequired, "OletxPhase0VolatileEnlistmentContainer.Phase0Request, enlistmentList element not marked as EnlistmentDuringPrepareRequired.");
                Debug.Assert(!abortHint, "OletxPhase0VolatileEnlistmentContainer.Phase0Request, abortingHint is true just before sending Prepares.");

                enlistment.Prepare(this);
            }
        }

        if (etwLog.IsEnabled())
        {
            string description = "OletxPhase0VolatileEnlistmentContainer.Phase0Request, abortHint = " + abortHint.ToString(CultureInfo.CurrentCulture);

            etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, description);
        }
    }
}

internal sealed class OletxPhase1VolatileEnlistmentContainer : OletxVolatileEnlistmentContainer
{
    private VoterBallotShim? _voterBallotShim;

    internal OletxPhase1VolatileEnlistmentContainer(RealOletxTransaction realOletxTransaction)
        : base(realOletxTransaction)
    {
        // This will be set later, after the caller creates the enlistment with the proxy.
        _voterBallotShim = null;

        Phase = -1;
        OutstandingNotifications = 0;
        IncompleteDependentClones = 0;
        AlreadyVoted = false;

        // If anybody votes false, this will get set to false.
        CollectedVoteYes = true;

        // This is a new undecided enlistment on the transaction.  Do this last since it has side affects.
        realOletxTransaction.IncrementUndecidedEnlistments();
    }

    // Returns true if this container is enlisted for Phase 0.
    internal void AddEnlistment(OletxVolatileEnlistment enlistment)
    {
        Debug.Assert(enlistment != null, "Argument is null");

        lock (this)
        {
            if (Phase != -1)
            {
                throw TransactionException.Create(SR.TooLate, null);
            }

            EnlistmentList.Add(enlistment);
        }
    }

    internal override void AddDependentClone()
    {
        lock (this)
        {
            if (Phase != -1)
            {
                throw TransactionException.CreateTransactionStateException(null, Guid.Empty);
            }

            // We simply need to block the response to the proxy until all clone is completed.
            IncompleteDependentClones++;
        }
    }

    internal override void DependentCloneCompleted()
    {
        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        if (etwLog.IsEnabled())
        {
            string description = "OletxPhase1VolatileEnlistmentContainer.DependentCloneCompleted, outstandingNotifications = " +
                OutstandingNotifications.ToString(CultureInfo.CurrentCulture) +
                ", incompleteDependentClones = " +
                IncompleteDependentClones.ToString(CultureInfo.CurrentCulture) +
                ", phase = " + Phase.ToString(CultureInfo.CurrentCulture);

            etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, this, description);
        }

        // This is to synchronize with the corresponding AddDependentClone which takes the container lock while incrementing the incompleteDependentClone count
        lock (this)
        {
            IncompleteDependentClones--;
        }

        Debug.Assert(OutstandingNotifications >= 0, "OletxPhase1VolatileEnlistmentContainer.DependentCloneCompleted - DependentCloneCompleted < 0");

        if (etwLog.IsEnabled())
        {
            string description = "OletxPhase1VolatileEnlistmentContainer.DependentCloneCompleted";
            etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, description);
        }
    }

    internal override void RollbackFromTransaction()
    {
        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        bool voteNo = false;
        VoterBallotShim? localVoterShim = null;

        lock (this)
        {
            if (etwLog.IsEnabled())
            {
                string description = "OletxPhase1VolatileEnlistmentContainer.RollbackFromTransaction, outstandingNotifications = " +
                    OutstandingNotifications.ToString(CultureInfo.CurrentCulture) +
                    ", incompleteDependentClones = " + IncompleteDependentClones.ToString(CultureInfo.CurrentCulture);

                etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, this, description);
            }

            if (Phase == 1 && OutstandingNotifications > 0)
            {
                AlreadyVoted = true;
                voteNo = true;
                localVoterShim = _voterBallotShim;
            }
        }

        if (voteNo)
        {
            try
            {
                localVoterShim?.Vote(false);

                // We are not going to hear anymore from the proxy if we voted no, so we need to tell the
                // enlistments to rollback.  The state of the OletxVolatileEnlistment will determine whether or
                // not the notification actually goes out to the app.
                Aborted();
            }
            catch (COMException ex) when (ex.ErrorCode == OletxHelper.XACT_E_CONNECTION_DOWN || ex.ErrorCode == OletxHelper.XACT_E_TMNOTAVAILABLE)
            {
                lock (this)
                {
                    // If we are in phase 1, we need to tell the enlistments that the transaction is InDoubt.
                    if (Phase == 1)
                    {
                        InDoubt();
                    }
                }

                if (etwLog.IsEnabled())
                {
                    etwLog.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
                }
            }
        }

        if (etwLog.IsEnabled())
        {
            etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxPhase1VolatileEnlistmentContainer.RollbackFromTransaction");
        }
    }

    internal VoterBallotShim? VoterBallotShim
    {
        get
        {
            lock (this)
            {
                return _voterBallotShim;
            }
        }
        set
        {
            lock (this)
            {
                _voterBallotShim = value;
            }
        }
    }

    internal override void DecrementOutstandingNotifications(bool voteYes)
    {
        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        bool respondToProxy = false;
        VoterBallotShim? localVoterShim = null;

        lock (this)
        {
            if (etwLog.IsEnabled())
            {
                string description = "OletxPhase1VolatileEnlistmentContainer.DecrementOutstandingNotifications, outstandingNotifications = " +
                    OutstandingNotifications.ToString(CultureInfo.CurrentCulture) +
                    ", incompleteDependentClones = " +
                    IncompleteDependentClones.ToString(CultureInfo.CurrentCulture);

                etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, this, description);
            }

            OutstandingNotifications--;
            Debug.Assert(OutstandingNotifications >= 0, "OletxPhase1VolatileEnlistmentContainer.DecrementOutstandingNotifications - outstandingNotifications < 0");
            CollectedVoteYes = CollectedVoteYes && voteYes;
            if (OutstandingNotifications == 0)
            {
                if (Phase == 1 && !AlreadyVoted)
                {
                    respondToProxy = true;
                    AlreadyVoted = true;
                    localVoterShim = VoterBallotShim;
                }
                RealOletxTransaction.DecrementUndecidedEnlistments();
            }
        }

        try
        {
            if (respondToProxy)
            {
                if (CollectedVoteYes && !RealOletxTransaction.Doomed)
                {
                    localVoterShim?.Vote(true);
                }
                else  // we need to vote no.
                {
                    localVoterShim?.Vote(false);

                    // We are not going to hear anymore from the proxy if we voted no, so we need to tell the
                    // enlistments to rollback.  The state of the OletxVolatileEnlistment will determine whether or
                    // not the notification actually goes out to the app.
                    Aborted();
                }
            }
        }
        catch (COMException ex) when (ex.ErrorCode == OletxHelper.XACT_E_CONNECTION_DOWN || ex.ErrorCode == OletxHelper.XACT_E_TMNOTAVAILABLE)
        {
            lock (this)
            {
                // If we are in phase 1, we need to tell the enlistments that the transaction is InDoubt.
                if (Phase == 1)
                {
                    InDoubt();
                }

                // There is nothing special to do for phase 2.
            }

            if (etwLog.IsEnabled())
            {
                etwLog.ExceptionConsumed(TraceSourceType.TraceSourceOleTx, ex);
            }
        }

        if (etwLog.IsEnabled())
        {
            etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxPhase1VolatileEnlistmentContainer.DecrementOutstandingNotifications");
        }
    }

    internal override void OutcomeFromTransaction(TransactionStatus outcome)
    {
        bool driveAbort = false;
        bool driveInDoubt = false;

        lock (this)
        {
            // If we are in Phase 1 and still have outstanding notifications, we need
            // to drive sending of the outcome to the enlistments.  If we are in any
            // other phase, or we don't have outstanding notifications, we will eventually
            // get the outcome notification on our OWN voter enlistment, so we will just
            // wait for that.
            if (Phase == 1 && OutstandingNotifications > 0)
            {
                switch (outcome)
                {
                    case TransactionStatus.Aborted:
                        driveAbort = true;
                        break;
                    case TransactionStatus.InDoubt:
                        driveInDoubt = true;
                        break;
                    default:
                        Debug.Fail("OletxPhase1VolatileEnlistmentContainer.OutcomeFromTransaction, outcome is not Aborted or InDoubt");
                        break;
                }
            }
        }

        if (driveAbort)
        {
            Aborted();
        }

        if (driveInDoubt)
        {
            InDoubt();
        }
    }

    internal override void Committed()
    {
        OletxVolatileEnlistment? enlistment;
        int localPhase1Count;

        lock (this)
        {
            Phase = 2;
            localPhase1Count = EnlistmentList.Count;
        }

        for (int i = 0; i < localPhase1Count; i++)
        {
            enlistment = EnlistmentList[i] as OletxVolatileEnlistment;
            if (enlistment == null)
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.InternalError();
                }

                Debug.Fail("OletxPhase1VolatileEnlistmentContainer.Committed, enlistmentList element is not an OletxVolatileEnlistment.");
                throw new InvalidOperationException(SR.InternalError);
            }

            enlistment.Commit();
        }
    }

    internal override void Aborted()
    {
        OletxVolatileEnlistment? enlistment;
        int localPhase1Count;

        lock (this)
        {
            Phase = 2;
            localPhase1Count = EnlistmentList.Count;
        }

        for (int i = 0; i < localPhase1Count; i++)
        {
            enlistment = EnlistmentList[i] as OletxVolatileEnlistment;
            if (enlistment == null)
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.InternalError();
                }

                Debug.Fail("OletxPhase1VolatileEnlistmentContainer.Aborted, enlistmentList element is not an OletxVolatileEnlistment.");
                throw new InvalidOperationException(SR.InternalError);
            }

            enlistment.Rollback();
        }
    }

    internal override void InDoubt()
    {
        OletxVolatileEnlistment? enlistment;
        int localPhase1Count;

        lock (this)
        {
            Phase = 2;
            localPhase1Count = EnlistmentList.Count;
        }

        for (int i = 0; i < localPhase1Count; i++)
        {
            enlistment = EnlistmentList[i] as OletxVolatileEnlistment;
            if (enlistment == null)
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.InternalError();
                }

                Debug.Fail("OletxPhase1VolatileEnlistmentContainer.InDoubt, enlistmentList element is not an OletxVolatileEnlistment.");
                throw new InvalidOperationException(SR.InternalError);
            }

            enlistment.InDoubt();
        }
    }

    internal void VoteRequest()
    {
        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        OletxVolatileEnlistment? enlistment;
        int localPhase1Count = 0;
        bool voteNo = false;

        lock (this)
        {
            if (etwLog.IsEnabled())
            {
                string description = "OletxPhase1VolatileEnlistmentContainer.VoteRequest";
                etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, this, description);
            }

            Phase = 1;

            // If we still have incomplete dependent clones, vote no now.
            if (IncompleteDependentClones > 0)
            {
                voteNo = true;
                OutstandingNotifications = 1;
            }
            else
            {
                OutstandingNotifications = EnlistmentList.Count;
                localPhase1Count = EnlistmentList.Count;
                // We may not have an volatile phase 1 enlistments, which means that this
                // container was created only for non-delay commit dependent clones.  If that
                // is the case, fake out a notification and response.
                if (localPhase1Count == 0)
                {
                    OutstandingNotifications = 1;
                }
            }

            RealOletxTransaction.TooLateForEnlistments = true;
        }

        if (voteNo)
        {
            DecrementOutstandingNotifications(false);
        }
        else if (localPhase1Count == 0)
        {
            DecrementOutstandingNotifications(true);
        }
        else
        {
            for (int i = 0; i < localPhase1Count; i++)
            {
                enlistment = EnlistmentList[i] as OletxVolatileEnlistment;
                if (enlistment == null)
                {
                    if (etwLog.IsEnabled())
                    {
                        etwLog.InternalError();
                    }

                    Debug.Fail("OletxPhase1VolatileEnlistmentContainer.VoteRequest, enlistmentList element is not an OletxVolatileEnlistment.");
                    throw new InvalidOperationException(SR.InternalError);
                }

                enlistment.Prepare(this);
            }
        }

        if (etwLog.IsEnabled())
        {
            string description = "OletxPhase1VolatileEnlistmentContainer.VoteRequest";
            etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, description);
        }
    }
}

internal sealed class OletxVolatileEnlistment : OletxBaseEnlistment, IPromotedEnlistment
{
    private enum OletxVolatileEnlistmentState
    {
        Active,
        Preparing,
        Committing,
        Aborting,
        Prepared,
        Aborted,
        InDoubt,
        Done
    }

    private readonly IEnlistmentNotificationInternal _iEnlistmentNotification;
    private OletxVolatileEnlistmentState _state = OletxVolatileEnlistmentState.Active;
    private OletxVolatileEnlistmentContainer? _container;
    internal bool EnlistDuringPrepareRequired;

    // This is used if the transaction outcome is received while a prepare request
    // is still outstanding to an app.  Active means no outcome, yet.  Aborted means
    // we should tell the app Aborted.  InDoubt means tell the app InDoubt.  This
    // should never be Committed because we shouldn't receive a Committed notification
    // from the proxy while we have a Prepare outstanding.
    private TransactionStatus _pendingOutcome;

    internal OletxVolatileEnlistment(
        IEnlistmentNotificationInternal enlistmentNotification,
        EnlistmentOptions enlistmentOptions,
        OletxTransaction oletxTransaction)
        : base(null!, oletxTransaction)
    {
        _iEnlistmentNotification = enlistmentNotification;
        EnlistDuringPrepareRequired = (enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0;

        // We get a container when we are asked to vote.
        _container = null;

        _pendingOutcome = TransactionStatus.Active;

        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        if (etwLog.IsEnabled())
        {
            etwLog.EnlistmentCreated(TraceSourceType.TraceSourceOleTx, InternalTraceIdentifier, EnlistmentType.Volatile, enlistmentOptions);
        }
    }

    internal void Prepare(OletxVolatileEnlistmentContainer container)
    {
        OletxVolatileEnlistmentState localState = OletxVolatileEnlistmentState.Active;
        IEnlistmentNotificationInternal localEnlistmentNotification;

        lock (this)
        {
            localEnlistmentNotification = _iEnlistmentNotification;

            // The app may have already called EnlistmentDone.  If this occurs, don't bother sending
            // the notification to the app.
            if (OletxVolatileEnlistmentState.Active == _state)
            {
                localState = _state = OletxVolatileEnlistmentState.Preparing;
            }
            else
            {
                localState = _state;
            }
            _container = container;
        }

        // Tell the application to do the work.
        if (localState == OletxVolatileEnlistmentState.Preparing)
        {
            if (localEnlistmentNotification != null)
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.EnlistmentStatus(TraceSourceType.TraceSourceOleTx, InternalTraceIdentifier, NotificationCall.Prepare);
                }

                localEnlistmentNotification.Prepare(this);
            }
            else
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.InternalError();
                }

                Debug.Fail("OletxVolatileEnlistment.Prepare, no enlistmentNotification member.");
                throw new InvalidOperationException(SR.InternalError);
            }
        }
        else if (localState == OletxVolatileEnlistmentState.Done)
        {
            // Voting yes because it was an early read-only vote.
            container.DecrementOutstandingNotifications(true);

            // We must have had a race between EnlistmentDone and the proxy telling
            // us Phase0Request.  Just return.
            return;
        }
        // It is okay to be in Prepared state if we are edpr=true because we already
        // did our prepare in Phase0.
        else if (localState == OletxVolatileEnlistmentState.Prepared && EnlistDuringPrepareRequired)
        {
            container.DecrementOutstandingNotifications(true);
            return;
        }
        else if (localState is OletxVolatileEnlistmentState.Aborting or OletxVolatileEnlistmentState.Aborted)
        {
            // An abort has raced with this volatile Prepare
            // decrement the outstanding notifications making sure to vote no.
            container.DecrementOutstandingNotifications(false);
            return;
        }
        else
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.InternalError();
            }

            Debug.Fail("OletxVolatileEnlistment.Prepare, invalid state.");
            throw new InvalidOperationException(SR.InternalError);
        }
    }

    internal void Commit()
    {
        OletxVolatileEnlistmentState localState = OletxVolatileEnlistmentState.Active;
        IEnlistmentNotificationInternal? localEnlistmentNotification = null;

        lock (this)
        {
            // The app may have already called EnlistmentDone.  If this occurs, don't bother sending
            // the notification to the app and we don't need to tell the proxy.
            if (_state == OletxVolatileEnlistmentState.Prepared)
            {
                localState = _state = OletxVolatileEnlistmentState.Committing;
                localEnlistmentNotification = _iEnlistmentNotification;
            }
            else
            {
                localState = _state;
            }
        }

        // Tell the application to do the work.
        if (OletxVolatileEnlistmentState.Committing == localState)
        {
            if (localEnlistmentNotification != null)
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.EnlistmentStatus(TraceSourceType.TraceSourceOleTx, InternalTraceIdentifier, NotificationCall.Commit);
                }

                localEnlistmentNotification.Commit(this);
            }
            else
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.InternalError();
                }

                Debug.Fail("OletxVolatileEnlistment.Commit, no enlistmentNotification member.");
                throw new InvalidOperationException(SR.InternalError);
            }
        }
        else if (localState == OletxVolatileEnlistmentState.Done)
        {
            // Early Exit - state was Done
        }
        else
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.InternalError();
            }

            Debug.Fail("OletxVolatileEnlistment.Commit, invalid state.");
            throw new InvalidOperationException(SR.InternalError);
        }
    }

    internal void Rollback()
    {
        OletxVolatileEnlistmentState localState = OletxVolatileEnlistmentState.Active;
        IEnlistmentNotificationInternal? localEnlistmentNotification = null;

        lock (this)
        {
            // The app may have already called EnlistmentDone.  If this occurs, don't bother sending
            // the notification to the app and we don't need to tell the proxy.
            if (_state is OletxVolatileEnlistmentState.Prepared or OletxVolatileEnlistmentState.Active)
            {
                localState = _state = OletxVolatileEnlistmentState.Aborting;
                localEnlistmentNotification = _iEnlistmentNotification;
            }
            else
            {
                if (_state == OletxVolatileEnlistmentState.Preparing)
                {
                    _pendingOutcome = TransactionStatus.Aborted;
                }

                localState = _state;
            }
        }

        switch (localState)
        {
            // Tell the application to do the work.
            case OletxVolatileEnlistmentState.Aborting:
                {
                    if (localEnlistmentNotification != null)
                    {
                        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                        if (etwLog.IsEnabled())
                        {
                            etwLog.EnlistmentStatus(TraceSourceType.TraceSourceOleTx, InternalTraceIdentifier, NotificationCall.Rollback);
                        }

                        localEnlistmentNotification.Rollback(this);
                    }

                    // There is a small race where Rollback could be called when the enlistment is already
                    // aborting the transaciton, so just ignore that call.  When the app enlistment
                    // finishes responding to its Rollback notification with EnlistmentDone, things will get
                    // cleaned up.
                    break;
                }
            case OletxVolatileEnlistmentState.Preparing:
                // We need to tolerate this state, but we have already marked the
                // enlistment as pendingRollback, so there is nothing else to do here.
                break;
            case OletxVolatileEnlistmentState.Done:
                // Early Exit - state was Done
                break;
            default:
                {
                    TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                    if (etwLog.IsEnabled())
                    {
                        etwLog.InternalError();
                    }

                    Debug.Fail("OletxVolatileEnlistment.Rollback, invalid state.");
                    throw new InvalidOperationException(SR.InternalError);
                }
        }
    }

    internal void InDoubt()
    {
        OletxVolatileEnlistmentState localState = OletxVolatileEnlistmentState.Active;
        IEnlistmentNotificationInternal? localEnlistmentNotification = null;

        lock (this)
        {
            // The app may have already called EnlistmentDone.  If this occurs, don't bother sending
            // the notification to the app and we don't need to tell the proxy.
            if (_state == OletxVolatileEnlistmentState.Prepared)
            {
                localState = _state = OletxVolatileEnlistmentState.InDoubt;
                localEnlistmentNotification = _iEnlistmentNotification;
            }
            else
            {
                if (_state == OletxVolatileEnlistmentState.Preparing)
                {
                    _pendingOutcome = TransactionStatus.InDoubt;
                }
                localState = _state;
            }
        }

        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;

        switch (localState)
        {
            // Tell the application to do the work.
            case OletxVolatileEnlistmentState.InDoubt when localEnlistmentNotification != null:
                {
                    if (etwLog.IsEnabled())
                    {
                        etwLog.EnlistmentStatus(TraceSourceType.TraceSourceOleTx, InternalTraceIdentifier, NotificationCall.InDoubt);
                    }

                    localEnlistmentNotification.InDoubt(this);
                    break;
                }
            case OletxVolatileEnlistmentState.InDoubt:
                {
                    if (etwLog.IsEnabled())
                    {
                        etwLog.InternalError();
                    }

                    Debug.Fail("OletxVolatileEnlistment.InDoubt, no enlistmentNotification member.");
                    throw new InvalidOperationException(SR.InternalError);
                }
            case OletxVolatileEnlistmentState.Preparing:
                // We have already set pendingOutcome, so there is nothing else to do.
                break;
            case OletxVolatileEnlistmentState.Done:
                // Early Exit - state was Done
                break;
            default:
                {
                    if (etwLog.IsEnabled())
                    {
                        etwLog.InternalError();
                    }

                    Debug.Fail("OletxVolatileEnlistment.InDoubt, invalid state.");
                    throw new InvalidOperationException(SR.InternalError);
                }
        }
    }

    void IPromotedEnlistment.EnlistmentDone()
    {
        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        if (etwLog.IsEnabled())
        {
            etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, this, $"{nameof(OletxEnlistment)}.{nameof(IPromotedEnlistment.EnlistmentDone)}");
            etwLog.EnlistmentCallbackPositive(InternalTraceIdentifier, EnlistmentCallback.Done);
        }

        OletxVolatileEnlistmentState localState = OletxVolatileEnlistmentState.Active;
        OletxVolatileEnlistmentContainer? localContainer;

        lock (this)
        {
            localState = _state;
            localContainer = _container;

            if (_state != OletxVolatileEnlistmentState.Active &&
                _state != OletxVolatileEnlistmentState.Preparing &&
                _state != OletxVolatileEnlistmentState.Aborting &&
                _state != OletxVolatileEnlistmentState.Committing &&
                _state != OletxVolatileEnlistmentState.InDoubt)
            {
                throw TransactionException.CreateEnlistmentStateException(null, DistributedTxId);
            }

            _state = OletxVolatileEnlistmentState.Done;
        }

        // For the Preparing state, we need to decrement the outstanding
        // count with the container.  If the state is Active, it is an early vote so we
        // just stay in the Done state and when we get the Prepare, we will vote appropriately.
        if (localState == OletxVolatileEnlistmentState.Preparing)
        {
            // Specify true.  If aborting, it is okay because the transaction is already
            // aborting.
            localContainer?.DecrementOutstandingNotifications(true);
        }

        if (etwLog.IsEnabled())
        {
            etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, $"{nameof(OletxEnlistment)}.{nameof(IPromotedEnlistment.EnlistmentDone)}");
        }
    }

    void IPromotedEnlistment.Prepared()
    {
        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        if (etwLog.IsEnabled())
        {
            etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, this, $"OletxPreparingEnlistment.{nameof(IPromotedEnlistment.Prepared)}");
            etwLog.EnlistmentCallbackPositive(InternalTraceIdentifier, EnlistmentCallback.Prepared);
        }

        OletxVolatileEnlistmentContainer localContainer;
        TransactionStatus localPendingOutcome = TransactionStatus.Active;

        lock (this)
        {
            if (_state != OletxVolatileEnlistmentState.Preparing)
            {
                throw TransactionException.CreateEnlistmentStateException(null, DistributedTxId);
            }

            _state = OletxVolatileEnlistmentState.Prepared;
            localPendingOutcome = _pendingOutcome;

            if (_container == null)
            {
                if (etwLog.IsEnabled())
                {
                    etwLog.InternalError();
                }

                Debug.Fail("OletxVolatileEnlistment.Prepared, no container member.");
                throw new InvalidOperationException(SR.InternalError);
            }

            localContainer = _container;
        }

        // Vote yes.
        localContainer.DecrementOutstandingNotifications(true);

        switch (localPendingOutcome)
        {
            case TransactionStatus.Active:
                // nothing to do.  Everything is proceeding as normal.
                break;

            case TransactionStatus.Aborted:
                // The transaction aborted while the Prepare was outstanding.
                // We need to tell the app to rollback.
                Rollback();
                break;

            case TransactionStatus.InDoubt:
                // The transaction went InDoubt while the Prepare was outstanding.
                // We need to tell the app.
                InDoubt();
                break;

            default:
                // This shouldn't happen.
                if (etwLog.IsEnabled())
                {
                    etwLog.InternalError();
                }

                Debug.Fail("OletxVolatileEnlistment.Prepared, invalid pending outcome value.");
                throw new InvalidOperationException(SR.InternalError);
        }

        if (etwLog.IsEnabled())
        {
            etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, $"OletxPreparingEnlistment.{nameof(IPromotedEnlistment.Prepared)}");
        }
    }

    void IPromotedEnlistment.ForceRollback()
        => ((IPromotedEnlistment)this).ForceRollback(null);

    void IPromotedEnlistment.ForceRollback(Exception? e)
    {
        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        if (etwLog.IsEnabled())
        {
            etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, this, $"OletxPreparingEnlistment.{nameof(IPromotedEnlistment.ForceRollback)}");
            etwLog.EnlistmentCallbackNegative(InternalTraceIdentifier, EnlistmentCallback.ForceRollback);
        }

        OletxVolatileEnlistmentContainer localContainer;

        lock (this)
        {
            if (_state != OletxVolatileEnlistmentState.Preparing)
            {
                throw TransactionException.CreateEnlistmentStateException(null, DistributedTxId);
            }

            // There are no more notifications that need to happen on this enlistment.
            _state = OletxVolatileEnlistmentState.Done;

            if (_container == null)
            {
                if (etwLog.IsEnabled())
                {
                    etwLog.InternalError();
                }

                Debug.Fail("OletxVolatileEnlistment.ForceRollback, no container member.");
                throw new InvalidOperationException(SR.InternalError);
            }

            localContainer = _container;
        }

        Interlocked.CompareExchange(ref oletxTransaction!.RealOletxTransaction.InnerException, e, null);

        // Vote no.
        localContainer.DecrementOutstandingNotifications(false);

        if (etwLog.IsEnabled())
        {
            etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, $"OletxPreparingEnlistment.{nameof(IPromotedEnlistment.ForceRollback)}");
        }
    }

    void IPromotedEnlistment.Committed() => throw new InvalidOperationException();
    void IPromotedEnlistment.Aborted() => throw new InvalidOperationException();
    void IPromotedEnlistment.Aborted(Exception? e) => throw new InvalidOperationException();
    void IPromotedEnlistment.InDoubt() => throw new InvalidOperationException();
    void IPromotedEnlistment.InDoubt(Exception? e) => throw new InvalidOperationException();

    byte[] IPromotedEnlistment.GetRecoveryInformation()
        => throw TransactionException.CreateInvalidOperationException(
            TraceSourceType.TraceSourceOleTx,
            SR.VolEnlistNoRecoveryInfo,
            null,
            DistributedTxId);

    InternalEnlistment? IPromotedEnlistment.InternalEnlistment
    {
        get => InternalEnlistment;
        set => InternalEnlistment = value;
    }
}
