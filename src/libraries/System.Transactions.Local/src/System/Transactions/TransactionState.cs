// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading;
using System.Transactions.Distributed;

namespace System.Transactions
{
    // The TransactionState object defines the basic set of operations that
    // are available for a transaction.  It is a base type and the base
    // implementations all throw exceptions.  For a particular state a derived
    // implementation will inheret from this object and implement the appropriate
    // operations for that state.
    internal abstract class TransactionState
    {
        // The state machines themselves are designed to be internally consistent.  So the only externally visable
        // state transition is to active.  All other state transitions must happen within the state machines
        // themselves.
        private static TransactionStateActive? s_transactionStateActive;
        private static TransactionStateSubordinateActive? s_transactionStateSubordinateActive;
        private static TransactionStatePhase0? s_transactionStatePhase0;
        private static TransactionStateVolatilePhase1? s_transactionStateVolatilePhase1;
        private static TransactionStateVolatileSPC? s_transactionStateVolatileSPC;
        private static TransactionStateSPC? s_transactionStateSPC;
        private static TransactionStateAborted? s_transactionStateAborted;
        private static TransactionStateCommitted? s_transactionStateCommitted;
        private static TransactionStateInDoubt? s_transactionStateInDoubt;

        private static TransactionStatePromoted? s_transactionStatePromoted;
        private static TransactionStateNonCommittablePromoted? s_transactionStateNonCommittablePromoted;
        private static TransactionStatePromotedP0Wave? s_transactionStatePromotedP0Wave;
        private static TransactionStatePromotedCommitting? s_transactionStatePromotedCommitting;
        private static TransactionStatePromotedPhase0? s_transactionStatePromotedPhase0;
        private static TransactionStatePromotedPhase1? s_transactionStatePromotedPhase1;
        private static TransactionStatePromotedP0Aborting? s_transactionStatePromotedP0Aborting;
        private static TransactionStatePromotedP1Aborting? s_transactionStatePromotedP1Aborting;
        private static TransactionStatePromotedAborted? s_transactionStatePromotedAborted;
        private static TransactionStatePromotedCommitted? s_transactionStatePromotedCommitted;
        private static TransactionStatePromotedIndoubt? s_transactionStatePromotedIndoubt;

        private static TransactionStateDelegated? s_transactionStateDelegated;
        private static TransactionStateDelegatedSubordinate? s_transactionStateDelegatedSubordinate;
        private static TransactionStateDelegatedP0Wave? s_transactionStateDelegatedP0Wave;
        private static TransactionStateDelegatedCommitting? s_transactionStateDelegatedCommitting;
        private static TransactionStateDelegatedAborting? s_transactionStateDelegatedAborting;
        private static TransactionStatePSPEOperation? s_transactionStatePSPEOperation;

        private static TransactionStateDelegatedNonMSDTC? s_transactionStateDelegatedNonMSDTC;
        private static TransactionStatePromotedNonMSDTCPhase0? s_transactionStatePromotedNonMSDTCPhase0;
        private static TransactionStatePromotedNonMSDTCVolatilePhase1? s_transactionStatePromotedNonMSDTCVolatilePhase1;
        private static TransactionStatePromotedNonMSDTCSinglePhaseCommit? s_transactionStatePromotedNonMSDTCSinglePhaseCommit;
        private static TransactionStatePromotedNonMSDTCAborted? s_transactionStatePromotedNonMSDTCAborted;
        private static TransactionStatePromotedNonMSDTCCommitted? s_transactionStatePromotedNonMSDTCCommitted;
        private static TransactionStatePromotedNonMSDTCIndoubt? s_transactionStatePromotedNonMSDTCIndoubt;

        // Object for synchronizing access to the entire class( avoiding lock( typeof( ... )) )
        internal static object? s_classSyncObject;

        internal static TransactionStateActive TransactionStateActive =>
            LazyInitializer.EnsureInitialized(ref s_transactionStateActive, ref s_classSyncObject, () => new TransactionStateActive());

        internal static TransactionStateSubordinateActive TransactionStateSubordinateActive =>
            LazyInitializer.EnsureInitialized(ref s_transactionStateSubordinateActive, ref s_classSyncObject, () => new TransactionStateSubordinateActive());

        internal static TransactionStatePSPEOperation TransactionStatePSPEOperation =>
            LazyInitializer.EnsureInitialized(ref s_transactionStatePSPEOperation, ref s_classSyncObject, () => new TransactionStatePSPEOperation());

        protected static TransactionStatePhase0 TransactionStatePhase0 =>
            LazyInitializer.EnsureInitialized(ref s_transactionStatePhase0, ref s_classSyncObject, () => new TransactionStatePhase0());

        protected static TransactionStateVolatilePhase1 TransactionStateVolatilePhase1 =>
            LazyInitializer.EnsureInitialized(ref s_transactionStateVolatilePhase1, ref s_classSyncObject, () => new TransactionStateVolatilePhase1());

        protected static TransactionStateVolatileSPC TransactionStateVolatileSPC =>
            LazyInitializer.EnsureInitialized(ref s_transactionStateVolatileSPC, ref s_classSyncObject, () => new TransactionStateVolatileSPC());

        protected static TransactionStateSPC TransactionStateSPC =>
            LazyInitializer.EnsureInitialized(ref s_transactionStateSPC, ref s_classSyncObject, () => new TransactionStateSPC());

        protected static TransactionStateAborted TransactionStateAborted =>
            LazyInitializer.EnsureInitialized(ref s_transactionStateAborted, ref s_classSyncObject, () => new TransactionStateAborted());

        protected static TransactionStateCommitted TransactionStateCommitted =>
            LazyInitializer.EnsureInitialized(ref s_transactionStateCommitted, ref s_classSyncObject, () => new TransactionStateCommitted());

        protected static TransactionStateInDoubt TransactionStateInDoubt =>
            LazyInitializer.EnsureInitialized(ref s_transactionStateInDoubt, ref s_classSyncObject, () => new TransactionStateInDoubt());

        internal static TransactionStatePromoted TransactionStatePromoted =>
            LazyInitializer.EnsureInitialized(ref s_transactionStatePromoted, ref s_classSyncObject, () => new TransactionStatePromoted());

        internal static TransactionStateNonCommittablePromoted TransactionStateNonCommittablePromoted =>
            LazyInitializer.EnsureInitialized(ref s_transactionStateNonCommittablePromoted, ref s_classSyncObject, () => new TransactionStateNonCommittablePromoted());

        protected static TransactionStatePromotedP0Wave TransactionStatePromotedP0Wave =>
            LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedP0Wave, ref s_classSyncObject, () => new TransactionStatePromotedP0Wave());

        protected static TransactionStatePromotedCommitting TransactionStatePromotedCommitting =>
            LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedCommitting, ref s_classSyncObject, () => new TransactionStatePromotedCommitting());

        protected static TransactionStatePromotedPhase0 TransactionStatePromotedPhase0 =>
            LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedPhase0, ref s_classSyncObject, () => new TransactionStatePromotedPhase0());

        protected static TransactionStatePromotedPhase1 TransactionStatePromotedPhase1 =>
            LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedPhase1, ref s_classSyncObject, () => new TransactionStatePromotedPhase1());

        protected static TransactionStatePromotedP0Aborting TransactionStatePromotedP0Aborting =>
            LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedP0Aborting, ref s_classSyncObject, () => new TransactionStatePromotedP0Aborting());

        protected static TransactionStatePromotedP1Aborting TransactionStatePromotedP1Aborting =>
            LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedP1Aborting, ref s_classSyncObject, () => new TransactionStatePromotedP1Aborting());

        protected static TransactionStatePromotedAborted TransactionStatePromotedAborted =>
            LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedAborted, ref s_classSyncObject, () => new TransactionStatePromotedAborted());

        protected static TransactionStatePromotedCommitted TransactionStatePromotedCommitted =>
            LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedCommitted, ref s_classSyncObject, () => new TransactionStatePromotedCommitted());

        protected static TransactionStatePromotedIndoubt TransactionStatePromotedIndoubt =>
            LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedIndoubt, ref s_classSyncObject, () => new TransactionStatePromotedIndoubt());

        protected static TransactionStateDelegated TransactionStateDelegated =>
            LazyInitializer.EnsureInitialized(ref s_transactionStateDelegated, ref s_classSyncObject, () => new TransactionStateDelegated());

        internal static TransactionStateDelegatedSubordinate TransactionStateDelegatedSubordinate =>
            LazyInitializer.EnsureInitialized(ref s_transactionStateDelegatedSubordinate, ref s_classSyncObject, () => new TransactionStateDelegatedSubordinate());

        protected static TransactionStateDelegatedP0Wave TransactionStateDelegatedP0Wave =>
            LazyInitializer.EnsureInitialized(ref s_transactionStateDelegatedP0Wave, ref s_classSyncObject, () => new TransactionStateDelegatedP0Wave());

        protected static TransactionStateDelegatedCommitting TransactionStateDelegatedCommitting =>
            LazyInitializer.EnsureInitialized(ref s_transactionStateDelegatedCommitting, ref s_classSyncObject, () => new TransactionStateDelegatedCommitting());

        protected static TransactionStateDelegatedAborting TransactionStateDelegatedAborting =>
            LazyInitializer.EnsureInitialized(ref s_transactionStateDelegatedAborting, ref s_classSyncObject, () => new TransactionStateDelegatedAborting());

        protected static TransactionStateDelegatedNonMSDTC TransactionStateDelegatedNonMSDTC =>
            LazyInitializer.EnsureInitialized(ref s_transactionStateDelegatedNonMSDTC, ref s_classSyncObject, () => new TransactionStateDelegatedNonMSDTC());

        protected static TransactionStatePromotedNonMSDTCPhase0 TransactionStatePromotedNonMSDTCPhase0 =>
            LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedNonMSDTCPhase0, ref s_classSyncObject, () => new TransactionStatePromotedNonMSDTCPhase0());

        protected static TransactionStatePromotedNonMSDTCVolatilePhase1 TransactionStatePromotedNonMSDTCVolatilePhase1 =>
            LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedNonMSDTCVolatilePhase1, ref s_classSyncObject, () => new TransactionStatePromotedNonMSDTCVolatilePhase1());

        protected static TransactionStatePromotedNonMSDTCSinglePhaseCommit TransactionStatePromotedNonMSDTCSinglePhaseCommit =>
            LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedNonMSDTCSinglePhaseCommit, ref s_classSyncObject, () => new TransactionStatePromotedNonMSDTCSinglePhaseCommit());

        protected static TransactionStatePromotedNonMSDTCAborted TransactionStatePromotedNonMSDTCAborted =>
            LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedNonMSDTCAborted, ref s_classSyncObject, () => new TransactionStatePromotedNonMSDTCAborted());

        protected static TransactionStatePromotedNonMSDTCCommitted TransactionStatePromotedNonMSDTCCommitted =>
            LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedNonMSDTCCommitted, ref s_classSyncObject, () => new TransactionStatePromotedNonMSDTCCommitted());

        protected static TransactionStatePromotedNonMSDTCIndoubt TransactionStatePromotedNonMSDTCIndoubt =>
            LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedNonMSDTCIndoubt, ref s_classSyncObject, () => new TransactionStatePromotedNonMSDTCIndoubt());

        internal void CommonEnterState(InternalTransaction tx)
        {
            Debug.Assert(tx.State != this, "Changing to the same state.");
            tx.State = this;

#if DEBUG
            tx._stateHistory[tx._currentStateHist] = this;
            if (++tx._currentStateHist > InternalTransaction.MaxStateHist)
            {
                tx._currentStateHist = 0;
            }
#endif
        }

        // Every state must override EnterState
        internal abstract void EnterState(InternalTransaction tx);

        internal virtual void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback? asyncCallback, object? asyncState)
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal virtual void EndCommit(InternalTransaction tx)
        {
            Debug.Fail($"Invalid Event for State; Current State: {GetType()}");

            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal virtual void Rollback(InternalTransaction tx, Exception? e)
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal virtual Enlistment EnlistDurable(
            InternalTransaction tx,
            Guid resourceManagerIdentifier,
            IEnlistmentNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction
            )
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal virtual Enlistment EnlistDurable(
            InternalTransaction tx,
            Guid resourceManagerIdentifier,
            ISinglePhaseNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction
            )
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal virtual Enlistment EnlistVolatile(
            InternalTransaction tx,
            IEnlistmentNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction
            )
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal virtual Enlistment EnlistVolatile(
            InternalTransaction tx,
            ISinglePhaseNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction
            )
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal virtual void CheckForFinishedTransaction(InternalTransaction tx)
        {
            // Aborted & InDoubt states should throw exceptions.
        }

        // If a specific state does not have a story for identifiers then
        // it simply gets a guid.  This would be to handle cases like aborted
        // and committed where the transaction has not been promoted and
        // cannot be promoted so it doesn't matter what guid is returned.
        //
        // This leaves two specific sets of states that MUST override this...
        // 1) Any state where the transaction could be promoted.
        // 2) Any state where the transaction is already promoted.
        internal virtual Guid get_Identifier(InternalTransaction tx)
        {
            return Guid.Empty;
        }

        // Every state derived from the base must override status
        internal abstract TransactionStatus get_Status(InternalTransaction tx);

        internal virtual void AddOutcomeRegistrant(InternalTransaction tx, TransactionCompletedEventHandler? transactionCompletedDelegate)
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal virtual void GetObjectData(InternalTransaction tx, SerializationInfo serializationInfo, StreamingContext context)
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal virtual bool EnlistPromotableSinglePhase(
            InternalTransaction tx,
            IPromotableSinglePhaseNotification promotableSinglePhaseNotification,
            Transaction atomicTransaction,
            Guid promoterType
            )
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal virtual void CompleteBlockingClone(InternalTransaction tx)
        {
        }


        internal virtual void CompleteAbortingClone(InternalTransaction tx)
        {
        }

        internal virtual void CreateBlockingClone(InternalTransaction tx)
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal virtual void CreateAbortingClone(InternalTransaction tx)
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal virtual void ChangeStateTransactionAborted(InternalTransaction tx, Exception? e)
        {
            Debug.Fail($"Invalid Event for State; Current State: {GetType()}");
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionExceptionTrace(TransactionExceptionType.InvalidOperationException, tx?.TransactionTraceId.TransactionIdentifier ?? string.Empty, e.ToString());
            }

            throw new InvalidOperationException();
        }

        internal virtual void ChangeStateTransactionCommitted(InternalTransaction tx)
        {
            Debug.Fail($"Invalid Event for State; Current State: {GetType()}");
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionExceptionTrace(TransactionExceptionType.InvalidOperationException, tx?.TransactionTraceId.TransactionIdentifier ?? string.Empty, string.Empty);
            }

            throw new InvalidOperationException();
        }

        internal virtual void InDoubtFromEnlistment(InternalTransaction tx)
        {
            Debug.Fail($"Invalid Event for State; Current State: {GetType()}");
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionExceptionTrace(TransactionExceptionType.InvalidOperationException, tx?.TransactionTraceId.TransactionIdentifier ?? string.Empty, string.Empty);
            }

            throw new InvalidOperationException();
        }

        internal virtual void ChangeStatePromotedAborted(InternalTransaction tx)
        {
            Debug.Fail($"Invalid Event for State; Current State: {GetType()}");
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionExceptionTrace(TransactionExceptionType.InvalidOperationException, tx?.TransactionTraceId.TransactionIdentifier ?? string.Empty, string.Empty);
            }

            throw new InvalidOperationException();
        }

        internal virtual void ChangeStatePromotedCommitted(InternalTransaction tx)
        {
            Debug.Fail($"Invalid Event for State; Current State: {GetType()}");
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionExceptionTrace(TransactionExceptionType.InvalidOperationException, tx?.TransactionTraceId.TransactionIdentifier ?? string.Empty, string.Empty);
            }

            throw new InvalidOperationException();
        }

        internal virtual void InDoubtFromDtc(InternalTransaction tx)
        {
            Debug.Fail($"Invalid Event for State; Current State: {GetType()}");
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionExceptionTrace(TransactionExceptionType.InvalidOperationException, tx?.TransactionTraceId.TransactionIdentifier ?? string.Empty, string.Empty);
            }

            throw new InvalidOperationException();
        }

        internal virtual void ChangeStatePromotedPhase0(InternalTransaction tx)
        {
            Debug.Fail($"Invalid Event for State; Current State: {GetType()}");
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionExceptionTrace(TransactionExceptionType.InvalidOperationException, tx?.TransactionTraceId.TransactionIdentifier ?? string.Empty, string.Empty);
            }

            throw new InvalidOperationException();
        }

        internal virtual void ChangeStatePromotedPhase1(InternalTransaction tx)
        {
            Debug.Fail($"Invalid Event for State; Current State: {GetType()}");
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionExceptionTrace(TransactionExceptionType.InvalidOperationException, tx?.TransactionTraceId.TransactionIdentifier ?? string.Empty, string.Empty);
            }

            throw new InvalidOperationException();
        }

        internal virtual void ChangeStateAbortedDuringPromotion(InternalTransaction tx)
        {
            Debug.Fail($"Invalid Event for State; Current State: {GetType()}");
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionExceptionTrace(TransactionExceptionType.InvalidOperationException, tx?.TransactionTraceId.TransactionIdentifier ?? string.Empty, string.Empty);
            }

            throw new InvalidOperationException();
        }

        internal virtual void Timeout(InternalTransaction tx)
        {
        }

        internal virtual void Phase0VolatilePrepareDone(InternalTransaction tx)
        {
            Debug.Fail($"Invalid Event for State; Current State: {GetType()}");
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal virtual void Phase1VolatilePrepareDone(InternalTransaction tx)
        {
            Debug.Fail($"Invalid Event for State; Current State: {GetType()}");
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal virtual void RestartCommitIfNeeded(InternalTransaction tx)
        {
            Debug.Fail($"Invalid Event for State; Current State: {GetType()}");
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionExceptionTrace(TransactionExceptionType.InvalidOperationException, tx?.TransactionTraceId.TransactionIdentifier ?? string.Empty, string.Empty);
            }

            throw new InvalidOperationException();
        }

        internal virtual bool ContinuePhase0Prepares()
        {
            return false;
        }

        internal virtual bool ContinuePhase1Prepares()
        {
            return false;
        }

        internal virtual void Promote(InternalTransaction tx)
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal virtual byte[] PromotedToken(InternalTransaction tx)
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal virtual Enlistment PromoteAndEnlistDurable(
            InternalTransaction tx,
            Guid resourceManagerIdentifier,
            IPromotableSinglePhaseNotification promotableNotification,
            ISinglePhaseNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction)
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal virtual void SetDistributedTransactionId(InternalTransaction tx,
                    IPromotableSinglePhaseNotification promotableNotification,
                    Guid distributedTransactionIdentifier)
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal virtual void DisposeRoot(InternalTransaction tx)
        {
        }

        internal virtual bool IsCompleted(InternalTransaction tx)
        {
            tx._needPulse = true;

            return false;
        }

        protected void AddVolatileEnlistment(ref VolatileEnlistmentSet enlistments, Enlistment enlistment)
        {
            // Grow the enlistment array if necessary.
            if (enlistments._volatileEnlistmentCount == enlistments._volatileEnlistmentSize)
            {
                InternalEnlistment[] newEnlistments =
                    new InternalEnlistment[enlistments._volatileEnlistmentSize + InternalTransaction.VolatileArrayIncrement];

                if (enlistments._volatileEnlistmentSize > 0)
                {
                    Array.Copy(
                        enlistments._volatileEnlistments,
                        0,
                        newEnlistments,
                        0,
                        enlistments._volatileEnlistmentSize
                        );
                }

                enlistments._volatileEnlistmentSize += InternalTransaction.VolatileArrayIncrement;
                enlistments._volatileEnlistments = newEnlistments;
            }

            // Add a new element to the end of the list
            enlistments._volatileEnlistments[enlistments._volatileEnlistmentCount] = enlistment.InternalEnlistment;
            enlistments._volatileEnlistmentCount++;

            // Make it's state active.
            VolatileEnlistmentState.VolatileEnlistmentActive.EnterState(
                enlistments._volatileEnlistments[enlistments._volatileEnlistmentCount - 1]);
        }
    }


    // ActiveStates
    //
    // All states for which the transaction is not done should derive from this state.
    internal abstract class ActiveStates : TransactionState
    {
        internal override TransactionStatus get_Status(InternalTransaction tx)
        {
            return TransactionStatus.Active;
        }

        internal override void AddOutcomeRegistrant(InternalTransaction tx, TransactionCompletedEventHandler? transactionCompletedDelegate)
        {
            tx._transactionCompletedDelegate = (TransactionCompletedEventHandler?)
                System.Delegate.Combine(tx._transactionCompletedDelegate, transactionCompletedDelegate);
        }
    }


    // EnlistableStates
    //
    // States for which it is ok to enlist.
    internal abstract class EnlistableStates : ActiveStates
    {
        internal override Enlistment EnlistDurable(
            InternalTransaction tx,
            Guid resourceManagerIdentifier,
            IEnlistmentNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction)
        {
            tx.ThrowIfPromoterTypeIsNotMSDTC();

            // Can't support an enlistment that dosn't support SPC
            tx._promoteState.EnterState(tx);
            // Note that just because we did an EnterState above does not mean that the state will be
            // the same when the next method is called.
            return tx.State!.EnlistDurable(tx, resourceManagerIdentifier, enlistmentNotification, enlistmentOptions, atomicTransaction);
        }

        internal override Enlistment EnlistDurable(
            InternalTransaction tx,
            Guid resourceManagerIdentifier,
            ISinglePhaseNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction)
        {
            tx.ThrowIfPromoterTypeIsNotMSDTC();

            if (tx._durableEnlistment != null || (enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0)
            {
                // These circumstances cause promotion
                tx._promoteState.EnterState(tx);
                return tx.State!.EnlistDurable(tx, resourceManagerIdentifier, enlistmentNotification, enlistmentOptions, atomicTransaction);
            }

            // Create a durable enlistment
            Enlistment en = new Enlistment(resourceManagerIdentifier, tx, enlistmentNotification, enlistmentNotification, atomicTransaction);
            tx._durableEnlistment = en.InternalEnlistment;
            DurableEnlistmentState.DurableEnlistmentActive.EnterState(tx._durableEnlistment);

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionstateEnlist(tx._durableEnlistment.EnlistmentTraceId, EnlistmentType.Durable, EnlistmentOptions.None);
            }

            return en;
        }

        internal override void Timeout(InternalTransaction tx)
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionTimeout(tx.TransactionTraceId);
            }

            TimeoutException e = new TimeoutException(SR.TraceTransactionTimeout);
            Rollback(tx, e);
        }

        internal override void GetObjectData(InternalTransaction tx, SerializationInfo serializationInfo, StreamingContext context)
        {
            // This is not allowed if the transaction's PromoterType is not MSDTC.
            tx.ThrowIfPromoterTypeIsNotMSDTC();

            // Promote the transaction.
            tx._promoteState.EnterState(tx);

            // Forward this call
            tx.State!.GetObjectData(tx, serializationInfo, context);
        }

        internal override void CompleteBlockingClone(InternalTransaction tx)
        {
            // A blocking clone simulates a phase 0 volatile

            // decrement the number of dependentClones
            tx._phase0Volatiles._dependentClones--;
            Debug.Assert(tx._phase0Volatiles._dependentClones >= 0);

            // Make certain we increment the right list.
            Debug.Assert(tx._phase0Volatiles._preparedVolatileEnlistments <=
                tx._phase0Volatiles._volatileEnlistmentCount + tx._phase0Volatiles._dependentClones);

            // Check to see if all of the volatile enlistments are done.
            if (tx._phase0Volatiles._preparedVolatileEnlistments ==
                tx._phase0VolatileWaveCount + tx._phase0Volatiles._dependentClones)
            {
                tx.State!.Phase0VolatilePrepareDone(tx);
            }
        }

        internal override void CompleteAbortingClone(InternalTransaction tx)
        {
            // A blocking clone simulates a phase 1 volatile
            //
            // Unlike a blocking clone however the aborting clones need to be accounted
            // for specifically.  So when one is complete remove it from the list.
            tx._phase1Volatiles._dependentClones--;
            Debug.Assert(tx._phase1Volatiles._dependentClones >= 0);
        }

        internal override void CreateBlockingClone(InternalTransaction tx)
        {
            // A blocking clone simulates a phase 0 volatile
            tx._phase0Volatiles._dependentClones++;
        }

        internal override void CreateAbortingClone(InternalTransaction tx)
        {
            // An aborting clone simulates a phase 1 volatile
            tx._phase1Volatiles._dependentClones++;
        }

        internal override void Promote(InternalTransaction tx)
        {
            tx._promoteState.EnterState(tx);
            tx.State!.CheckForFinishedTransaction(tx);
        }

        internal override byte[] PromotedToken(InternalTransaction tx)
        {
            if (tx.promotedToken == null)
            {
                tx._promoteState.EnterState(tx);
                tx.State!.CheckForFinishedTransaction(tx);
            }
            Debug.Assert(tx.promotedToken != null);
            return tx.promotedToken;
        }
    }



    // TransactionStateActive
    //
    // Transaction state before commit has been called
    internal class TransactionStateActive : EnlistableStates
    {
        internal override void EnterState(InternalTransaction tx)
        {
            // Set the transaction state
            CommonEnterState(tx);

            // Yeah it's active.
        }

        internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback? asyncCallback, object? asyncState)
        {
            // Store the given values
            tx._asyncCommit = asyncCommit;
            tx._asyncCallback = asyncCallback;
            tx._asyncState = asyncState;

            // Start the process for commit.
            TransactionStatePhase0.EnterState(tx);
        }

        internal override void Rollback(InternalTransaction tx, Exception? e)
        {
            // Start the process for abort.  From the active state we can transition directly
            // to the aborted state.

            if (tx._innerException == null)
            {
                tx._innerException = e;
            }

            TransactionStateAborted.EnterState(tx);
        }

        internal override Enlistment EnlistVolatile(
            InternalTransaction tx,
            IEnlistmentNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction
            )
        {
            Enlistment enlistment = new Enlistment(tx, enlistmentNotification, null, atomicTransaction, enlistmentOptions);
            if ((enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0)
            {
                AddVolatileEnlistment(ref tx._phase0Volatiles, enlistment);
            }
            else
            {
                AddVolatileEnlistment(ref tx._phase1Volatiles, enlistment);
            }

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionstateEnlist(enlistment.InternalEnlistment.EnlistmentTraceId, EnlistmentType.Volatile, enlistmentOptions);
            }

            return enlistment;
        }

        internal override Enlistment EnlistVolatile(
            InternalTransaction tx,
            ISinglePhaseNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction
            )
        {
            Enlistment enlistment = new Enlistment(tx, enlistmentNotification, enlistmentNotification, atomicTransaction, enlistmentOptions);
            if ((enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0)
            {
                AddVolatileEnlistment(ref tx._phase0Volatiles, enlistment);
            }
            else
            {
                AddVolatileEnlistment(ref tx._phase1Volatiles, enlistment);
            }

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionstateEnlist(enlistment.InternalEnlistment.EnlistmentTraceId, EnlistmentType.Volatile, enlistmentOptions);
            }

            return enlistment;
        }

        internal override bool EnlistPromotableSinglePhase(
            InternalTransaction tx, IPromotableSinglePhaseNotification promotableSinglePhaseNotification,
            Transaction atomicTransaction,
            Guid promoterType
            )
        {
            // Delegation will fail if there is a durable enlistment
            if (tx._durableEnlistment != null)
            {
                return false;
            }

            TransactionStatePSPEOperation.PSPEInitialize(tx, promotableSinglePhaseNotification, promoterType);

            // Create a durable enlistment.
            Enlistment en = new Enlistment(tx, promotableSinglePhaseNotification, atomicTransaction);
            tx._durableEnlistment = en.InternalEnlistment;
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionstateEnlist(tx._durableEnlistment.EnlistmentTraceId, EnlistmentType.PromotableSinglePhase, EnlistmentOptions.None);
            }

            // Specify the promoter for the transaction.
            tx._promoter = promotableSinglePhaseNotification;

            // Change the state that the transaction will promote to.  Normally this would be simply
            // be TransactionStatePromoted.  However it now needs to promote to a delegated state.
            // If the PromoterType is NOT TransactionInterop.PromoterTypeDtc, then the promoteState needs
            // to be TransactionStateDelegatedNonMSDTC.
            // tx.PromoterType was set in PSPEInitialize.
            Debug.Assert(tx._promoterType != Guid.Empty, "InternalTransaction.PromoterType was not set in PSPEInitialize");
            if (tx._promoterType == TransactionInterop.PromoterTypeDtc)
            {
                tx._promoteState = TransactionStateDelegated;
            }
            else
            {
                tx._promoteState = TransactionStateDelegatedNonMSDTC;
            }

            // Pud the enlistment in an active state
            DurableEnlistmentState.DurableEnlistmentActive.EnterState(tx._durableEnlistment);

            // Hand back the enlistment.
            return true;
        }


        // Volatile prepare is done for
        internal override void Phase0VolatilePrepareDone(InternalTransaction tx)
        {
            // Ignore this event at the moment.  It can be checked again in Phase0
        }

        internal override void Phase1VolatilePrepareDone(InternalTransaction tx)
        {
            // Ignore this event at the moment.  It can be checked again in Phase1
        }

        internal override void DisposeRoot(InternalTransaction tx)
        {
            tx.State!.Rollback(tx, null);
        }
    }


    // TransactionStateSubordinateActive
    //
    // This is a transaction that is a very basic subordinate to some external TM.
    internal sealed class TransactionStateSubordinateActive : TransactionStateActive
    {
        // Every state must override EnterState
        internal override void EnterState(InternalTransaction tx)
        {
            // Set the transaction state
            CommonEnterState(tx);

            Debug.Assert(tx._promoter != null, "Transaction Promoter is Null entering SubordinateActive");
        }

        internal override void Rollback(InternalTransaction tx, Exception? e)
        {
            // Start the process for abort.  From the active state we can transition directly
            // to the aborted state.

            if (tx._innerException == null)
            {
                tx._innerException = e;
            }

            Debug.Assert(tx._promoter != null);
            ((ISimpleTransactionSuperior)tx._promoter).Rollback();
            TransactionStateAborted.EnterState(tx);
        }

        internal override Enlistment EnlistVolatile(
            InternalTransaction tx,
            IEnlistmentNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction
            )
        {
            tx._promoteState.EnterState(tx);
            return tx.State!.EnlistVolatile(tx, enlistmentNotification, enlistmentOptions, atomicTransaction);
        }

        internal override Enlistment EnlistVolatile(
            InternalTransaction tx,
            ISinglePhaseNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction
            )
        {
            tx._promoteState.EnterState(tx);
            return tx.State!.EnlistVolatile(tx, enlistmentNotification, enlistmentOptions, atomicTransaction);
        }

        // Every state derived from the base must override status
        internal override TransactionStatus get_Status(InternalTransaction tx)
        {
            tx._promoteState.EnterState(tx);
            return tx.State!.get_Status(tx);
        }

        internal override void AddOutcomeRegistrant(InternalTransaction tx, TransactionCompletedEventHandler? transactionCompletedDelegate)
        {
            tx._promoteState.EnterState(tx);
            tx.State!.AddOutcomeRegistrant(tx, transactionCompletedDelegate);
        }

        internal override bool EnlistPromotableSinglePhase(
            InternalTransaction tx,
            IPromotableSinglePhaseNotification promotableSinglePhaseNotification,
            Transaction atomicTransaction,
            Guid promoterType
            )
        {
            return false;
        }

        internal override void CreateBlockingClone(InternalTransaction tx)
        {
            tx._promoteState.EnterState(tx);
            tx.State!.CreateBlockingClone(tx);
        }


        internal override void CreateAbortingClone(InternalTransaction tx)
        {
            tx._promoteState.EnterState(tx);
            tx.State!.CreateAbortingClone(tx);
        }
    }


    // TransactionStatePhase0
    //
    // A transaction that is in the beginning stage of committing.
    internal sealed class TransactionStatePhase0 : EnlistableStates
    {
        internal override void EnterState(InternalTransaction tx)
        {
            // Set the transaction state
            CommonEnterState(tx);

            // Get a copy of the current volatile enlistment count before entering this loop so that other
            // threads don't affect the operation of this loop.
            int volatileCount = tx._phase0Volatiles._volatileEnlistmentCount;
            int dependentCount = tx._phase0Volatiles._dependentClones;

            // Store the number of phase0 volatiles for this wave.
            tx._phase0VolatileWaveCount = volatileCount;

            // Check for volatile enlistments
            if (tx._phase0Volatiles._preparedVolatileEnlistments < volatileCount + dependentCount)
            {
                // Broadcast prepare to the phase 0 enlistments
                for (int i = 0; i < volatileCount; i++)
                {
                    tx._phase0Volatiles._volatileEnlistments[i]._twoPhaseState!.ChangeStatePreparing(tx._phase0Volatiles._volatileEnlistments[i]);
                    if (!tx.State!.ContinuePhase0Prepares())
                    {
                        break;
                    }
                }
            }
            else
            {
                // No volatile enlistments.  Start phase 1.
                TransactionStateVolatilePhase1.EnterState(tx);
            }
        }

        internal override Enlistment EnlistDurable(
            InternalTransaction tx,
            Guid resourceManagerIdentifier,
            IEnlistmentNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction
            )
        {
            tx.ThrowIfPromoterTypeIsNotMSDTC();

            Enlistment en = base.EnlistDurable(tx, resourceManagerIdentifier, enlistmentNotification,
                enlistmentOptions, atomicTransaction);

            // Calling durable enlist in Phase0 may cause the transaction to promote.  Leverage the promoted
            tx.State!.RestartCommitIfNeeded(tx);
            return en;
        }

        internal override Enlistment EnlistDurable(
            InternalTransaction tx,
            Guid resourceManagerIdentifier,
            ISinglePhaseNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction
            )
        {
            tx.ThrowIfPromoterTypeIsNotMSDTC();

            Enlistment en = base.EnlistDurable(tx, resourceManagerIdentifier, enlistmentNotification,
                enlistmentOptions, atomicTransaction);

            // Calling durable enlist in Phase0 may cause the transaction to promote.  Leverage the promoted
            tx.State!.RestartCommitIfNeeded(tx);
            return en;
        }

        internal override Enlistment EnlistVolatile(
            InternalTransaction tx,
            IEnlistmentNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction
            )
        {
            Enlistment enlistment = new Enlistment(tx, enlistmentNotification, null, atomicTransaction, enlistmentOptions);
            if ((enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0)
            {
                AddVolatileEnlistment(ref tx._phase0Volatiles, enlistment);
            }
            else
            {
                AddVolatileEnlistment(ref tx._phase1Volatiles, enlistment);
            }

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionstateEnlist(enlistment.InternalEnlistment.EnlistmentTraceId, EnlistmentType.Volatile, enlistmentOptions);
            }

            return enlistment;
        }

        internal override Enlistment EnlistVolatile(
            InternalTransaction tx,
            ISinglePhaseNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction
            )
        {
            Enlistment enlistment = new Enlistment(tx, enlistmentNotification, enlistmentNotification, atomicTransaction, enlistmentOptions);

            if ((enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0)
            {
                AddVolatileEnlistment(ref tx._phase0Volatiles, enlistment);
            }
            else
            {
                AddVolatileEnlistment(ref tx._phase1Volatiles, enlistment);
            }

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionstateEnlist(enlistment.InternalEnlistment.EnlistmentTraceId, EnlistmentType.Volatile, enlistmentOptions);
            }

            return enlistment;
        }

        internal override void Rollback(InternalTransaction tx, Exception? e)
        {
            ChangeStateTransactionAborted(tx, e);
        }

        // Support PSPE enlistment during Phase0 prepare notification.

        internal override bool EnlistPromotableSinglePhase(
            InternalTransaction tx,
            IPromotableSinglePhaseNotification promotableSinglePhaseNotification,
            Transaction atomicTransaction,
            Guid promoterType
            )
        {
            // Delegation will fail if there is a durable enlistment
            if (tx._durableEnlistment != null)
            {
                return false;
            }

            // Initialize PSPE Operation and call initialize on IPromotableSinglePhaseNotification
            TransactionStatePSPEOperation.Phase0PSPEInitialize(tx, promotableSinglePhaseNotification, promoterType);

            // Create a durable enlistment.
            Enlistment en = new Enlistment(tx, promotableSinglePhaseNotification, atomicTransaction);
            tx._durableEnlistment = en.InternalEnlistment;
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionstateEnlist(tx._durableEnlistment.EnlistmentTraceId, EnlistmentType.PromotableSinglePhase, EnlistmentOptions.None);
            }

            // Specify the promoter for the transaction.
            tx._promoter = promotableSinglePhaseNotification;

            // Change the state that the transaction will promote to.  Normally this would be simply
            // be TransactionStatePromoted.  However it now needs to promote to a delegated state.
            // If the PromoterType is NOT TransactionInterop.PromoterTypeDtc, then the promoteState needs
            // to be TransactionStateDelegatedNonMSDTC.
            // tx.PromoterType was set in Phase0PSPEInitialize.
            Debug.Assert(tx._promoterType != Guid.Empty, "InternalTransaction.PromoterType was not set in Phase0PSPEInitialize");
            if (tx._promoterType == TransactionInterop.PromoterTypeDtc)
            {
                tx._promoteState = TransactionStateDelegated;
            }
            else
            {
                tx._promoteState = TransactionStateDelegatedNonMSDTC;
            }

            // Put the enlistment in an active state
            DurableEnlistmentState.DurableEnlistmentActive.EnterState(tx._durableEnlistment);

            // Hand back the enlistment.
            return true;
        }

        // Volatile prepare is done for
        internal override void Phase0VolatilePrepareDone(InternalTransaction tx)
        {
            // Check to see if any Phase0Volatiles have been added in Phase0.
            // If so go through the list again.

            // Get a copy of the current volatile enlistment count before entering this loop so that other
            // threads don't affect the operation of this loop.
            int volatileCount = tx._phase0Volatiles._volatileEnlistmentCount;
            int dependentCount = tx._phase0Volatiles._dependentClones;

            // Store the number of phase0 volatiles for this wave.
            tx._phase0VolatileWaveCount = volatileCount;

            // Check for volatile enlistments
            if (tx._phase0Volatiles._preparedVolatileEnlistments < volatileCount + dependentCount)
            {
                // Broadcast prepare to the phase 0 enlistments
                for (int i = 0; i < volatileCount; i++)
                {
                    tx._phase0Volatiles._volatileEnlistments[i]._twoPhaseState!.ChangeStatePreparing(tx._phase0Volatiles._volatileEnlistments[i]);
                    if (!tx.State!.ContinuePhase0Prepares())
                    {
                        break;
                    }
                }
            }
            else
            {
                // No volatile enlistments.  Start phase 1.
                TransactionStateVolatilePhase1.EnterState(tx);
            }
        }

        internal override void Phase1VolatilePrepareDone(InternalTransaction tx)
        {
            // Ignore this for now it can be checked again in Phase 1
        }

        internal override void RestartCommitIfNeeded(InternalTransaction tx)
        {
            // Commit does not need to be restarted
        }

        internal override bool ContinuePhase0Prepares()
        {
            return true;
        }

        internal override void Promote(InternalTransaction tx)
        {
            tx._promoteState.EnterState(tx);
            tx.State!.CheckForFinishedTransaction(tx);
            tx.State.RestartCommitIfNeeded(tx);
        }

        internal override void ChangeStateTransactionAborted(InternalTransaction tx, Exception? e)
        {
            if (tx._innerException == null)
            {
                tx._innerException = e;
            }

            TransactionStateAborted.EnterState(tx);
        }

        internal override void GetObjectData(InternalTransaction tx, SerializationInfo serializationInfo, StreamingContext context)
        {
            // This is not allowed if the transaction's PromoterType is not MSDTC.
            tx.ThrowIfPromoterTypeIsNotMSDTC();

            // Promote the transaction.
            tx._promoteState.EnterState(tx);

            // Forward this call
            tx.State!.GetObjectData(tx, serializationInfo, context);

            // Restart the commit process.
            tx.State.RestartCommitIfNeeded(tx);
        }
    }

    // TransactionStateVolatilePhase1
    //
    // Represents the transaction state during phase 1 preparing volatile enlistments
    internal sealed class TransactionStateVolatilePhase1 : ActiveStates
    {
        internal override void EnterState(InternalTransaction tx)
        {
            // Set the transaction state
            CommonEnterState(tx);

            Debug.Assert(tx._committableTransaction != null);
            // Mark the committable transaction as complete.
            tx._committableTransaction._complete = true;

            // If at this point there are phase1 dependent clones abort the transaction
            if (tx._phase1Volatiles._dependentClones != 0)
            {
                TransactionStateAborted.EnterState(tx);
                return;
            }

            if (tx._phase1Volatiles._volatileEnlistmentCount == 1 && tx._durableEnlistment == null
                && tx._phase1Volatiles._volatileEnlistments[0].SinglePhaseNotification != null)
            {
                // This is really a case of SPC for volatiles
                TransactionStateVolatileSPC.EnterState(tx);
            }
            else if (tx._phase1Volatiles._volatileEnlistmentCount > 0)
            {
                // Broadcast prepare to the phase 0 enlistments
                for (int i = 0; i < tx._phase1Volatiles._volatileEnlistmentCount; i++)
                {
                    tx._phase1Volatiles._volatileEnlistments[i]._twoPhaseState!.ChangeStatePreparing(tx._phase1Volatiles._volatileEnlistments[i]);
                    if (!tx.State!.ContinuePhase1Prepares())
                    {
                        break;
                    }
                }
            }
            else
            {
                // No volatile phase 1 enlistments.  Start phase durable SPC.
                TransactionStateSPC.EnterState(tx);
            }
        }

        internal override void Rollback(InternalTransaction tx, Exception? e)
        {
            ChangeStateTransactionAborted(tx, e);
        }

        internal override void ChangeStateTransactionAborted(InternalTransaction tx, Exception? e)
        {
            if (tx._innerException == null)
            {
                tx._innerException = e;
            }

            TransactionStateAborted.EnterState(tx);
        }

        // Volatile prepare is done for
        internal override void Phase1VolatilePrepareDone(InternalTransaction tx)
        {
            TransactionStateSPC.EnterState(tx);
        }

        internal override bool ContinuePhase1Prepares()
        {
            return true;
        }

        internal override void Timeout(InternalTransaction tx)
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionTimeout(tx.TransactionTraceId);
            }

            TimeoutException e = new TimeoutException(SR.TraceTransactionTimeout);
            Rollback(tx, e);
        }
    }


    // TransactionStateVolatileSPC
    //
    // Represents the transaction state during phase 1 when issuing SPC to a volatile enlistment
    internal sealed class TransactionStateVolatileSPC : ActiveStates
    {
        internal override void EnterState(InternalTransaction tx)
        {
            // Set the transaction state
            CommonEnterState(tx);

            Debug.Assert(tx._phase1Volatiles._volatileEnlistmentCount == 1,
                "There must be exactly 1 phase 1 volatile enlistment for TransactionStateVolatileSPC");

            tx._phase1Volatiles._volatileEnlistments[0]._twoPhaseState!.ChangeStateSinglePhaseCommit(
                tx._phase1Volatiles._volatileEnlistments[0]);
        }

        internal override void ChangeStateTransactionCommitted(InternalTransaction tx)
        {
            // The durable enlistment must have committed.  Go to the committed state.
            TransactionStateCommitted.EnterState(tx);
        }

        internal override void InDoubtFromEnlistment(InternalTransaction tx)
        {
            // The transaction is indoubt
            TransactionStateInDoubt.EnterState(tx);
        }

        internal override void ChangeStateTransactionAborted(InternalTransaction tx, Exception? e)
        {
            if (tx._innerException == null)
            {
                tx._innerException = e;
            }

            // The durable enlistment must have aborted.  Go to the aborted state.
            TransactionStateAborted.EnterState(tx);
        }
    }

    // TransactionStateSPC
    //
    // Represents the transaction state during phase 1
    internal sealed class TransactionStateSPC : ActiveStates
    {
        internal override void EnterState(InternalTransaction tx)
        {
            // Set the transaction state
            CommonEnterState(tx);

            // Check for a durable enlistment
            if (tx._durableEnlistment != null)
            {
                // Send SPC to the durable enlistment
                tx._durableEnlistment.State.ChangeStateCommitting(tx._durableEnlistment);
            }
            else
            {
                // No durable enlistments.  Go to the committed state.
                TransactionStateCommitted.EnterState(tx);
            }
        }

        internal override void ChangeStateTransactionCommitted(InternalTransaction tx)
        {
            // The durable enlistment must have committed.  Go to the committed state.
            TransactionStateCommitted.EnterState(tx);
        }

        internal override void InDoubtFromEnlistment(InternalTransaction tx)
        {
            // The transaction is indoubt
            TransactionStateInDoubt.EnterState(tx);
        }

        internal override void ChangeStateTransactionAborted(InternalTransaction tx, Exception? e)
        {
            if (tx._innerException == null)
            {
                tx._innerException = e;
            }

            // The durable enlistment must have aborted.  Go to the aborted state.
            TransactionStateAborted.EnterState(tx);
        }
    }


    // TransactionStateEnded
    //
    // This state indicates that the transaction is in some form of ended state.
    internal abstract class TransactionStateEnded : TransactionState
    {
        internal override void EnterState(InternalTransaction tx)
        {
            if (tx._needPulse)
            {
                Monitor.Pulse(tx);
            }
        }

        internal override void AddOutcomeRegistrant(InternalTransaction tx, TransactionCompletedEventHandler? transactionCompletedDelegate)
        {
            if (transactionCompletedDelegate != null)
            {
                TransactionEventArgs args = new TransactionEventArgs();
                args._transaction = tx._outcomeSource.InternalClone();
                transactionCompletedDelegate(args._transaction, args);
            }
        }

        internal override bool IsCompleted(InternalTransaction tx)
        {
            return true;
        }
    }


    // TransactionStateAborted
    //
    // The transaction has been aborted.  Abort is itempotent and can be called again but any
    // other operations on the transaction should fail.
    internal sealed class TransactionStateAborted : TransactionStateEnded
    {
        internal override void EnterState(InternalTransaction tx)
        {
            base.EnterState(tx);

            // Set the transaction state
            CommonEnterState(tx);

            // Do NOT mark the committable transaction as complete because it is aborting.

            // Notify the enlistments that the transaction has aborted
            for (int i = 0; i < tx._phase0Volatiles._volatileEnlistmentCount; i++)
            {
                tx._phase0Volatiles._volatileEnlistments[i]._twoPhaseState!.InternalAborted(tx._phase0Volatiles._volatileEnlistments[i]);
            }

            for (int i = 0; i < tx._phase1Volatiles._volatileEnlistmentCount; i++)
            {
                tx._phase1Volatiles._volatileEnlistments[i]._twoPhaseState!.InternalAborted(tx._phase1Volatiles._volatileEnlistments[i]);
            }

            // Notify the durable enlistment
            if (tx._durableEnlistment != null)
            {
                tx._durableEnlistment.State.InternalAborted(tx._durableEnlistment);
            }

            // Remove this from the timeout list
            TransactionManager.TransactionTable.Remove(tx);

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionTimeout(tx.TransactionTraceId);
            }

            // Fire Completion for anyone listening
            tx.FireCompletion();

            // Check to see if we need to release some waiter.
            if (tx._asyncCommit)
            {
                tx.SignalAsyncCompletion();
            }
        }

        internal override TransactionStatus get_Status(InternalTransaction tx)
        {
            return TransactionStatus.Aborted;
        }


        internal override void Rollback(InternalTransaction tx, Exception? e)
        {
            // Abort is itempotent.  Ignore this if the transaction is already aborted.
        }

        internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback? asyncCallback, object? asyncState)
        {
            // End Commit Must throw a TransactionAbortedException to let the caller know that the tx aborted.
            throw CreateTransactionAbortedException(tx);
        }

        internal override void EndCommit(InternalTransaction tx)
        {
            // End Commit Must throw a TransactionAbortedException to let the caller know that the tx aborted.
            throw CreateTransactionAbortedException(tx);
        }

        internal override void RestartCommitIfNeeded(InternalTransaction tx)
        {
            // Commit does not need to be restarted.
        }

        internal override void Timeout(InternalTransaction tx)
        {
            // The transaction has aborted already
        }

        // When all enlisments respond to prepare this event will fire.
        internal override void Phase0VolatilePrepareDone(InternalTransaction tx)
        {
            // Since the transaction is aborted ignore it.
        }

        internal override void Phase1VolatilePrepareDone(InternalTransaction tx)
        {
            // Since the transaction is aborted ignore it.
        }

        internal override void ChangeStateTransactionAborted(InternalTransaction tx, Exception? e)
        {
            // Yes, yes, yes... I already know.
        }

        internal override void ChangeStatePromotedAborted(InternalTransaction tx)
        {
            // The transaction must have aborted during promotion
        }

        internal override void ChangeStateAbortedDuringPromotion(InternalTransaction tx)
        {
            // This is fine too.
        }

        internal override void CreateBlockingClone(InternalTransaction tx)
        {
            throw CreateTransactionAbortedException(tx);
        }

        internal override void CreateAbortingClone(InternalTransaction tx)
        {
            throw CreateTransactionAbortedException(tx);
        }

        internal override void GetObjectData(InternalTransaction tx, SerializationInfo serializationInfo, StreamingContext context)
        {
            throw CreateTransactionAbortedException(tx);
        }

        internal override void CheckForFinishedTransaction(InternalTransaction tx)
        {
            throw CreateTransactionAbortedException(tx);
        }

        private TransactionException CreateTransactionAbortedException(InternalTransaction tx)
        {
            return TransactionAbortedException.Create(SR.TransactionAborted, tx._innerException, tx.DistributedTxId);
        }
    }



    // TransactionStateCommitted
    //
    // This state indicates that the transaction has been committed.  Basically any
    // operations on the transaction should fail at this point.
    internal sealed class TransactionStateCommitted : TransactionStateEnded
    {
        internal override void EnterState(InternalTransaction tx)
        {
            base.EnterState(tx);

            // Set the transaction state
            CommonEnterState(tx);

            // Notify the phase 0 enlistments that the transaction has aborted
            for (int i = 0; i < tx._phase0Volatiles._volatileEnlistmentCount; i++)
            {
                tx._phase0Volatiles._volatileEnlistments[i]._twoPhaseState!.InternalCommitted(tx._phase0Volatiles._volatileEnlistments[i]);
            }

            // Notify the phase 1 enlistments that the transaction has aborted
            for (int i = 0; i < tx._phase1Volatiles._volatileEnlistmentCount; i++)
            {
                tx._phase1Volatiles._volatileEnlistments[i]._twoPhaseState!.InternalCommitted(tx._phase1Volatiles._volatileEnlistments[i]);
            }

            // Remove this from the timeout list
            TransactionManager.TransactionTable.Remove(tx);

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionCommitted(tx.TransactionTraceId);
            }

            // Fire Completion for anyone listening
            tx.FireCompletion();

            // Check to see if we need to release some waiter.
            if (tx._asyncCommit)
            {
                tx.SignalAsyncCompletion();
            }
        }


        internal override TransactionStatus get_Status(InternalTransaction tx)
        {
            return TransactionStatus.Committed;
        }


        internal override void Rollback(InternalTransaction tx, Exception? e)
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }


        internal override void EndCommit(InternalTransaction tx)
        {
            // End Commit does nothing because life is wonderful and we are happy!
        }
    }


    // TransactionStateInDoubt
    //
    // This state indicates that the transaction is in doubt
    internal sealed class TransactionStateInDoubt : TransactionStateEnded
    {
        internal override void EnterState(InternalTransaction tx)
        {
            base.EnterState(tx);

            // Set the transaction state
            CommonEnterState(tx);

            // Notify the phase 0 enlistments that the transaction has aborted
            for (int i = 0; i < tx._phase0Volatiles._volatileEnlistmentCount; i++)
            {
                tx._phase0Volatiles._volatileEnlistments[i]._twoPhaseState!.InternalIndoubt(tx._phase0Volatiles._volatileEnlistments[i]);
            }

            // Notify the phase 1 enlistments that the transaction has aborted
            for (int i = 0; i < tx._phase1Volatiles._volatileEnlistmentCount; i++)
            {
                tx._phase1Volatiles._volatileEnlistments[i]._twoPhaseState!.InternalIndoubt(tx._phase1Volatiles._volatileEnlistments[i]);
            }

            // Remove this from the timeout list
            TransactionManager.TransactionTable.Remove(tx);

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionInDoubt(tx.TransactionTraceId);
            }

            // Fire Completion for anyone listening
            tx.FireCompletion();

            // Check to see if we need to release some waiter.
            if (tx._asyncCommit)
            {
                tx.SignalAsyncCompletion();
            }
        }


        internal override TransactionStatus get_Status(InternalTransaction tx)
        {
            return TransactionStatus.InDoubt;
        }


        internal override void Rollback(InternalTransaction tx, Exception? e)
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }


        internal override void EndCommit(InternalTransaction tx)
        {
            throw TransactionInDoubtException.Create(TraceSourceType.TraceSourceBase, SR.TransactionIndoubt, tx._innerException, tx.DistributedTxId);
        }


        internal override void CheckForFinishedTransaction(InternalTransaction tx)
        {
            throw TransactionInDoubtException.Create(TraceSourceType.TraceSourceBase, SR.TransactionIndoubt, tx._innerException, tx.DistributedTxId);
        }


        internal override void GetObjectData(InternalTransaction tx, SerializationInfo serializationInfo, StreamingContext context)
        {
            throw TransactionInDoubtException.Create(TraceSourceType.TraceSourceBase, SR.TransactionIndoubt, tx._innerException, tx.DistributedTxId);
        }
    }



    // TransactionStatePromotedBase
    //
    // This is the base class for promoted states.  It's main function is to pass calls
    // through to the distributed transaction.
    internal abstract class TransactionStatePromotedBase : TransactionState
    {
        internal override TransactionStatus get_Status(InternalTransaction tx)
        {
            // Since the distributed transaction manager will always tell the ltm about state
            // changes via the enlistment that the Ltm has with it, the Ltm can tell client
            // code what it thinks the state is on behalf of the distributed tm.  Doing so
            // prevents races with state changes of the promoted tx to the Ltm being
            // told about those changes.
            return TransactionStatus.Active;
        }


        internal override Enlistment EnlistVolatile(
            InternalTransaction tx,
            IEnlistmentNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction
            )
        {
            Debug.Assert(tx.PromotedTransaction != null, "Promoted state not valid for transaction.");
            // Don't get in the way for new volatile enlistments

            // Don't hold locks while calling into the promoted tx
            Monitor.Exit(tx);
            try
            {
                Enlistment en = new Enlistment(enlistmentNotification, tx, atomicTransaction);
                EnlistmentState.EnlistmentStatePromoted.EnterState(en.InternalEnlistment);

                en.InternalEnlistment.PromotedEnlistment =
                    tx.PromotedTransaction.EnlistVolatile(
                        en.InternalEnlistment, enlistmentOptions);
                return en;
            }
            finally
            {
                Monitor.Enter(tx);
            }
        }


        internal override Enlistment EnlistVolatile(
            InternalTransaction tx,
            ISinglePhaseNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction
            )
        {
            Debug.Assert(tx.PromotedTransaction != null, "Promoted state not valid for transaction.");
            // Don't get in the way for new volatile enlistments

            // Don't hold locks while calling into the promoted tx
            Monitor.Exit(tx);
            try
            {
                Enlistment en = new Enlistment(enlistmentNotification, tx, atomicTransaction);
                EnlistmentState.EnlistmentStatePromoted.EnterState(en.InternalEnlistment);

                en.InternalEnlistment.PromotedEnlistment =
                    tx.PromotedTransaction.EnlistVolatile(
                        en.InternalEnlistment, enlistmentOptions);
                return en;
            }
            finally
            {
                Monitor.Enter(tx);
            }
        }


        internal override Enlistment EnlistDurable(
            InternalTransaction tx,
            Guid resourceManagerIdentifier,
            IEnlistmentNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction
            )
        {
            Debug.Assert(tx.PromotedTransaction != null, "Promoted state not valid for transaction.");

            tx.ThrowIfPromoterTypeIsNotMSDTC();

            // Don't hold locks while calling into the promoted tx
            Monitor.Exit(tx);
            try
            {
                Enlistment en = new Enlistment(
                    resourceManagerIdentifier,
                    tx,
                    enlistmentNotification,
                    null,
                    atomicTransaction
                    );
                EnlistmentState.EnlistmentStatePromoted.EnterState(en.InternalEnlistment);

                en.InternalEnlistment.PromotedEnlistment =
                    tx.PromotedTransaction.EnlistDurable(
                        resourceManagerIdentifier,
                        (DurableInternalEnlistment)en.InternalEnlistment,
                        false,
                        enlistmentOptions
                        );
                return en;
            }
            finally
            {
                Monitor.Enter(tx);
            }
        }


        internal override Enlistment EnlistDurable(
            InternalTransaction tx,
            Guid resourceManagerIdentifier,
            ISinglePhaseNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction
            )
        {
            Debug.Assert(tx.PromotedTransaction != null, "Promoted state not valid for transaction.");

            tx.ThrowIfPromoterTypeIsNotMSDTC();

            // Don't hold locks while calling into the promoted tx
            Monitor.Exit(tx);
            try
            {
                Enlistment en = new Enlistment(
                    resourceManagerIdentifier,
                    tx,
                    enlistmentNotification,
                    enlistmentNotification,
                    atomicTransaction
                    );
                EnlistmentState.EnlistmentStatePromoted.EnterState(en.InternalEnlistment);

                en.InternalEnlistment.PromotedEnlistment =
                    tx.PromotedTransaction.EnlistDurable(
                        resourceManagerIdentifier,
                        (DurableInternalEnlistment)en.InternalEnlistment,
                        true,
                        enlistmentOptions
                        );
                return en;
            }
            finally
            {
                Monitor.Enter(tx);
            }
        }


        internal override void Rollback(InternalTransaction tx, Exception? e)
        {
            Debug.Assert(tx.PromotedTransaction != null, "Promoted state not valid for transaction.");
            // Forward this on to the promoted transaction.

            if (tx._innerException == null)
            {
                tx._innerException = e;
            }

            // Don't hold locks while calling into the promoted tx
            Monitor.Exit(tx);
            try
            {
                tx.PromotedTransaction.Rollback();
            }
            finally
            {
                Monitor.Enter(tx);
            }
        }


        internal override Guid get_Identifier(InternalTransaction? tx)
        {
            if (tx != null && tx.PromotedTransaction != null)
            {
                return tx.PromotedTransaction.Identifier;
            }
            else
            {
                return Guid.Empty;
            }
        }


        internal override void AddOutcomeRegistrant(InternalTransaction tx, TransactionCompletedEventHandler? transactionCompletedDelegate)
        {
            // Add this delegate to the list of delegates to be notified of the outcome.
            tx._transactionCompletedDelegate = (TransactionCompletedEventHandler?)
                System.Delegate.Combine(tx._transactionCompletedDelegate, transactionCompletedDelegate);
        }


        internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback? asyncCallback, object? asyncState)
        {
            // Store the given values
            tx._asyncCommit = asyncCommit;
            tx._asyncCallback = asyncCallback;
            tx._asyncState = asyncState;

            // Start the commit process.
            TransactionStatePromotedCommitting.EnterState(tx);
        }


        internal override void RestartCommitIfNeeded(InternalTransaction tx)
        {
            TransactionStatePromotedP0Wave.EnterState(tx);
        }


        internal override bool EnlistPromotableSinglePhase(
            InternalTransaction tx, IPromotableSinglePhaseNotification promotableSinglePhaseNotification,
            Transaction atomicTransaction,
            Guid promoterType
            )
        {
            // The transaction has been promoted and cannot support a promotable singe phase enlistment
            return false;
        }


        internal override void CompleteBlockingClone(InternalTransaction tx)
        {
            // First try to complete one of the internal blocking clones
            if (tx._phase0Volatiles._dependentClones > 0)
            {
                // decrement the number of clones
                tx._phase0Volatiles._dependentClones--;

                // Make certain we increment the right list.
                Debug.Assert(tx._phase0Volatiles._preparedVolatileEnlistments <=
                    tx._phase0Volatiles._volatileEnlistmentCount + tx._phase0Volatiles._dependentClones);

                // Check to see if all of the volatile enlistments are done.
                if (tx._phase0Volatiles._preparedVolatileEnlistments ==
                    tx._phase0VolatileWaveCount + tx._phase0Volatiles._dependentClones)
                {
                    tx.State!.Phase0VolatilePrepareDone(tx);
                }
            }
            else
            {
                // Otherwise this must be a dependent clone created after promotion
                tx._phase0WaveDependentCloneCount--;
                Debug.Assert(tx._phase0WaveDependentCloneCount >= 0);
                if (tx._phase0WaveDependentCloneCount == 0)
                {
                    DistributedDependentTransaction dtx = tx._phase0WaveDependentClone!;
                    tx._phase0WaveDependentClone = null;

                    Monitor.Exit(tx);
                    try
                    {
                        try
                        {
                            dtx.Complete();
                        }
                        finally
                        {
                            dtx.Dispose();
                        }
                    }
                    finally
                    {
                        Monitor.Enter(tx);
                    }
                }
            }
        }


        internal override void CompleteAbortingClone(InternalTransaction tx)
        {
            // If we have a phase1Volatile.VolatileDemux, we have a phase1 volatile enlistment
            // on the promoted transaction and it will take care of checking for incomplete aborting
            // dependent clones in its Prepare processing.
            if (null != tx._phase1Volatiles.VolatileDemux)
            {
                tx._phase1Volatiles._dependentClones--;
                Debug.Assert(tx._phase1Volatiles._dependentClones >= 0);
            }
            else
            // We need to deal with the aborting clones ourself, possibly completing the aborting
            // clone we have on the promoted transaction.
            {
                tx._abortingDependentCloneCount--;
                Debug.Assert(0 <= tx._abortingDependentCloneCount);
                if (0 == tx._abortingDependentCloneCount)
                {
                    // We need to complete our dependent clone on the promoted transaction and null it out
                    // so if we get a new one, a new one will be created on the promoted transaction.
                    DistributedDependentTransaction dtx = tx._abortingDependentClone!;
                    tx._abortingDependentClone = null;

                    Monitor.Exit(tx);
                    try
                    {
                        try
                        {
                            dtx.Complete();
                        }
                        finally
                        {
                            dtx.Dispose();
                        }
                    }
                    finally
                    {
                        Monitor.Enter(tx);
                    }
                }
            }
        }


        internal override void CreateBlockingClone(InternalTransaction tx)
        {
            // Once the transaction is promoted leverage the distributed
            // transaction manager for blocking dependent clones so that they
            // will handle phase 0 waves.
            if (tx._phase0WaveDependentClone == null)
            {
                Debug.Assert(tx.PromotedTransaction != null);
                tx._phase0WaveDependentClone = tx.PromotedTransaction.DependentClone(true);
            }

            tx._phase0WaveDependentCloneCount++;
        }


        internal override void CreateAbortingClone(InternalTransaction tx)
        {
            // If we have a VolatileDemux in phase1Volatiles, then we have a phase1 volatile enlistment
            // on the promoted transaction, so we can depend on that to deal with our aborting dependent clones.
            if (null != tx._phase1Volatiles.VolatileDemux)
            {
                tx._phase1Volatiles._dependentClones++;
            }
            else
            // We promoted without creating a phase1 volatile enlistment on the promoted transaction,
            // so we let the promoted transaction deal with the aboring clone.
            {
                if (null == tx._abortingDependentClone)
                {
                    Debug.Assert(tx.PromotedTransaction != null);
                    tx._abortingDependentClone = tx.PromotedTransaction.DependentClone(false);
                }
                tx._abortingDependentCloneCount++;
            }
        }


        internal override bool ContinuePhase0Prepares()
        {
            return true;
        }


        internal override void GetObjectData(InternalTransaction tx, SerializationInfo serializationInfo, StreamingContext context)
        {
            Debug.Assert(tx.PromotedTransaction != null, "Promoted state not valid for transaction.");

            // This is not allowed if the transaction's PromoterType is not MSDTC.
            tx.ThrowIfPromoterTypeIsNotMSDTC();

            // Simply get call get object data for the promoted transaction.
            ISerializable serializableTx = tx.PromotedTransaction as ISerializable;
            if (serializableTx == null)
            {
                // The LTM can only support this if the Distributed TM Supports it.
                throw new NotSupportedException();
            }

            // Before forwarding this call to the promoted tx make sure to change
            // the full type info so that only if the promoted tx does not set this
            // then it should be set correctly.
            serializationInfo.FullTypeName = tx.PromotedTransaction.GetType().FullName!;

            // Now forward the call.
            serializableTx.GetObjectData(serializationInfo, context);
        }

        internal override void ChangeStatePromotedAborted(InternalTransaction tx)
        {
            TransactionStatePromotedAborted.EnterState(tx);
        }


        internal override void ChangeStatePromotedCommitted(InternalTransaction tx)
        {
            TransactionStatePromotedCommitted.EnterState(tx);
        }


        internal override void InDoubtFromDtc(InternalTransaction tx)
        {
            TransactionStatePromotedIndoubt.EnterState(tx);
        }


        internal override void InDoubtFromEnlistment(InternalTransaction tx)
        {
            TransactionStatePromotedIndoubt.EnterState(tx);
        }


        internal override void ChangeStateAbortedDuringPromotion(InternalTransaction tx)
        {
            TransactionStateAborted.EnterState(tx);
        }


        internal override void Timeout(InternalTransaction tx)
        {
            // LTM gives up the ability to control Tx timeout when it promotes.
            try
            {
                if (tx._innerException == null)
                {
                    tx._innerException = new TimeoutException(SR.TraceTransactionTimeout);
                }
                Debug.Assert(tx.PromotedTransaction != null);
                tx.PromotedTransaction.Rollback();

                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.TransactionTimeout(tx.TransactionTraceId);
                }
            }
            catch (TransactionException te)
            {
                // This could fail for any number of reasons based on the state of the transaction.
                // The Ltm tries anyway because PSPE transactions have no timeout specified and some
                // distributed transaction managers may not honer the timeout correctly.

                // The exception needs to be caught because we don't want it to go unhandled on the
                // timer thread.

                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.ExceptionConsumed(te);
                }
            }
        }


        internal override void Promote(InternalTransaction tx)
        {
            // do nothing, we are already promoted
        }

        internal override byte[] PromotedToken(InternalTransaction tx)
        {
            // Since we are in TransactionStatePromotedBase or one if its derived classes, we
            // must already be promoted. So return the InternalTransaction's promotedToken.
            Debug.Assert(tx.promotedToken != null, "InternalTransaction.promotedToken is null in TransactionStateDelegatedNonMSDTCBase or one of its derived classes.");
            return tx.promotedToken;
        }

        internal override void Phase0VolatilePrepareDone(InternalTransaction tx)
        {
            // Early done notifications may come from volatiles at any time.
            // The state machine will handle all enlistments being complete in later phases.
        }


        internal override void Phase1VolatilePrepareDone(InternalTransaction tx)
        {
            // Early done notifications may come from volatiles at any time.
            // The state machine will handle all enlistments being complete in later phases.
        }
    }


    // TransactionStateNonCommittablePromoted
    //
    // This state indicates that the transaction has been promoted and all further actions on
    // the transaction should be forwarded to the promoted transaction.
    internal sealed class TransactionStateNonCommittablePromoted : TransactionStatePromotedBase
    {
        internal override void EnterState(InternalTransaction tx)
        {
            // Set the transaction state
            CommonEnterState(tx);

            Debug.Assert(tx.PromotedTransaction != null && tx.PromotedTransaction.RealTransaction != null);
            // Let the distributed transaction know that we want to know about the outcome.
            tx.PromotedTransaction.RealTransaction.InternalTransaction = tx;
        }
    }

    // TransactionStatePromoted
    //
    // This state indicates that the transaction has been promoted and all further actions on
    // the transaction should be forwarded to the promoted transaction.
    internal class TransactionStatePromoted : TransactionStatePromotedBase
    {
        internal override void EnterState(InternalTransaction tx)
        {
            Debug.Assert((tx._promoterType == Guid.Empty) || (tx._promoterType == TransactionInterop.PromoterTypeDtc), "Promoted to MSTC but PromoterType is not TransactionInterop.PromoterTypeDtc");
            // The promoterType may not yet be set. This state assumes we are promoting to MSDTC.
            tx.SetPromoterTypeToMSDTC();

            if (tx._outcomeSource._isoLevel == IsolationLevel.Snapshot)
            {
                throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceLtm,
                    SR.CannotPromoteSnapshot, null);
            }

            // Set the transaction state
            CommonEnterState(tx);

            // Create a transaction with the distributed transaction manager
            DistributedCommittableTransaction? distributedTx = null;
            try
            {
                TimeSpan newTimeout;
                if (tx.AbsoluteTimeout == long.MaxValue)
                {
                    // The transaction has no timeout
                    newTimeout = TimeSpan.Zero;
                }
                else
                {
                    newTimeout = TransactionManager.TransactionTable.RecalcTimeout(tx);
                    if (newTimeout <= TimeSpan.Zero)
                    {
                        return;
                    }
                }

                // Just create a new transaction.
                TransactionOptions options = default;

                options.IsolationLevel = tx._outcomeSource._isoLevel;
                options.Timeout = newTimeout;

                // Create a new distributed transaction.
                distributedTx =
                    TransactionManager.DistributedTransactionManager.CreateTransaction(options);
                distributedTx.SavedLtmPromotedTransaction = tx._outcomeSource;

                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.TransactionPromoted(tx.TransactionTraceId, distributedTx.TransactionTraceId);
                }
            }
            catch (TransactionException te)
            {
                // There was an exception trying to create the distributed transaction.
                // Save the exception and let the transaction get aborted by the finally block.
                tx._innerException = te;
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.ExceptionConsumed(te);
                }
                return;
            }
            finally
            {
                if (distributedTx == null)
                {
                    // There was an exception trying to create the distributed transaction abort
                    // the local transaction and exit.
                    tx.State!.ChangeStateAbortedDuringPromotion(tx);
                }
            }

            // Associate the distributed transaction with the local transaction.
            tx.PromotedTransaction = distributedTx;

            // Add a weak reference to the transaction to the promotedTransactionTable.
            Hashtable promotedTransactionTable = TransactionManager.PromotedTransactionTable;
            lock (promotedTransactionTable)
            {
                // Since we are adding this reference to the table create an object that will clean that
                // entry up.
                tx._finalizedObject = new FinalizedObject(tx, distributedTx.Identifier);

                WeakReference weakRef = new WeakReference(tx._outcomeSource, false);
                promotedTransactionTable[distributedTx.Identifier] = weakRef;
            }
            TransactionManager.FireDistributedTransactionStarted(tx._outcomeSource);

            // Once we have a promoted transaction promote the enlistments.
            PromoteEnlistmentsAndOutcome(tx);
        }


        protected bool PromotePhaseVolatiles(
            InternalTransaction tx,
            ref VolatileEnlistmentSet volatiles,
            bool phase0)
        {
            if (volatiles._volatileEnlistmentCount + volatiles._dependentClones > 0)
            {
                if (phase0)
                {
                    // Create a volatile demultiplexer for the transaction
                    volatiles.VolatileDemux = new Phase0VolatileDemultiplexer(tx);
                }
                else
                {
                    // Create a volatile demultiplexer for the transaction
                    volatiles.VolatileDemux = new Phase1VolatileDemultiplexer(tx);
                }

                Debug.Assert(tx.PromotedTransaction != null);
                volatiles.VolatileDemux._promotedEnlistment = tx.PromotedTransaction.EnlistVolatile(volatiles.VolatileDemux,
                    phase0 ? EnlistmentOptions.EnlistDuringPrepareRequired : EnlistmentOptions.None);
            }

            return true;
        }


        internal virtual bool PromoteDurable(InternalTransaction tx)
        {
            // Promote the durable enlistment if one exists.
            if (tx._durableEnlistment != null)
            {
                // Directly enlist the durable enlistment with the resource manager.
                InternalEnlistment enlistment = tx._durableEnlistment;
                Debug.Assert(tx.PromotedTransaction != null);
                IPromotedEnlistment promotedEnlistment = tx.PromotedTransaction.EnlistDurable(
                    enlistment.ResourceManagerIdentifier,
                    (DurableInternalEnlistment)enlistment,
                    enlistment.SinglePhaseNotification != null,
                    EnlistmentOptions.None
                    );

                // Promote the enlistment.
                tx._durableEnlistment.State.ChangeStatePromoted(tx._durableEnlistment, promotedEnlistment);
            }

            return true;
        }


        internal virtual void PromoteEnlistmentsAndOutcome(InternalTransaction tx)
        {
            // Failures from this point on will simply abort the two types of transaction
            // separately.  Note that this may cause duplicate internal aborted events to
            // be sent to some of the enlistments however the enlistment state machines
            // can handle the duplicate notification.

            bool enlistmentsPromoted = false;

            Debug.Assert(tx.PromotedTransaction != null && tx.PromotedTransaction.RealTransaction != null);
            // Tell the real transaction that we want a callback for the outcome.
            tx.PromotedTransaction.RealTransaction.InternalTransaction = tx;

            Debug.Assert(tx.State != null);
            // Promote Phase 0 Volatiles
            try
            {
                enlistmentsPromoted = PromotePhaseVolatiles(tx, ref tx._phase0Volatiles, true);
            }
            catch (TransactionException te)
            {
                // Record the exception information.
                tx._innerException = te;
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.ExceptionConsumed(te);
                }

                return;
            }
            finally
            {
                if (!enlistmentsPromoted)
                {
                    tx.PromotedTransaction.Rollback();

                    // Now abort this transaction.
                    tx.State.ChangeStateAbortedDuringPromotion(tx);
                }
            }

            enlistmentsPromoted = false;

            try
            {
                enlistmentsPromoted = PromotePhaseVolatiles(tx, ref tx._phase1Volatiles, false);
            }
            catch (TransactionException te)
            {
                // Record the exception information.
                tx._innerException = te;
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.ExceptionConsumed(te);
                }

                return;
            }
            finally
            {
                if (!enlistmentsPromoted)
                {
                    tx.PromotedTransaction.Rollback();

                    // Now abort this transaction.
                    tx.State.ChangeStateAbortedDuringPromotion(tx);
                }
            }

            enlistmentsPromoted = false;

            // Promote the durable enlistment
            try
            {
                enlistmentsPromoted = PromoteDurable(tx);
            }
            catch (TransactionException te)
            {
                // Record the exception information.
                tx._innerException = te;
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.ExceptionConsumed(te);
                }

                return;
            }
            finally
            {
                if (!enlistmentsPromoted)
                {
                    tx.PromotedTransaction.Rollback();

                    // Now abort this transaction.
                    tx.State.ChangeStateAbortedDuringPromotion(tx);
                }
            }
        }


        internal override void DisposeRoot(InternalTransaction tx)
        {
            tx.State!.Rollback(tx, null);
        }
    }


    // TransactionStatePromotedP0Wave
    //
    // This state indicates that the transaction has been promoted during phase 0.  This
    // is a holding state until the current phase 0 wave is complete.  When the current
    // wave is complete the state changes to committing.
    internal class TransactionStatePromotedP0Wave : TransactionStatePromotedBase
    {
        internal override void EnterState(InternalTransaction tx)
        {
            CommonEnterState(tx);
        }


        internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback? asyncCallback, object? asyncState)
        {
            // Don't allow this again.
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }


        internal override void Phase0VolatilePrepareDone(InternalTransaction tx)
        {
            try
            {
                // Now that the previous wave is done continue start committing the transaction.
                TransactionStatePromotedCommitting.EnterState(tx);
            }
            catch (TransactionException e)
            {
                // In this state we don't want a transaction exception from BeginCommit to randomly
                // bubble up to the application or go unhandled.  So catch the exception and if the
                // inner exception for the transaction has not already been set then set it.
                if (tx._innerException == null)
                {
                    tx._innerException = e;
                }
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.ExceptionConsumed(e);
                }
            }
        }


        internal override bool ContinuePhase0Prepares()
        {
            return true;
        }


        internal override void ChangeStateTransactionAborted(InternalTransaction tx, Exception? e)
        {
            if (tx._innerException == null)
            {
                tx._innerException = e;
            }

            // This change state event at this point would be caused by one of the enlistments
            // aborting.  Really change to P0Aborting
            TransactionStatePromotedP0Aborting.EnterState(tx);
        }
    }


    // TransactionStatePromotedCommitting
    //
    // The transaction has been promoted but is in the process of committing.
    internal class TransactionStatePromotedCommitting : TransactionStatePromotedBase
    {
        internal override void EnterState(InternalTransaction tx)
        {
            CommonEnterState(tx);

            // Use the asynchronous commit provided by the promoted transaction
            var ctx = (DistributedCommittableTransaction)tx.PromotedTransaction!;
            ctx.BeginCommit(tx);
        }


        internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback? asyncCallback, object? asyncState)
        {
            // Don't allow this again.
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }


        internal override void ChangeStatePromotedPhase0(InternalTransaction tx)
        {
            TransactionStatePromotedPhase0.EnterState(tx);
        }


        internal override void ChangeStatePromotedPhase1(InternalTransaction tx)
        {
            TransactionStatePromotedPhase1.EnterState(tx);
        }
    }

    // TransactionStatePromotedPhase0
    //
    // This state indicates that the transaction has been promoted and started the process
    // of committing.  The transaction had volatile phase0 enlistments and is acting as a
    // proxy to the TM for those phase0 enlistments.
    internal sealed class TransactionStatePromotedPhase0 : TransactionStatePromotedCommitting
    {
        internal override void EnterState(InternalTransaction tx)
        {
            CommonEnterState(tx);

            // Get a copy of the current volatile enlistment count before entering this loop so that other
            // threads don't affect the operation of this loop.
            int volatileCount = tx._phase0Volatiles._volatileEnlistmentCount;
            int dependentCount = tx._phase0Volatiles._dependentClones;

            // Store the number of phase0 volatiles for this wave.
            tx._phase0VolatileWaveCount = volatileCount;

            // Check to see if we still need to send out volatile prepare notifications or if
            // they are all done.  They may be done if the transaction was already in phase 0
            // before it got promoted.
            if (tx._phase0Volatiles._preparedVolatileEnlistments <
                volatileCount + dependentCount)
            {
                // Broadcast preprepare to the volatile subordinates
                for (int i = 0; i < volatileCount; i++)
                {
                    tx._phase0Volatiles._volatileEnlistments[i]._twoPhaseState!.ChangeStatePreparing(
                        tx._phase0Volatiles._volatileEnlistments[i]);

                    if (!tx.State!.ContinuePhase0Prepares())
                    {
                        break;
                    }
                }
            }
            else
            {
                Phase0VolatilePrepareDone(tx);
            }
        }


        internal override void Phase0VolatilePrepareDone(InternalTransaction tx)
        {
            Debug.Assert(tx._phase0Volatiles.VolatileDemux != null, "Volatile Demux must exist for VolatilePrepareDone when promoted.");

            Monitor.Exit(tx);
            try
            {
                Debug.Assert(tx._phase0Volatiles.VolatileDemux._promotedEnlistment != null);
                // Tell the distributed TM that the volatile enlistments are prepared
                tx._phase0Volatiles.VolatileDemux._promotedEnlistment.Prepared();
            }
            finally
            {
                Monitor.Enter(tx);
            }
        }


        internal override bool ContinuePhase0Prepares()
        {
            return true;
        }


        internal override void ChangeStateTransactionAborted(InternalTransaction tx, Exception? e)
        {
            if (tx._innerException == null)
            {
                tx._innerException = e;
            }

            // This change state event at this point would be caused by one of the enlistments
            // aborting.  Really change to P0Aborting
            TransactionStatePromotedP0Aborting.EnterState(tx);
        }
    }


    // TransactionStatePromotedPhase1
    //
    // This state indicates that the transaction has been promoted and started the process
    // of committing.  The transaction had volatile phase1 enlistments and is acting as a
    // proxy to the TM for those phase1 enlistments.
    internal sealed class TransactionStatePromotedPhase1 : TransactionStatePromotedCommitting
    {
        internal override void EnterState(InternalTransaction tx)
        {
            CommonEnterState(tx);

            if (tx._committableTransaction != null)
            {
                // If we have a committable transaction then mark it as complete.
                tx._committableTransaction._complete = true;
            }

            // If at this point there are phase1 dependent clones abort the transaction
            if (tx._phase1Volatiles._dependentClones != 0)
            {
                tx.State!.ChangeStateTransactionAborted(tx, null);
                return;
            }

            // Get a copy of the current volatile enlistment count before entering this loop so that other
            // threads don't affect the operation of this loop.
            int volatileCount = tx._phase1Volatiles._volatileEnlistmentCount;

            // Check to see if we still need to send out volatile prepare notifications or if
            // they are all done.  They may be done if the transaction was already in phase 0
            // before it got promoted.
            if (tx._phase1Volatiles._preparedVolatileEnlistments < volatileCount)
            {
                // Broadcast preprepare to the volatile subordinates
                for (int i = 0; i < volatileCount; i++)
                {
                    tx._phase1Volatiles._volatileEnlistments[i]._twoPhaseState!.ChangeStatePreparing(
                        tx._phase1Volatiles._volatileEnlistments[i]);
                    if (!tx.State!.ContinuePhase1Prepares())
                    {
                        break;
                    }
                }
            }
            else
            {
                Phase1VolatilePrepareDone(tx);
            }
        }


        internal override void CreateBlockingClone(InternalTransaction tx)
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }


        internal override void CreateAbortingClone(InternalTransaction tx)
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }


        internal override void ChangeStateTransactionAborted(InternalTransaction tx, Exception? e)
        {
            if (tx._innerException == null)
            {
                tx._innerException = e;
            }

            // This change state event at this point would be caused by one of the enlistments
            // aborting.  Really change to P1Aborting
            TransactionStatePromotedP1Aborting.EnterState(tx);
        }


        internal override void Phase1VolatilePrepareDone(InternalTransaction tx)
        {
            Debug.Assert(tx._phase1Volatiles.VolatileDemux != null, "Volatile Demux must exist for VolatilePrepareDone when promoted.");

            Monitor.Exit(tx);
            try
            {
                Debug.Assert(tx._phase1Volatiles.VolatileDemux._promotedEnlistment != null);
                // Tell the distributed TM that the volatile enlistments are prepared
                tx._phase1Volatiles.VolatileDemux._promotedEnlistment.Prepared();
            }
            finally
            {
                Monitor.Enter(tx);
            }
        }


        internal override bool ContinuePhase1Prepares()
        {
            return true;
        }


        internal override Enlistment EnlistVolatile(InternalTransaction tx, IEnlistmentNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
        {
            throw TransactionException.Create(SR.TooLate, tx == null ? Guid.Empty : tx.DistributedTxId);
        }


        internal override Enlistment EnlistVolatile(InternalTransaction tx, ISinglePhaseNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
        {
            throw TransactionException.Create(SR.TooLate, tx == null ? Guid.Empty : tx.DistributedTxId);
        }


        internal override Enlistment EnlistDurable(InternalTransaction tx, Guid resourceManagerIdentifier, IEnlistmentNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
        {
            throw TransactionException.Create(SR.TooLate, tx == null ? Guid.Empty : tx.DistributedTxId);
        }


        internal override Enlistment EnlistDurable(InternalTransaction tx, Guid resourceManagerIdentifier, ISinglePhaseNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
        {
            throw TransactionException.Create(SR.TooLate, tx == null ? Guid.Empty : tx.DistributedTxId);
        }
    }



    // TransactionStatePromotedAborting
    //
    // This state indicates that the transaction has been promoted but aborted.  Once the volatile
    // enlistments have finished responding the tx can be finished.
    internal abstract class TransactionStatePromotedAborting : TransactionStatePromotedBase
    {
        internal override void EnterState(InternalTransaction tx)
        {
            CommonEnterState(tx);
        }


        internal override TransactionStatus get_Status(InternalTransaction tx)
        {
            return TransactionStatus.Aborted;
        }


        internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback? asyncCallback, object? asyncState)
        {
            // Don't allow this again.
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }


        internal override void CreateBlockingClone(InternalTransaction tx)
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }


        internal override void CreateAbortingClone(InternalTransaction tx)
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }


        internal override void ChangeStatePromotedAborted(InternalTransaction tx)
        {
            TransactionStatePromotedAborted.EnterState(tx);
        }


        internal override void ChangeStateTransactionAborted(InternalTransaction tx, Exception? e)
        {
            // Don't do this yet wait until all of the notifications come back.
        }


        internal override void RestartCommitIfNeeded(InternalTransaction tx)
        {
            // Commit cannot be restarted
        }
    }


    // TransactionStatePromotedP0Aborting
    //
    // This state indicates that the transaction has been promoted but aborted by a phase 0 volatile
    // enlistment.  Once the volatile enlistments have finished responding the tx can be finished.
    internal sealed class TransactionStatePromotedP0Aborting : TransactionStatePromotedAborting
    {
        internal override void EnterState(InternalTransaction tx)
        {
            CommonEnterState(tx);

            ChangeStatePromotedAborted(tx);

            // If we have a volatilePreparingEnlistment tell it to roll back.
            if (tx._phase0Volatiles.VolatileDemux._preparingEnlistment != null)
            {
                Monitor.Exit(tx);
                try
                {
                    Debug.Assert(tx._phase0Volatiles.VolatileDemux._promotedEnlistment != null);
                    // Tell the distributed TM that the tx aborted.
                    tx._phase0Volatiles.VolatileDemux._promotedEnlistment.ForceRollback();
                }
                finally
                {
                    Monitor.Enter(tx);
                }
            }
            else
            {
                Debug.Assert(tx.PromotedTransaction != null);
                // Otherwise make sure that the transaction rolls back.
                tx.PromotedTransaction.Rollback();
            }
        }


        internal override void Phase0VolatilePrepareDone(InternalTransaction tx)
        {
            // If this happens as a race it is just fine.
        }
    }


    // TransactionStatePromotedP1Aborting
    //
    // This state indicates that the transaction has been promoted but aborted by a phase 1 volatile
    // enlistment.  Once the volatile enlistments have finished responding the tx can be finished.
    internal sealed class TransactionStatePromotedP1Aborting : TransactionStatePromotedAborting
    {
        internal override void EnterState(InternalTransaction tx)
        {
            CommonEnterState(tx);

            Debug.Assert(tx._phase1Volatiles.VolatileDemux != null, "Volatile Demux must exist.");

            ChangeStatePromotedAborted(tx);

            Monitor.Exit(tx);
            try
            {
                Debug.Assert(tx._phase1Volatiles.VolatileDemux._promotedEnlistment != null);
                // Tell the distributed TM that the tx aborted.
                tx._phase1Volatiles.VolatileDemux._promotedEnlistment.ForceRollback();
            }
            finally
            {
                Monitor.Enter(tx);
            }
        }


        internal override void Phase1VolatilePrepareDone(InternalTransaction tx)
        {
            // If this happens as a race it is fine.
        }
    }


    // TransactionStatePromotedEnded
    //
    // This is a common base class for committed, aborted, and indoubt states of a promoted
    // transaction.
    internal abstract class TransactionStatePromotedEnded : TransactionStateEnded
    {
        internal override void EnterState(InternalTransaction tx)
        {
            base.EnterState(tx);

            CommonEnterState(tx);

            if (!ThreadPool.QueueUserWorkItem(SignalMethod, tx))
            {
                throw TransactionException.CreateInvalidOperationException(
                    TraceSourceType.TraceSourceLtm,
                    SR.UnexpectedFailureOfThreadPool,
                    null,
                    tx == null ? Guid.Empty : tx.DistributedTxId
                    );
            }
        }


        internal override void AddOutcomeRegistrant(InternalTransaction tx, TransactionCompletedEventHandler? transactionCompletedDelegate)
        {
            if (transactionCompletedDelegate != null)
            {
                TransactionEventArgs args = new TransactionEventArgs();
                args._transaction = tx._outcomeSource.InternalClone();
                transactionCompletedDelegate(args._transaction, args);
            }
        }


        internal override void EndCommit(InternalTransaction tx)
        {
            // Test the outcome of the transaction and respond accordingly.
            Debug.Assert(tx.PromotedTransaction != null, "Promoted state not valid for transaction.");
            PromotedTransactionOutcome(tx);
        }


        internal override void CompleteBlockingClone(InternalTransaction tx)
        {
            // The transaction is finished ignore these.
        }


        internal override void CompleteAbortingClone(InternalTransaction tx)
        {
            // The transaction is finished ignore these.
        }


        internal override void CreateBlockingClone(InternalTransaction tx)
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }


        internal override void CreateAbortingClone(InternalTransaction tx)
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal override Guid get_Identifier(InternalTransaction tx)
        {
            Debug.Assert(tx.PromotedTransaction != null);
            return tx.PromotedTransaction.Identifier;
        }

        internal override void Promote(InternalTransaction tx)
        {
            // do nothing, we are already promoted
        }

        protected abstract void PromotedTransactionOutcome(InternalTransaction tx);

        private static WaitCallback? s_signalMethod;
        private static WaitCallback SignalMethod => LazyInitializer.EnsureInitialized(ref s_signalMethod, ref s_classSyncObject, () => new WaitCallback(SignalCallback!));


        private static void SignalCallback(object state)
        {
            InternalTransaction tx = (InternalTransaction)state;
            lock (tx)
            {
                tx.SignalAsyncCompletion();
                TransactionManager.TransactionTable.Remove(tx);
            }
        }
    }


    // TransactionStatePromotedAborted
    //
    // This state indicates that the transaction has been promoted and the outcome
    // of the transaction is aborted.
    internal class TransactionStatePromotedAborted : TransactionStatePromotedEnded
    {
        internal override void EnterState(InternalTransaction tx)
        {
            base.EnterState(tx);

            // Tell all the enlistments the outcome.
            if (tx._phase1Volatiles.VolatileDemux != null)
            {
                tx._phase1Volatiles.VolatileDemux.BroadcastRollback(ref tx._phase1Volatiles);
            }

            if (tx._phase0Volatiles.VolatileDemux != null)
            {
                tx._phase0Volatiles.VolatileDemux.BroadcastRollback(ref tx._phase0Volatiles);
            }

            // Fire Completion for anyone listening
            tx.FireCompletion();
            // We don't need to do the AsyncCompletion stuff.  If it was needed, it was done out of SignalCallback.

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionAborted(tx.TransactionTraceId);
            }
        }


        internal override TransactionStatus get_Status(InternalTransaction tx)
        {
            return TransactionStatus.Aborted;
        }


        internal override void Rollback(InternalTransaction tx, Exception? e)
        {
            // Already done.
        }


        internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback? asyncCallback, object? asyncState)
        {
            throw TransactionAbortedException.Create(SR.TransactionAborted, tx._innerException, tx.DistributedTxId);
        }


        internal override void CreateBlockingClone(InternalTransaction tx)
        {
            throw TransactionAbortedException.Create(SR.TransactionAborted, tx._innerException, tx.DistributedTxId);
        }


        internal override void CreateAbortingClone(InternalTransaction tx)
        {
            throw TransactionAbortedException.Create(SR.TransactionAborted, tx._innerException, tx.DistributedTxId);
        }


        internal override void RestartCommitIfNeeded(InternalTransaction tx)
        {
            // Commit cannot be restarted
        }


        internal override void Phase0VolatilePrepareDone(InternalTransaction tx)
        {
            // Since the transaction is aborted ignore it.
        }


        internal override void Phase1VolatilePrepareDone(InternalTransaction tx)
        {
            // Since the transaction is aborted ignore it.
        }


        internal override void ChangeStatePromotedPhase0(InternalTransaction tx)
        {
            throw new TransactionAbortedException(tx._innerException, tx.DistributedTxId);
        }

        internal override void ChangeStatePromotedPhase1(InternalTransaction tx)
        {
            throw new TransactionAbortedException(tx._innerException, tx.DistributedTxId);
        }

        internal override void ChangeStatePromotedAborted(InternalTransaction tx)
        {
            // This call may come from multiple events.  Support being told more than once.
        }


        internal override void ChangeStateTransactionAborted(InternalTransaction tx, Exception? e)
        {
            // This may come from a promotable single phase enlistments abort response.
        }


        protected override void PromotedTransactionOutcome(InternalTransaction tx)
        {
            if ((null == tx._innerException) && (null != tx.PromotedTransaction))
            {
                tx._innerException = tx.PromotedTransaction.InnerException;
            }
            throw TransactionAbortedException.Create(SR.TransactionAborted, tx._innerException, tx.DistributedTxId);
        }


        internal override void CheckForFinishedTransaction(InternalTransaction tx)
        {
            throw new TransactionAbortedException(tx._innerException, tx.DistributedTxId);
        }


        internal override void GetObjectData(InternalTransaction tx, SerializationInfo serializationInfo, StreamingContext context)
        {
            throw TransactionAbortedException.Create(SR.TransactionAborted, tx._innerException, tx.DistributedTxId);
        }


        internal override void InDoubtFromDtc(InternalTransaction tx)
        {
            // Getting this event would mean that a PSPE enlistment has told us the
            // transaction outcome.  It is possible that a PSPE enlistment would know
            // the transaction outcome when DTC does not.  So ignore the indoubt
            // notification from DTC.
        }


        internal override void InDoubtFromEnlistment(InternalTransaction tx)
        {
            // In this case DTC has told us the outcome but a PSPE enlistment
            // is telling us that it does not know the outcome of the transaction.
            // So ignore the notification from the enlistment.
        }
    }



    // TransactionStatePromotedCommitted
    //
    // This state indicates that the transaction has been promoted and the outcome
    // of the transaction is committed
    internal sealed class TransactionStatePromotedCommitted : TransactionStatePromotedEnded
    {
        internal override void EnterState(InternalTransaction tx)
        {
            base.EnterState(tx);

            // Tell all the enlistments the outcome.
            if (tx._phase1Volatiles.VolatileDemux != null)
            {
                tx._phase1Volatiles.VolatileDemux.BroadcastCommitted(ref tx._phase1Volatiles);
            }

            if (tx._phase0Volatiles.VolatileDemux != null)
            {
                tx._phase0Volatiles.VolatileDemux.BroadcastCommitted(ref tx._phase0Volatiles);
            }

            // Fire Completion for anyone listening
            tx.FireCompletion();
            // We don't need to do the AsyncCompletion stuff.  If it was needed, it was done out of SignalCallback.

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionCommitted(tx.TransactionTraceId);
            }
        }


        internal override TransactionStatus get_Status(InternalTransaction tx)
        {
            return TransactionStatus.Committed;
        }


        internal override void ChangeStatePromotedCommitted(InternalTransaction tx)
        {
            // This call may come from multiple different events.  Support being told more than once.
        }


        protected override void PromotedTransactionOutcome(InternalTransaction tx)
        {
            // This is a happy transaction.
        }


        internal override void InDoubtFromDtc(InternalTransaction tx)
        {
            // Getting this event would mean that a PSPE enlistment has told us the
            // transaction outcome.  It is possible that a PSPE enlistment would know
            // the transaction outcome when DTC does not.  So ignore the indoubt
            // notification from DTC.
        }


        internal override void InDoubtFromEnlistment(InternalTransaction tx)
        {
            // In this case DTC has told us the outcome but a PSPE enlistment
            // is telling us that it does not know the outcome of the transaction.
            // So ignore the notification from the enlistment.
        }
    }



    // TransactionStatePromotedIndoubt
    //
    // This state indicates that the transaction has been promoted but the outcome
    // of the transaction is indoubt.
    internal sealed class TransactionStatePromotedIndoubt : TransactionStatePromotedEnded
    {
        internal override void EnterState(InternalTransaction tx)
        {
            base.EnterState(tx);

            // Tell all the enlistments the outcome.
            if (tx._phase1Volatiles.VolatileDemux != null)
            {
                tx._phase1Volatiles.VolatileDemux.BroadcastInDoubt(ref tx._phase1Volatiles);
            }

            if (tx._phase0Volatiles.VolatileDemux != null)
            {
                tx._phase0Volatiles.VolatileDemux.BroadcastInDoubt(ref tx._phase0Volatiles);
            }

            // Fire Completion for anyone listening
            tx.FireCompletion();
            // We don't need to do the AsyncCompletion stuff.  If it was needed, it was done out of SignalCallback.

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionInDoubt(tx.TransactionTraceId);
            }
        }


        internal override TransactionStatus get_Status(InternalTransaction tx)
        {
            return TransactionStatus.InDoubt;
        }


        internal override void RestartCommitIfNeeded(InternalTransaction tx)
        {
            // Commit cannot be restarted
        }


        internal override void ChangeStatePromotedPhase0(InternalTransaction tx)
        {
            throw TransactionInDoubtException.Create(TraceSourceType.TraceSourceBase, SR.TransactionIndoubt, tx._innerException, tx.DistributedTxId);
        }

        internal override void ChangeStatePromotedPhase1(InternalTransaction tx)
        {
            throw TransactionInDoubtException.Create(TraceSourceType.TraceSourceBase, SR.TransactionIndoubt, tx._innerException, tx.DistributedTxId);
        }


        internal override void InDoubtFromDtc(InternalTransaction tx)
        {
            // This call may actually come from multiple sources that race.
            // Since we already took action based on the first notification ignore the
            // others.
        }


        internal override void InDoubtFromEnlistment(InternalTransaction tx)
        {
            // This call may actually come from multiple sources that race.
            // Since we already took action based on the first notification ignore the
            // others.
        }


        protected override void PromotedTransactionOutcome(InternalTransaction tx)
        {
            if ((null == tx._innerException) && (null != tx.PromotedTransaction))
            {
                tx._innerException = tx.PromotedTransaction.InnerException;
            }
            throw TransactionInDoubtException.Create(TraceSourceType.TraceSourceBase, SR.TransactionIndoubt, tx._innerException, tx.DistributedTxId);
        }


        internal override void CheckForFinishedTransaction(InternalTransaction tx)
        {
            throw TransactionInDoubtException.Create(TraceSourceType.TraceSourceBase, SR.TransactionIndoubt, tx._innerException, tx.DistributedTxId);
        }


        internal override void GetObjectData(InternalTransaction tx, SerializationInfo serializationInfo, StreamingContext context)
        {
            throw TransactionInDoubtException.Create(TraceSourceType.TraceSourceBase, SR.TransactionIndoubt, tx._innerException, tx.DistributedTxId);
        }


        internal override void ChangeStatePromotedAborted(InternalTransaction tx)
        {
            // Transaction outcome can come from different directions.  In the case of InDoubt
            // transactions it is possible that one source knowns the actual outcome for
            // the transaction.  However since the transaction does not know if it will receive
            // a different answer for the outcome it accepts the first answer it gets.
            // By the time we receive a better answer the clients of this transaction
            // have already been informed that the transaction is InDoubt.
        }


        internal override void ChangeStatePromotedCommitted(InternalTransaction tx)
        {
            // See comment in ChangeStatePromotedAborted
        }
    }


    // TransactionStateDelegatedBase
    //
    // This state is the base state for delegated transactions
    internal abstract class TransactionStateDelegatedBase : TransactionStatePromoted
    {
        internal override void EnterState(InternalTransaction tx)
        {
            if (tx._outcomeSource._isoLevel == IsolationLevel.Snapshot)
            {
                throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceLtm,
                    SR.CannotPromoteSnapshot, null, tx.DistributedTxId);
            }

            // Assign the state
            CommonEnterState(tx);

            // Create a transaction with the distributed transaction manager
            DistributedTransaction? distributedTx = null;
            try
            {
                // Ask the delegation interface to promote the transaction.
                if (tx._durableEnlistment != null)
                {
                    TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                    if (etwLog.IsEnabled())
                    {
                        etwLog.EnlistmentStatus(tx._durableEnlistment, NotificationCall.Promote);
                    }
                }


                distributedTx = TransactionStatePSPEOperation.PSPEPromote(tx);
            }
            catch (TransactionPromotionException e)
            {
                tx._innerException = e;
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.ExceptionConsumed(e);
                }
            }
            finally
            {
                if (((object?)distributedTx) == null)
                {
                    // There was an exception trying to create the distributed transaction abort
                    // the local transaction and exit.
                    tx.State!.ChangeStateAbortedDuringPromotion(tx);
                }
            }

            if (((object?)distributedTx) == null)
            {
                return;
            }

            // If tx.PromotedTransaction is already set to the distributedTx that was
            // returned, then the PSPE enlistment must have used
            // Transaction.PSPEPromoteAndConvertToEnlistDurable to promote the transaction
            // within the same AppDomain. So we don't need to add the distributedTx to the
            // PromotedTransactionTable and we don't need to call
            // FireDistributedTransactionStarted and we don't need to promote the
            // enlistments. That was all done when the transaction was changed to
            // TransactionStatePromoted.
            if (tx.PromotedTransaction != distributedTx)
            {
                // Associate the distributed transaction with the local transaction.
                tx.PromotedTransaction = distributedTx;

                // Add a weak reference to the transaction to the promotedTransactionTable.
                Hashtable promotedTransactionTable = TransactionManager.PromotedTransactionTable;
                lock (promotedTransactionTable)
                {
                    // Since we are adding this reference to the table create an object that will clean that
                    // entry up.
                    tx._finalizedObject = new FinalizedObject(tx, tx.PromotedTransaction.Identifier);

                    WeakReference weakRef = new WeakReference(tx._outcomeSource, false);
                    promotedTransactionTable[tx.PromotedTransaction.Identifier] = weakRef;
                }
                TransactionManager.FireDistributedTransactionStarted(tx._outcomeSource);

                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.TransactionPromoted(tx.TransactionTraceId, distributedTx.TransactionTraceId);
                }

                // Once we have a promoted transaction promote the enlistments.
                PromoteEnlistmentsAndOutcome(tx);
            }
        }
    }


    // TransactionStateDelegated
    //
    // This state represents a transaction that had a promotable single phase enlistment that then
    // was promoted.  Most of the functionality is inherited from transaction state promoted
    // except for the way that commit happens.
    internal sealed class TransactionStateDelegated : TransactionStateDelegatedBase
    {
        internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback? asyncCallback, object? asyncState)
        {
            // Store the given values
            tx._asyncCommit = asyncCommit;
            tx._asyncCallback = asyncCallback;
            tx._asyncState = asyncState;

            // Initiate the commit process.
            TransactionStateDelegatedCommitting.EnterState(tx);
        }


        internal override bool PromoteDurable(InternalTransaction tx)
        {
            Debug.Assert(tx._durableEnlistment != null);
            // Let the enlistment know that it has been delegated.  For this type of enlistment that
            // is really all that needs to be done.
            tx._durableEnlistment.State.ChangeStateDelegated(tx._durableEnlistment);

            return true;
        }


        internal override void RestartCommitIfNeeded(InternalTransaction tx)
        {
            TransactionStateDelegatedP0Wave.EnterState(tx);
        }


        internal override void Rollback(InternalTransaction tx, Exception? e)
        {
            // Pass the Rollback through the promotable single phase enlistment to be
            // certain it is notified.

            if (tx._innerException == null)
            {
                tx._innerException = e;
            }

            TransactionStateDelegatedAborting.EnterState(tx);
        }
    }


    // TransactionStatePromotedNonMSDTCBase
    //
    // This is the base class for non-MSDTC promoted states.  It's main function is to pass calls
    // through to the distributed transaction.
    internal abstract class TransactionStatePromotedNonMSDTCBase : TransactionState
    {
        internal override TransactionStatus get_Status(InternalTransaction tx)
        {
            return TransactionStatus.Active;
        }

        internal override Enlistment EnlistVolatile(
            InternalTransaction tx,
            IEnlistmentNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction
            )
        {
            Enlistment enlistment = new Enlistment(tx, enlistmentNotification, null, atomicTransaction, enlistmentOptions);
            if ((enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0)
            {
                AddVolatileEnlistment(ref tx._phase0Volatiles, enlistment);
            }
            else
            {
                AddVolatileEnlistment(ref tx._phase1Volatiles, enlistment);
            }

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionstateEnlist(enlistment.InternalEnlistment.EnlistmentTraceId, EnlistmentType.Volatile, enlistmentOptions);
            }

            return enlistment;
        }

        internal override Enlistment EnlistVolatile(
            InternalTransaction tx,
            ISinglePhaseNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction
            )
        {
            Enlistment enlistment = new Enlistment(tx, enlistmentNotification, enlistmentNotification, atomicTransaction, enlistmentOptions);

            if ((enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0)
            {
                AddVolatileEnlistment(ref tx._phase0Volatiles, enlistment);
            }
            else
            {
                AddVolatileEnlistment(ref tx._phase1Volatiles, enlistment);
            }

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionstateEnlist(enlistment.InternalEnlistment.EnlistmentTraceId, EnlistmentType.Volatile, enlistmentOptions);
            }

            return enlistment;
        }

        internal override Enlistment EnlistDurable(
            InternalTransaction tx,
            Guid resourceManagerIdentifier,
            IEnlistmentNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction
            )
        {
            throw new TransactionPromotionException(SR.Format(SR.PromoterTypeUnrecognized, tx._promoterType.ToString()),
                tx._innerException);
        }

        internal override Enlistment EnlistDurable(
            InternalTransaction tx,
            Guid resourceManagerIdentifier,
            ISinglePhaseNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction
            )
        {
            throw new TransactionPromotionException(SR.Format(SR.PromoterTypeUnrecognized, tx._promoterType.ToString()),
                tx._innerException);
        }

        internal override bool EnlistPromotableSinglePhase(
            InternalTransaction tx, IPromotableSinglePhaseNotification promotableSinglePhaseNotification,
            Transaction atomicTransaction,
            Guid promoterType
            )
        {
            // The transaction has been promoted and cannot support a promotable singe phase enlistment
            return false;
        }

        internal override void Rollback(InternalTransaction tx, Exception? e)
        {
            // Start the process for abort.  Transitioning to the Aborted state will cause
            // the tx.durableEnlistment to get aborted, which is how the non-MSDTC
            // transaction promoter will get notified of the abort.
            Debug.Assert(tx._durableEnlistment != null, "PromotedNonMSDTC state is not valid for transaction");

            if (tx._innerException == null)
            {
                tx._innerException = e;
            }

            TransactionStateAborted.EnterState(tx);
        }

        internal override Guid get_Identifier(InternalTransaction tx)
        {
            // In this state, we know that the we are dealing with a non-MSDTC promoter, so get the identifier from the internal transaction.
            return tx._distributedTransactionIdentifierNonMSDTC;
        }


        internal override void AddOutcomeRegistrant(InternalTransaction tx, TransactionCompletedEventHandler? transactionCompletedDelegate)
        {
            // Add this guy to the list of people to be notified of the outcome.
            tx._transactionCompletedDelegate = (TransactionCompletedEventHandler?)
                System.Delegate.Combine(tx._transactionCompletedDelegate, transactionCompletedDelegate);
        }


        // Start the commit processing by transitioning to TransactionStatePromotedNonMSDTCPhase0.
        internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback? asyncCallback, object? asyncState)
        {
            tx._asyncCommit = asyncCommit;
            tx._asyncCallback = asyncCallback;
            tx._asyncState = asyncState;

            TransactionStatePromotedNonMSDTCPhase0.EnterState(tx);
        }

        internal override void CompleteBlockingClone(InternalTransaction tx)
        {
            // First try to complete one of the internal blocking clones
            if (tx._phase0Volatiles._dependentClones > 0)
            {
                // decrement the number of clones
                tx._phase0Volatiles._dependentClones--;

                // Make certain we increment the right list.
                Debug.Assert(tx._phase0Volatiles._preparedVolatileEnlistments <=
                    tx._phase0Volatiles._volatileEnlistmentCount + tx._phase0Volatiles._dependentClones);

                // Check to see if all of the volatile enlistments are done.
                if (tx._phase0Volatiles._preparedVolatileEnlistments ==
                    tx._phase0VolatileWaveCount + tx._phase0Volatiles._dependentClones)
                {
                    tx.State!.Phase0VolatilePrepareDone(tx);
                }
            }
        }

        internal override void CompleteAbortingClone(InternalTransaction tx)
        {
            // A blocking clone simulates a phase 1 volatile
            //
            // Unlike a blocking clone however the aborting clones need to be accounted
            // for specifically.  So when one is complete remove it from the list.
            tx._phase1Volatiles._dependentClones--;
            Debug.Assert(tx._phase1Volatiles._dependentClones >= 0);
        }

        internal override void CreateBlockingClone(InternalTransaction tx)
        {
            // A blocking clone simulates a phase 0 volatile
            tx._phase0Volatiles._dependentClones++;
        }

        internal override void CreateAbortingClone(InternalTransaction tx)
        {
            // An aborting clone simulates a phase 1 volatile
            tx._phase1Volatiles._dependentClones++;
        }

        internal override bool ContinuePhase0Prepares()
        {
            return true;
        }

        internal override void GetObjectData(InternalTransaction tx, SerializationInfo serializationInfo, StreamingContext context)
        {
            throw new TransactionPromotionException(SR.Format(SR.PromoterTypeUnrecognized, tx._promoterType.ToString()),
                tx._innerException);
        }

        internal override void ChangeStateTransactionAborted(InternalTransaction tx, Exception? e)
        {
            // Just transition to Aborted. The PSPE will be told to rollback thru the durableEnlistment.
            // This is also overridden in TransactionStatePromotedNonMSDTCSinglePhaseCommit
            // that does something slightly differently.
            if (tx._innerException == null)
            {
                tx._innerException = e;
            }

            TransactionStateAborted.EnterState(tx);
        }

        internal override void InDoubtFromEnlistment(InternalTransaction tx)
        {
            TransactionStatePromotedNonMSDTCIndoubt.EnterState(tx);
        }

        internal override void ChangeStateAbortedDuringPromotion(InternalTransaction tx)
        {
            TransactionStateAborted.EnterState(tx);
        }

        internal override void Timeout(InternalTransaction tx)
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionTimeout(tx.TransactionTraceId);
            }

            TimeoutException e = new TimeoutException(SR.TraceTransactionTimeout);
            Rollback(tx, e);
        }

        internal override void Promote(InternalTransaction tx)
        {
            // do nothing, we are already promoted
        }

        internal override void Phase0VolatilePrepareDone(InternalTransaction tx)
        {
            // Early done notifications may come from volatiles at any time.
            // The state machine will handle all enlistments being complete in later phases.
        }

        internal override void Phase1VolatilePrepareDone(InternalTransaction tx)
        {
            // Early done notifications may come from volatiles at any time.
            // The state machine will handle all enlistments being complete in later phases.
        }

        internal override byte[] PromotedToken(InternalTransaction tx)
        {
            // Since we are in TransactionStateDelegatedNonMSDTCBase or one if its derived classes, we
            // must already be promoted. So return the InternalTransaction's promotedToken.
            Debug.Assert(tx.promotedToken != null, "InternalTransaction.promotedToken is null in TransactionStateDelegatedNonMSDTCBase or one of its derived classes.");
            return tx.promotedToken;
        }

        internal override void DisposeRoot(InternalTransaction tx)
        {
            tx.State!.Rollback(tx, null);
        }
    }

    // TransactionStatePromotedNonMSDTCPhase0
    //
    // A transaction that is in the beginning stage of committing.
    internal sealed class TransactionStatePromotedNonMSDTCPhase0 : TransactionStatePromotedNonMSDTCBase
    {
        internal override void EnterState(InternalTransaction tx)
        {
            // Set the transaction state
            CommonEnterState(tx);

            // Get a copy of the current volatile enlistment count before entering this loop so that other
            // threads don't affect the operation of this loop.
            int volatileCount = tx._phase0Volatiles._volatileEnlistmentCount;
            int dependentCount = tx._phase0Volatiles._dependentClones;

            // Store the number of phase0 volatiles for this wave.
            tx._phase0VolatileWaveCount = volatileCount;

            // Check for volatile enlistments
            if (tx._phase0Volatiles._preparedVolatileEnlistments < volatileCount + dependentCount)
            {
                // Broadcast prepare to the phase 0 enlistments
                for (int i = 0; i < volatileCount; i++)
                {
                    tx._phase0Volatiles._volatileEnlistments[i]._twoPhaseState!.ChangeStatePreparing(tx._phase0Volatiles._volatileEnlistments[i]);
                    if (!tx.State!.ContinuePhase0Prepares())
                    {
                        break;
                    }
                }
            }
            else
            {
                // No volatile enlistments.  Start phase 1.
                TransactionStatePromotedNonMSDTCVolatilePhase1.EnterState(tx);
            }
        }

        internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback? asyncCallback, object? asyncState)
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal override void Rollback(InternalTransaction tx, Exception? e)
        {
            ChangeStateTransactionAborted(tx, e);
        }

        // Volatile prepare is done for Phase0 enlistments
        internal override void Phase0VolatilePrepareDone(InternalTransaction tx)
        {
            // Check to see if any Phase0Volatiles have been added in Phase0.
            // If so go through the list again.

            // Get a copy of the current volatile enlistment count before entering this loop so that other
            // threads don't affect the operation of this loop.
            int volatileCount = tx._phase0Volatiles._volatileEnlistmentCount;
            int dependentCount = tx._phase0Volatiles._dependentClones;

            // Store the number of phase0 volatiles for this wave.
            tx._phase0VolatileWaveCount = volatileCount;

            // Check for volatile enlistments
            if (tx._phase0Volatiles._preparedVolatileEnlistments < volatileCount + dependentCount)
            {
                // Broadcast prepare to the phase 0 enlistments
                for (int i = 0; i < volatileCount; i++)
                {
                    tx._phase0Volatiles._volatileEnlistments[i]._twoPhaseState!.ChangeStatePreparing(tx._phase0Volatiles._volatileEnlistments[i]);
                    if (!tx.State!.ContinuePhase0Prepares())
                    {
                        break;
                    }
                }
            }
            else
            {
                // No volatile enlistments.  Start phase 1.
                TransactionStatePromotedNonMSDTCVolatilePhase1.EnterState(tx);
            }
        }

        internal override void Phase1VolatilePrepareDone(InternalTransaction tx)
        {
            // Ignore this for now it can be checked again in Phase 1
        }

        internal override bool ContinuePhase0Prepares()
        {
            return true;
        }
    }

    // TransactionStatePromotedNonMSDTCVolatilePhase1
    //
    // Represents the transaction state during phase 1 preparing volatile enlistments
    internal sealed class TransactionStatePromotedNonMSDTCVolatilePhase1 : TransactionStatePromotedNonMSDTCBase
    {
        internal override void EnterState(InternalTransaction tx)
        {
            // Set the transaction state
            CommonEnterState(tx);

            Debug.Assert(tx._committableTransaction != null);
            // Mark the committable transaction as complete.
            tx._committableTransaction._complete = true;

            // If at this point there are phase1 dependent clones abort the transaction
            if (tx._phase1Volatiles._dependentClones != 0)
            {
                ChangeStateTransactionAborted(tx, null);
                return;
            }

            if (tx._phase1Volatiles._volatileEnlistmentCount > 0)
            {
                // Broadcast prepare to the phase 0 enlistments
                for (int i = 0; i < tx._phase1Volatiles._volatileEnlistmentCount; i++)
                {
                    tx._phase1Volatiles._volatileEnlistments[i]._twoPhaseState!.ChangeStatePreparing(tx._phase1Volatiles._volatileEnlistments[i]);
                    if (!tx.State!.ContinuePhase1Prepares())
                    {
                        break;
                    }
                }
            }
            else
            {
                // No volatile phase 1 enlistments.  Transition to the state that will do SinglePhaseCommit to the PSPE.
                TransactionStatePromotedNonMSDTCSinglePhaseCommit.EnterState(tx);
            }
        }


        internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback? asyncCallback, object? asyncState)
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal override void Rollback(InternalTransaction tx, Exception? e)
        {
            ChangeStateTransactionAborted(tx, e);
        }

        // Volatile prepare is done for Phase1
        internal override void Phase1VolatilePrepareDone(InternalTransaction tx)
        {
            TransactionStatePromotedNonMSDTCSinglePhaseCommit.EnterState(tx);
        }

        internal override bool ContinuePhase1Prepares()
        {
            return true;
        }

        internal override Enlistment EnlistVolatile(
           InternalTransaction tx,
           IEnlistmentNotification enlistmentNotification,
           EnlistmentOptions enlistmentOptions,
           Transaction atomicTransaction
           )
        {
            throw TransactionException.Create(SR.TooLate, tx == null ? Guid.Empty : tx.DistributedTxId);
        }

        internal override Enlistment EnlistVolatile(
            InternalTransaction tx,
            ISinglePhaseNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction
            )
        {
            throw TransactionException.Create(SR.TooLate, tx == null ? Guid.Empty : tx.DistributedTxId);
        }

        internal override bool EnlistPromotableSinglePhase(
            InternalTransaction tx, IPromotableSinglePhaseNotification promotableSinglePhaseNotification,
            Transaction atomicTransaction,
            Guid promoterType
            )
        {
            throw TransactionException.Create(SR.TooLate, tx == null ? Guid.Empty : tx.DistributedTxId);
        }

        internal override void CreateBlockingClone(InternalTransaction tx)
        {
            throw TransactionException.Create(SR.TooLate, tx == null ? Guid.Empty : tx.DistributedTxId);
        }

        internal override void CreateAbortingClone(InternalTransaction tx)
        {
            throw TransactionException.Create(SR.TooLate, tx == null ? Guid.Empty : tx.DistributedTxId);
        }
    }

    // TransactionStatePromotedNonMSDTCSinglePhaseCommit
    //
    // The transaction has been delegated to a NON-MSDTC promoter and is in the process of committing.
    internal sealed class TransactionStatePromotedNonMSDTCSinglePhaseCommit : TransactionStatePromotedNonMSDTCBase
    {
        internal override void EnterState(InternalTransaction tx)
        {
            CommonEnterState(tx);

            Debug.Assert(tx._durableEnlistment != null);
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.EnlistmentStatus(tx._durableEnlistment, NotificationCall.SinglePhaseCommit);
            }

            // We are about to tell the PSPE to do the SinglePhaseCommit. It is too late for us to timeout the transaction.
            // Remove this from the timeout list
            TransactionManager.TransactionTable.Remove(tx);

            tx._durableEnlistment.State.ChangeStateCommitting(tx._durableEnlistment);
        }

        internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback? asyncCallback, object? asyncState)
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal override void Rollback(InternalTransaction tx, Exception? e)
        {
            // We have told the PSPE enlistment to do a single phase commit. It's too late to rollback.
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal override void ChangeStateTransactionCommitted(InternalTransaction tx)
        {
            // The durable enlistment must have committed.  Go to the committed state.
            TransactionStatePromotedNonMSDTCCommitted.EnterState(tx);
        }

        internal override void InDoubtFromEnlistment(InternalTransaction tx)
        {
            // The transaction is indoubt
            TransactionStatePromotedNonMSDTCIndoubt.EnterState(tx);
        }

        internal override void ChangeStateTransactionAborted(InternalTransaction tx, Exception? e)
        {
            if (tx._innerException == null)
            {
                tx._innerException = e;
            }

            // The durable enlistment must have aborted.  Go to the aborted state.
            TransactionStatePromotedNonMSDTCAborted.EnterState(tx);
        }

        internal override void ChangeStateAbortedDuringPromotion(InternalTransaction tx)
        {
            TransactionStateAborted.EnterState(tx);
        }

        internal override Enlistment EnlistVolatile(
           InternalTransaction tx,
           IEnlistmentNotification enlistmentNotification,
           EnlistmentOptions enlistmentOptions,
           Transaction atomicTransaction
           )
        {
            throw TransactionException.Create(SR.TooLate, tx == null ? Guid.Empty : tx.DistributedTxId);
        }

        internal override Enlistment EnlistVolatile(
            InternalTransaction tx,
            ISinglePhaseNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction
            )
        {
            throw TransactionException.Create(SR.TooLate, tx == null ? Guid.Empty : tx.DistributedTxId);
        }

        internal override bool EnlistPromotableSinglePhase(
            InternalTransaction tx, IPromotableSinglePhaseNotification promotableSinglePhaseNotification,
            Transaction atomicTransaction,
            Guid promoterType
            )
        {
            throw TransactionException.Create(SR.TooLate, tx == null ? Guid.Empty : tx.DistributedTxId);
        }

        internal override void CreateBlockingClone(InternalTransaction tx)
        {
            throw TransactionException.Create(SR.TooLate, tx == null ? Guid.Empty : tx.DistributedTxId);
        }

        internal override void CreateAbortingClone(InternalTransaction tx)
        {
            throw TransactionException.Create(SR.TooLate, tx == null ? Guid.Empty : tx.DistributedTxId);
        }
    }

    // TransactionStatePromotedNonMSDTCEnded
    //
    // This is a common base class for committed, aborted, and indoubt states of a non-MSDTC promoted
    // transaction.
    internal abstract class TransactionStatePromotedNonMSDTCEnded : TransactionStateEnded
    {
        internal override void EnterState(InternalTransaction tx)
        {
            base.EnterState(tx);

            CommonEnterState(tx);

            if (!ThreadPool.QueueUserWorkItem(SignalMethod, tx))
            {
                throw TransactionException.CreateInvalidOperationException(
                    TraceSourceType.TraceSourceLtm,
                    SR.UnexpectedFailureOfThreadPool,
                    null,
                    tx == null ? Guid.Empty : tx.DistributedTxId
                    );
            }
        }

        internal override void AddOutcomeRegistrant(InternalTransaction tx, TransactionCompletedEventHandler? transactionCompletedDelegate)
        {
            if (transactionCompletedDelegate != null)
            {
                TransactionEventArgs args = new TransactionEventArgs();
                args._transaction = tx._outcomeSource.InternalClone();
                transactionCompletedDelegate(args._transaction, args);
            }
        }

        internal override void EndCommit(InternalTransaction tx)
        {
            // Test the outcome of the transaction and respond accordingly.
            PromotedTransactionOutcome(tx);
        }

        internal override void CompleteBlockingClone(InternalTransaction tx)
        {
            // The transaction is finished ignore these.
        }

        internal override void CompleteAbortingClone(InternalTransaction tx)
        {
            // The transaction is finished ignore these.
        }

        internal override void CreateBlockingClone(InternalTransaction tx)
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal override void CreateAbortingClone(InternalTransaction tx)
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }

        internal override Guid get_Identifier(InternalTransaction tx)
        {
            // In this state, we know that the we are dealing with a non-MSDTC promoter, so get the identifier from the internal transaction.
            return tx._distributedTransactionIdentifierNonMSDTC;
        }

        internal override void Promote(InternalTransaction tx)
        {
            // do nothing, we are already promoted
        }

        protected abstract void PromotedTransactionOutcome(InternalTransaction tx);

        private static WaitCallback? s_signalMethod;
        private static WaitCallback SignalMethod => LazyInitializer.EnsureInitialized(ref s_signalMethod, ref s_classSyncObject, () => new WaitCallback(SignalCallback!));

        private static void SignalCallback(object state)
        {
            InternalTransaction tx = (InternalTransaction)state;
            lock (tx)
            {
                tx.SignalAsyncCompletion();
            }
        }
    }

    // TransactionStatePromotedNonMSDTCAborted
    //
    // This state indicates that the transaction has been promoted to a non-MSDTC promoter and the outcome
    // of the transaction is aborted.
    internal sealed class TransactionStatePromotedNonMSDTCAborted : TransactionStatePromotedNonMSDTCEnded
    {
        internal override void EnterState(InternalTransaction tx)
        {
            base.EnterState(tx);

            // Notify the enlistments that the transaction has aborted
            for (int i = 0; i < tx._phase0Volatiles._volatileEnlistmentCount; i++)
            {
                tx._phase0Volatiles._volatileEnlistments[i]._twoPhaseState!.InternalAborted(tx._phase0Volatiles._volatileEnlistments[i]);
            }

            for (int i = 0; i < tx._phase1Volatiles._volatileEnlistmentCount; i++)
            {
                tx._phase1Volatiles._volatileEnlistments[i]._twoPhaseState!.InternalAborted(tx._phase1Volatiles._volatileEnlistments[i]);
            }

            // Notify the durable enlistment
            if (tx._durableEnlistment != null)
            {
                tx._durableEnlistment.State.InternalAborted(tx._durableEnlistment);
            }

            // Fire Completion for anyone listening
            tx.FireCompletion();
            // We don't need to do the AsyncCompletion stuff.  If it was needed, it was done out of SignalCallback.

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionAborted(tx.TransactionTraceId);
            }
        }

        internal override TransactionStatus get_Status(InternalTransaction tx)
        {
            return TransactionStatus.Aborted;
        }

        internal override void Rollback(InternalTransaction tx, Exception? e)
        {
            // Already done.
        }

        internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback? asyncCallback, object? asyncState)
        {
            throw TransactionAbortedException.Create(SR.TransactionAborted, tx._innerException, tx.DistributedTxId);
        }

        internal override void CreateBlockingClone(InternalTransaction tx)
        {
            throw TransactionAbortedException.Create(SR.TransactionAborted, tx._innerException, tx.DistributedTxId);
        }

        internal override void CreateAbortingClone(InternalTransaction tx)
        {
            throw TransactionAbortedException.Create(SR.TransactionAborted, tx._innerException, tx.DistributedTxId);
        }

        internal override void Phase0VolatilePrepareDone(InternalTransaction tx)
        {
            // Since the transaction is aborted ignore it.
        }

        internal override void Phase1VolatilePrepareDone(InternalTransaction tx)
        {
            // Since the transaction is aborted ignore it.
        }

        internal override void ChangeStateTransactionAborted(InternalTransaction tx, Exception? e)
        {
            // This may come from a promotable single phase enlistments abort response.
        }

        protected override void PromotedTransactionOutcome(InternalTransaction tx)
        {
            if ((null == tx._innerException) && (null != tx.PromotedTransaction))
            {
                tx._innerException = tx.PromotedTransaction.InnerException;
            }
            throw TransactionAbortedException.Create(SR.TransactionAborted, tx._innerException, tx.DistributedTxId);
        }

        internal override void CheckForFinishedTransaction(InternalTransaction tx)
        {
            throw new TransactionAbortedException(tx._innerException, tx.DistributedTxId);
        }

        internal override void GetObjectData(InternalTransaction tx, SerializationInfo serializationInfo, StreamingContext context)
        {
            throw TransactionAbortedException.Create(SR.TransactionAborted, tx._innerException, tx.DistributedTxId);
        }
    }

    // TransactionStatePromotedNonMSDTCCommitted
    //
    // This state indicates that the transaction has been non-MSDTC promoted and the outcome
    // of the transaction is committed
    internal sealed class TransactionStatePromotedNonMSDTCCommitted : TransactionStatePromotedNonMSDTCEnded
    {
        internal override void EnterState(InternalTransaction tx)
        {
            base.EnterState(tx);

            // Notify the phase 0 enlistments that the transaction has committed
            for (int i = 0; i < tx._phase0Volatiles._volatileEnlistmentCount; i++)
            {
                tx._phase0Volatiles._volatileEnlistments[i]._twoPhaseState!.InternalCommitted(tx._phase0Volatiles._volatileEnlistments[i]);
            }

            // Notify the phase 1 enlistments that the transaction has committed
            for (int i = 0; i < tx._phase1Volatiles._volatileEnlistmentCount; i++)
            {
                tx._phase1Volatiles._volatileEnlistments[i]._twoPhaseState!.InternalCommitted(tx._phase1Volatiles._volatileEnlistments[i]);
            }

            // Fire Completion for anyone listening
            tx.FireCompletion();
            // We don't need to do the AsyncCompletion stuff.  If it was needed, it was done out of SignalCallback.

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionCommitted(tx.TransactionTraceId);
            }
        }

        internal override TransactionStatus get_Status(InternalTransaction tx)
        {
            return TransactionStatus.Committed;
        }

        protected override void PromotedTransactionOutcome(InternalTransaction tx)
        {
            // This is a happy transaction.
        }
    }

    // TransactionStatePromotedNonMSDTCIndoubt
    //
    // This state indicates that the transaction has been non-MSDTC promoted but the outcome
    // of the transaction is indoubt.
    internal sealed class TransactionStatePromotedNonMSDTCIndoubt : TransactionStatePromotedNonMSDTCEnded
    {
        internal override void EnterState(InternalTransaction tx)
        {
            base.EnterState(tx);

            // Notify the phase 0 enlistments that the transaction is indoubt
            for (int i = 0; i < tx._phase0Volatiles._volatileEnlistmentCount; i++)
            {
                tx._phase0Volatiles._volatileEnlistments[i]._twoPhaseState!.InternalIndoubt(tx._phase0Volatiles._volatileEnlistments[i]);
            }

            // Notify the phase 1 enlistments that the transaction is indoubt
            for (int i = 0; i < tx._phase1Volatiles._volatileEnlistmentCount; i++)
            {
                tx._phase1Volatiles._volatileEnlistments[i]._twoPhaseState!.InternalIndoubt(tx._phase1Volatiles._volatileEnlistments[i]);
            }

            // Fire Completion for anyone listening
            tx.FireCompletion();
            // We don't need to do the AsyncCompletion stuff.  If it was needed, it was done out of SignalCallback.

            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionInDoubt(tx.TransactionTraceId);
            }
        }

        internal override TransactionStatus get_Status(InternalTransaction tx)
        {
            return TransactionStatus.InDoubt;
        }

        internal override void ChangeStatePromotedPhase0(InternalTransaction tx)
        {
            throw TransactionInDoubtException.Create(TraceSourceType.TraceSourceBase, SR.TransactionIndoubt, tx._innerException, tx.DistributedTxId);
        }

        internal override void ChangeStatePromotedPhase1(InternalTransaction tx)
        {
            throw TransactionInDoubtException.Create(TraceSourceType.TraceSourceBase, SR.TransactionIndoubt, tx._innerException, tx.DistributedTxId);
        }

        protected override void PromotedTransactionOutcome(InternalTransaction tx)
        {
            if ((null == tx._innerException) && (null != tx.PromotedTransaction))
            {
                tx._innerException = tx.PromotedTransaction.InnerException;
            }
            throw TransactionInDoubtException.Create(TraceSourceType.TraceSourceBase, SR.TransactionIndoubt, tx._innerException, tx.DistributedTxId);
        }

        internal override void CheckForFinishedTransaction(InternalTransaction tx)
        {
            throw TransactionInDoubtException.Create(TraceSourceType.TraceSourceBase, SR.TransactionIndoubt, tx._innerException, tx.DistributedTxId);
        }

        internal override void GetObjectData(InternalTransaction tx, SerializationInfo serializationInfo, StreamingContext context)
        {
            throw TransactionInDoubtException.Create(TraceSourceType.TraceSourceBase, SR.TransactionIndoubt, tx._innerException, tx.DistributedTxId);
        }

        internal override void CreateBlockingClone(InternalTransaction tx)
        {
            throw TransactionInDoubtException.Create(SR.TransactionAborted, tx._innerException, tx.DistributedTxId);
        }

        internal override void CreateAbortingClone(InternalTransaction tx)
        {
            throw TransactionInDoubtException.Create(SR.TransactionAborted, tx._innerException, tx.DistributedTxId);
        }
    }

    // TransactionStateDelegatedNonMSDTC
    //
    // This state is the base state for delegated transactions to non-MSDTC promoters.
    internal sealed class TransactionStateDelegatedNonMSDTC : TransactionStatePromotedNonMSDTCBase
    {
        internal override void EnterState(InternalTransaction tx)
        {
            // Assign the state
            CommonEnterState(tx);

            // We are never going to have an DistributedTransaction for this one.
            DistributedTransaction? distributedTx = null;
            try
            {
                // Ask the delegation interface to promote the transaction.
                if (tx._durableEnlistment != null)
                {
                    TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                    if (etwLog.IsEnabled())
                    {
                        etwLog.EnlistmentStatus(tx._durableEnlistment, NotificationCall.Promote);
                    }
                }


                distributedTx = TransactionStatePSPEOperation.PSPEPromote(tx);
                Debug.Assert((distributedTx == null), "PSPEPromote for non-MSDTC promotion returned a distributed transaction.");
                Debug.Assert((tx.promotedToken != null), "PSPEPromote for non-MSDTC promotion did not set InternalTransaction.PromotedToken.");
            }
            catch (TransactionPromotionException e)
            {
                tx._innerException = e;
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.ExceptionConsumed(e);
                }
            }
            finally
            {
                if (tx.promotedToken == null)
                {
                    // There was an exception trying to promote the transaction.
                    tx.State!.ChangeStateAbortedDuringPromotion(tx);
                }
            }
        }
    }

    // TransactionStateDelegatedSubordinate
    //
    // This state represents a transaction that is subordinate to another TM and has been
    // promoted.
    internal sealed class TransactionStateDelegatedSubordinate : TransactionStateDelegatedBase
    {
        internal override bool PromoteDurable(InternalTransaction tx)
        {
            return true;
        }


        internal override void Rollback(InternalTransaction tx, Exception? e)
        {
            // Pass the Rollback through the promotable single phase enlistment to be
            // certain it is notified.

            if (tx._innerException == null)
            {
                tx._innerException = e;
            }

            Debug.Assert(tx.PromotedTransaction != null);
            tx.PromotedTransaction.Rollback();
            TransactionStatePromotedAborted.EnterState(tx);
        }


        internal override void ChangeStatePromotedPhase0(InternalTransaction tx)
        {
            TransactionStatePromotedPhase0.EnterState(tx);
        }


        internal override void ChangeStatePromotedPhase1(InternalTransaction tx)
        {
            TransactionStatePromotedPhase1.EnterState(tx);
        }
    }


    // TransactionStatePSPEOperation
    //
    // Someone is trying to enlist for promotable single phase.
    // Don't allow anything that is not supported.
    internal sealed class TransactionStatePSPEOperation : TransactionState
    {
        internal override void EnterState(InternalTransaction tx)
        {
            // No one should ever use this particular version.  It has to be overridden because
            // the base is abstract.
            throw new InvalidOperationException();
        }


        internal override TransactionStatus get_Status(InternalTransaction tx)
        {
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }


        internal void PSPEInitialize(
            InternalTransaction tx,
            IPromotableSinglePhaseNotification promotableSinglePhaseNotification,
            Guid promoterType)
        {
            Debug.Assert(tx.State == TransactionStateActive, "PSPEPromote called from state other than TransactionStateActive");
            CommonEnterState(tx);

            try
            {
                // Try to initialize the pspn.  If an exception is thrown let it propigate
                // all the way up to the caller.
                promotableSinglePhaseNotification.Initialize();

                // Set the PromoterType for the transaction.
                tx._promoterType = promoterType;
            }
            finally
            {
                TransactionStateActive.CommonEnterState(tx);
            }
        }

        // This method will call the initialize method on IPromotableSinglePhaseNotification.
        // The tx state will be set to TransactionStatePhase0 to receive and process further
        // enlistments during Phase0.

        internal void Phase0PSPEInitialize(
            InternalTransaction tx,
            IPromotableSinglePhaseNotification promotableSinglePhaseNotification,
            Guid promoterType)
        {
            Debug.Assert(tx.State == TransactionStatePhase0, "Phase0PSPEInitialize called from state other than TransactionStatePhase0");
            CommonEnterState(tx);

            try
            {
                // Try to initialize the PSPE.  If an exception is thrown let it propagate
                // all the way up to the caller.
                promotableSinglePhaseNotification.Initialize();

                // Set the PromoterType for the transaction.
                tx._promoterType = promoterType;
            }
            finally
            {
                TransactionStatePhase0.CommonEnterState(tx);
            }
        }

        internal DistributedTransaction? PSPEPromote(InternalTransaction tx)
        {
            bool changeToReturnState = true;

            TransactionState? returnState = tx.State;
            Debug.Assert(returnState == TransactionStateDelegated ||
                returnState == TransactionStateDelegatedSubordinate ||
                returnState == TransactionStateDelegatedNonMSDTC,
                "PSPEPromote called from state other than TransactionStateDelegated[NonMSDTC]");
            CommonEnterState(tx);

            DistributedTransaction? distributedTx = null;
            try
            {
                if (tx._attemptingPSPEPromote)
                {
                    // There should not already be a PSPEPromote call outstanding.
                    throw TransactionException.CreateInvalidOperationException(
                            TraceSourceType.TraceSourceLtm,
                            SR.PromotedReturnedInvalidValue,
                            null,
                            tx.DistributedTxId
                            );
                }
                tx._attemptingPSPEPromote = true;

                Debug.Assert(tx._promoter != null);
                byte[]? propagationToken = tx._promoter.Promote();

                // If the PromoterType is NOT MSDTC, then we can't assume that the returned
                // byte[] is an MSDTC propagation token and we can't create an DistributedTransaction from it.
                if (tx._promoterType != TransactionInterop.PromoterTypeDtc)
                {
                    if (propagationToken == null)
                    {
                        throw TransactionException.CreateInvalidOperationException(
                                TraceSourceType.TraceSourceLtm,
                                SR.PromotedReturnedInvalidValue,
                                null,
                                tx.DistributedTxId
                                );
                    }

                    tx.promotedToken = propagationToken;
                    return null;
                }

                // From this point forward, we know that the PromoterType is TransactionInterop.PromoterTypeDtc so we can
                // treat the propagationToken as an MSDTC propagation token. If one was returned.
                if (propagationToken == null)
                {
                    // If the returned propagationToken is null AND the tx.PromotedTransaction is null, the promote failed.
                    // But if the PSPE promoter used PSPEPromoteAndConvertToEnlistDurable, tx.PromotedTransaction will NOT be null
                    // at this point and we just use tx.PromotedTransaction as distributedTx and we don't bother to change to the
                    // "return state" because the transaction is already in the state it needs to be in.
                    if (tx.PromotedTransaction == null)
                    {
                        // The PSPE has returned an invalid promoted transaction.
                        throw TransactionException.CreateInvalidOperationException(
                                TraceSourceType.TraceSourceLtm,
                                SR.PromotedReturnedInvalidValue,
                                null,
                                tx.DistributedTxId
                                );
                    }
                    // The transaction has already transitioned to TransactionStatePromoted, so we don't want
                    // to change the state to the "returnState" because TransactionStateDelegatedBase.EnterState, would
                    // try to promote the enlistments again.
                    changeToReturnState = false;
                    distributedTx = tx.PromotedTransaction;
                }

                // At this point, if we haven't yet set distributedTx, we need to get it using the returned
                // propagation token. The PSPE promoter must NOT have used PSPEPromoteAndConvertToEnlistDurable.
                if (distributedTx == null)
                {
                    try
                    {
                        distributedTx = TransactionInterop.GetDistributedTransactionFromTransmitterPropagationToken(
                                            propagationToken!
                                            );
                    }
                    catch (ArgumentException e)
                    {
                        // The PSPE has returned an invalid promoted transaction.
                        throw TransactionException.CreateInvalidOperationException(
                                TraceSourceType.TraceSourceLtm,
                                SR.PromotedReturnedInvalidValue,
                                e,
                                tx.DistributedTxId
                                );
                    }

                    if (TransactionManager.FindPromotedTransaction(distributedTx.Identifier) != null)
                    {
                        // If there is already a promoted transaction then someone has committed an error.
                        distributedTx.Dispose();
                        throw TransactionException.CreateInvalidOperationException(
                                TraceSourceType.TraceSourceLtm,
                                SR.PromotedTransactionExists,
                                null,
                                tx.DistributedTxId
                                );
                    }
                }
            }
            finally
            {
                tx._attemptingPSPEPromote = false;
                // If we get here and changeToReturnState is false, the PSPE enlistment must have requested that we
                // promote and convert the enlistment to a durable enlistment
                // (Transaction.PSPEPromoteAndConvertToEnlistDurable). In that case, the internal transaction is
                // already in TransactionStatePromoted, so we don't want to put it BACK into TransactionStateDelegatedBase.
                if (changeToReturnState)
                {
                    returnState.CommonEnterState(tx);
                }
            }

            return distributedTx;
        }

        internal override Enlistment PromoteAndEnlistDurable(
            InternalTransaction tx,
            Guid resourceManagerIdentifier,
            IPromotableSinglePhaseNotification promotableNotification,
            ISinglePhaseNotification enlistmentNotification,
            EnlistmentOptions enlistmentOptions,
            Transaction atomicTransaction
            )
        {
            // This call is only allowed if we have an outstanding call to ITransactionPromoter.Promote.
            if (!tx._attemptingPSPEPromote)
            {
                throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
            }

            if (promotableNotification != tx._promoter)
            {
                throw TransactionException.CreateInvalidOperationException(
                        TraceSourceType.TraceSourceLtm,
                        SR.InvalidIPromotableSinglePhaseNotificationSpecified,
                        null,
                        tx.DistributedTxId
                        );
            }

            Enlistment enlistment;

            // First promote the transaction. We do this by simply changing the state of the transaction to Promoted.
            // In TransactionStateActive.EnlistPromotableSinglePhase, tx.durableEnlistment was set to point at the InternalEnlistment
            // for that PSPE enlistment. We are going to replace that with a "true" durable enlistment here. But we need to
            // set tx.durableEnlistment to null BEFORE we promote because if we don't the promotion will attempt to promote
            // the tx.durableEnlistment. Because we are doing the EnlistDurable AFTER promotion, it will be a "promoted"
            // durable enlistment and we can safely set tx.durableEnlistment to the InternalEnlistment of that Enlistment.
            tx._durableEnlistment = null;
            tx._promoteState = TransactionState.TransactionStatePromoted;
            tx._promoteState.EnterState(tx);

            // Now we need to create the durable enlistment that will replace the PSPE enlistment. Use the internalEnlistment of
            // this newly created durable enlistment as the tx.durableEnlistment.
            enlistment = tx.State!.EnlistDurable(tx, resourceManagerIdentifier, enlistmentNotification, enlistmentOptions, atomicTransaction);
            tx._durableEnlistment = enlistment.InternalEnlistment;

            return enlistment;
        }

        // TransactionStatePSPEOperation is the only state where this is allowed and we further check to make sure there is
        // an outstanding call to ITransactionPromoter.Promote and that the specified promotableNotification matches the
        // transaction's promoter object.
        internal override void SetDistributedTransactionId(InternalTransaction tx,
                    IPromotableSinglePhaseNotification promotableNotification,
                    Guid distributedTransactionIdentifier)
        {
            // This call is only allowed if we have an outstanding call to ITransactionPromoter.Promote.
            if (!tx._attemptingPSPEPromote)
            {
                throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
            }

            if (promotableNotification != tx._promoter)
            {
                throw TransactionException.CreateInvalidOperationException(
                        TraceSourceType.TraceSourceLtm,
                        SR.InvalidIPromotableSinglePhaseNotificationSpecified,
                        null,
                        tx.DistributedTxId
                        );
            }

            tx._distributedTransactionIdentifierNonMSDTC = distributedTransactionIdentifier;
        }
    }


    // TransactionStateDelegatedP0Wave
    //
    // This state is exactly the same as TransactionStatePromotedP0Wave with
    // the exception that when commit is restarted it is restarted in a different
    // way.
    internal sealed class TransactionStateDelegatedP0Wave : TransactionStatePromotedP0Wave
    {
        internal override void Phase0VolatilePrepareDone(InternalTransaction tx)
        {
            TransactionStateDelegatedCommitting.EnterState(tx);
        }
    }


    // TransactionStateDelegatedCommitting
    //
    // The transaction has been promoted but is in the process of committing.
    internal sealed class TransactionStateDelegatedCommitting : TransactionStatePromotedCommitting
    {
        internal override void EnterState(InternalTransaction tx)
        {
            CommonEnterState(tx);

            // Forward this on to the promotable single phase enlisment
            Monitor.Exit(tx);

            Debug.Assert(tx._durableEnlistment != null);
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.EnlistmentStatus(tx._durableEnlistment, NotificationCall.SinglePhaseCommit);
            }

            try
            {
                tx._durableEnlistment.PromotableSinglePhaseNotification.SinglePhaseCommit(
                    tx._durableEnlistment.SinglePhaseEnlistment);
            }
            finally
            {
                Monitor.Enter(tx);
            }
        }
    }


    // TransactionStateDelegatedAborting
    //
    // The transaction has been promoted but is in the process of committing.
    internal sealed class TransactionStateDelegatedAborting : TransactionStatePromotedAborted
    {
        internal override void EnterState(InternalTransaction tx)
        {
            CommonEnterState(tx);

            // The distributed TM is driving the commit processing, so marking of complete
            // is done in TransactionStatePromotedPhase0Aborting.EnterState or
            // TransactionStatePromotedPhase1Aborting.EnterState.

            // Release the lock
            Monitor.Exit(tx);
            try
            {
                Debug.Assert(tx._durableEnlistment != null);
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.EnlistmentStatus(tx._durableEnlistment, NotificationCall.Rollback);
                }

                tx._durableEnlistment.PromotableSinglePhaseNotification.Rollback(
                    tx._durableEnlistment.SinglePhaseEnlistment);
            }
            finally
            {
                Monitor.Enter(tx);
            }
        }


        internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback? asyncCallback, object? asyncState)
        {
            // Initiate the commit process.
            throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
        }


        internal override void ChangeStatePromotedAborted(InternalTransaction tx)
        {
            TransactionStatePromotedAborted.EnterState(tx);
        }
    }
}
