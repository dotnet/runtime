// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;

namespace System.Transactions
{
    internal delegate void FinishVolatileDelegate(InternalEnlistment enlistment);

    // Base class for all volatile enlistment states
    internal abstract class VolatileEnlistmentState : EnlistmentState
    {
        private static VolatileEnlistmentActive? s_volatileEnlistmentActive;
        private static VolatileEnlistmentPreparing? s_volatileEnlistmentPreparing;
        private static VolatileEnlistmentPrepared? s_volatileEnlistmentPrepared;
        private static VolatileEnlistmentSPC? s_volatileEnlistmentSPC;
        private static VolatileEnlistmentPreparingAborting? s_volatileEnlistmentPreparingAborting;
        private static VolatileEnlistmentAborting? s_volatileEnlistmentAborting;
        private static VolatileEnlistmentCommitting? s_volatileEnlistmentCommitting;
        private static VolatileEnlistmentInDoubt? s_volatileEnlistmentInDoubt;
        private static VolatileEnlistmentEnded? s_volatileEnlistmentEnded;
        private static VolatileEnlistmentDone? s_volatileEnlistmentDone;

        // Object for synchronizing access to the entire class( avoiding lock( typeof( ... )) )
        private static object? s_classSyncObject;

        internal static VolatileEnlistmentActive VolatileEnlistmentActive =>
            LazyInitializer.EnsureInitialized(ref s_volatileEnlistmentActive, ref s_classSyncObject, () => new VolatileEnlistmentActive());


        protected static VolatileEnlistmentPreparing VolatileEnlistmentPreparing =>
            LazyInitializer.EnsureInitialized(ref s_volatileEnlistmentPreparing, ref s_classSyncObject, () => new VolatileEnlistmentPreparing());


        protected static VolatileEnlistmentPrepared VolatileEnlistmentPrepared =>
            LazyInitializer.EnsureInitialized(ref s_volatileEnlistmentPrepared, ref s_classSyncObject, () => new VolatileEnlistmentPrepared());


        protected static VolatileEnlistmentSPC VolatileEnlistmentSPC =>
            LazyInitializer.EnsureInitialized(ref s_volatileEnlistmentSPC, ref s_classSyncObject, () => new VolatileEnlistmentSPC());


        protected static VolatileEnlistmentPreparingAborting VolatileEnlistmentPreparingAborting =>
            LazyInitializer.EnsureInitialized(ref s_volatileEnlistmentPreparingAborting, ref s_classSyncObject, () => new VolatileEnlistmentPreparingAborting());


        protected static VolatileEnlistmentAborting VolatileEnlistmentAborting =>
            LazyInitializer.EnsureInitialized(ref s_volatileEnlistmentAborting, ref s_classSyncObject, () => new VolatileEnlistmentAborting());


        protected static VolatileEnlistmentCommitting VolatileEnlistmentCommitting =>
            LazyInitializer.EnsureInitialized(ref s_volatileEnlistmentCommitting, ref s_classSyncObject, () => new VolatileEnlistmentCommitting());


        protected static VolatileEnlistmentInDoubt VolatileEnlistmentInDoubt =>
            LazyInitializer.EnsureInitialized(ref s_volatileEnlistmentInDoubt, ref s_classSyncObject, () => new VolatileEnlistmentInDoubt());


        protected static VolatileEnlistmentEnded VolatileEnlistmentEnded =>
            LazyInitializer.EnsureInitialized(ref s_volatileEnlistmentEnded, ref s_classSyncObject, () => new VolatileEnlistmentEnded());


        protected static VolatileEnlistmentDone VolatileEnlistmentDone =>
            LazyInitializer.EnsureInitialized(ref s_volatileEnlistmentDone, ref s_classSyncObject, () => new VolatileEnlistmentDone());


        // Override of get_RecoveryInformation to be more specific with the exception string.
        internal override byte[] RecoveryInformation(InternalEnlistment enlistment)
        {
            throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceLtm,
                SR.VolEnlistNoRecoveryInfo, null, enlistment == null ? Guid.Empty : enlistment.DistributedTxId);
        }
    }

    // Active state for a volatile enlistment indicates that the enlistment has been created
    // but no one has begun committing or aborting the transaction.  From this state the enlistment
    // can abort the transaction or call read only to indicate that it does not want to
    // participate further in the transaction.
    internal sealed class VolatileEnlistmentActive : VolatileEnlistmentState
    {
        internal override void EnterState(InternalEnlistment enlistment)
        {
            // Set the enlistment state
            enlistment.State = this;

            // Yeah it's active.
        }

        #region IEnlistment Related Events

        internal override void EnlistmentDone(InternalEnlistment enlistment)
        {
            // End this enlistment
            VolatileEnlistmentDone.EnterState(enlistment);

            // Note another enlistment finished.
            enlistment.FinishEnlistment();
        }

        #endregion

        #region State Change Events

        internal override void ChangeStatePreparing(InternalEnlistment enlistment)
        {
            VolatileEnlistmentPreparing.EnterState(enlistment);
        }


        internal override void ChangeStateSinglePhaseCommit(InternalEnlistment enlistment)
        {
            VolatileEnlistmentSPC.EnterState(enlistment);
        }


        #endregion

        #region Internal Events

        internal override void InternalAborted(InternalEnlistment enlistment)
        {
            // Change the enlistment state to aborting.
            VolatileEnlistmentAborting.EnterState(enlistment);
        }

        #endregion
    }

    // Preparing state is the time after prepare has been called but no response has been received.
    internal sealed class VolatileEnlistmentPreparing : VolatileEnlistmentState
    {
        internal override void EnterState(InternalEnlistment enlistment)
        {
            // Set the enlistment state
            enlistment.State = this;

            Monitor.Exit(enlistment.Transaction);
            try // Don't hold this lock while calling into the application code.
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.EnlistmentStatus(enlistment, NotificationCall.Prepare);
                }

                Debug.Assert(enlistment.EnlistmentNotification != null);
                enlistment.EnlistmentNotification.Prepare(enlistment.PreparingEnlistment);
            }
            finally
            {
                Monitor.Enter(enlistment.Transaction);
            }
        }

        internal override void EnlistmentDone(InternalEnlistment enlistment)
        {
            VolatileEnlistmentDone.EnterState(enlistment);

            // Process Finished InternalEnlistment
            enlistment.FinishEnlistment();
        }

        internal override void Prepared(InternalEnlistment enlistment)
        {
            // Change the enlistments state to prepared.
            VolatileEnlistmentPrepared.EnterState(enlistment);

            // Process Finished InternalEnlistment
            enlistment.FinishEnlistment();
        }

        // The enlistment says to abort start the abort sequence.
        internal override void ForceRollback(InternalEnlistment enlistment, Exception? e)
        {
            // Change enlistment state to aborting
            VolatileEnlistmentEnded.EnterState(enlistment);

            Debug.Assert(enlistment.Transaction.State != null);
            // Start the transaction aborting
            enlistment.Transaction.State.ChangeStateTransactionAborted(enlistment.Transaction, e);

            // Process Finished InternalEnlistment
            enlistment.FinishEnlistment();
        }

        internal override void ChangeStatePreparing(InternalEnlistment enlistment)
        {
            // If the transaction promotes during phase 0 then the transition to
            // the promoted phase 0 state for the transaction may cause this
            // notification to be delivered again.  So in this case it should be
            // ignored.
        }

        internal override void InternalAborted(InternalEnlistment enlistment)
        {
            VolatileEnlistmentPreparingAborting.EnterState(enlistment);
        }
    }

    // SPC state for a volatile enlistment is the point at which there is exactly 1 enlisment
    // and it supports SPC.  The TM will send a single phase commit to the enlistment and wait
    // for the response from the TM.
    internal sealed class VolatileEnlistmentSPC : VolatileEnlistmentState
    {
        internal override void EnterState(InternalEnlistment enlistment)
        {
            bool spcCommitted = false;
            // Set the enlistment state
            enlistment.State = this;

            // Send Single Phase Commit to the enlistment
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.EnlistmentStatus(enlistment, NotificationCall.SinglePhaseCommit);
            }

            Monitor.Exit(enlistment.Transaction);
            try // Don't hold this lock while calling into the application code.
            {
                Debug.Assert(enlistment.SinglePhaseNotification != null);
                enlistment.SinglePhaseNotification.SinglePhaseCommit(enlistment.SinglePhaseEnlistment);
                spcCommitted = true;
            }
            finally
            {
                if (!spcCommitted)
                {
                    //If we have an exception thrown in SPC, we don't know the if the enlistment is committed or not
                    //reply indoubt
                    enlistment.SinglePhaseEnlistment.InDoubt();
                }
                Monitor.Enter(enlistment.Transaction);
            }
        }

        internal override void EnlistmentDone(InternalEnlistment enlistment)
        {
            VolatileEnlistmentEnded.EnterState(enlistment);
            Debug.Assert(enlistment.Transaction.State != null);
            enlistment.Transaction.State.ChangeStateTransactionCommitted(enlistment.Transaction);
        }

        internal override void Committed(InternalEnlistment enlistment)
        {
            VolatileEnlistmentEnded.EnterState(enlistment);
            Debug.Assert(enlistment.Transaction.State != null);
            enlistment.Transaction.State.ChangeStateTransactionCommitted(enlistment.Transaction);
        }

        internal override void Aborted(InternalEnlistment enlistment, Exception? e)
        {
            VolatileEnlistmentEnded.EnterState(enlistment);

            Debug.Assert(enlistment.Transaction.State != null);
            enlistment.Transaction.State.ChangeStateTransactionAborted(enlistment.Transaction, e);
        }

        internal override void InDoubt(InternalEnlistment enlistment, Exception? e)
        {
            VolatileEnlistmentEnded.EnterState(enlistment);

            enlistment.Transaction._innerException ??= e;

            Debug.Assert(enlistment.Transaction.State != null);
            enlistment.Transaction.State.InDoubtFromEnlistment(enlistment.Transaction);
        }
    }

    // Prepared state for a volatile enlistment is the point at which prepare has been called
    // and the enlistment has responded prepared.  No enlistment operations are valid at this
    // point.  The RM must wait for the TM to take the next action.
    internal sealed class VolatileEnlistmentPrepared : VolatileEnlistmentState
    {
        internal override void EnterState(InternalEnlistment enlistment)
        {
            // Set the enlistment state
            enlistment.State = this;

            // Wait for Committed
        }

        internal override void InternalAborted(InternalEnlistment enlistment)
        {
            VolatileEnlistmentAborting.EnterState(enlistment);
        }

        internal override void InternalCommitted(InternalEnlistment enlistment)
        {
            VolatileEnlistmentCommitting.EnterState(enlistment);
        }

        internal override void InternalIndoubt(InternalEnlistment enlistment)
        {
            // Change the enlistment state to InDoubt.
            VolatileEnlistmentInDoubt.EnterState(enlistment);
        }

        internal override void ChangeStatePreparing(InternalEnlistment enlistment)
        {
            // This would happen in the second pass of a phase 0 wave.
        }
    }

    // Aborting state is when Rollback has been sent to the enlistment.
    internal sealed class VolatileEnlistmentPreparingAborting : VolatileEnlistmentState
    {
        internal override void EnterState(InternalEnlistment enlistment)
        {
            // Set the enlistment state
            enlistment.State = this;
        }

        internal override void EnlistmentDone(InternalEnlistment enlistment)
        {
            // Move this enlistment to the ended state
            VolatileEnlistmentEnded.EnterState(enlistment);
        }

        internal override void Prepared(InternalEnlistment enlistment)
        {
            // The enlistment has respondend so changes it's state to aborting.
            VolatileEnlistmentAborting.EnterState(enlistment);

            // Process Finished InternalEnlistment
            enlistment.FinishEnlistment();
        }

        // The enlistment says to abort start the abort sequence.
        internal override void ForceRollback(InternalEnlistment enlistment, Exception? e)
        {
            // Change enlistment state to aborting
            VolatileEnlistmentEnded.EnterState(enlistment);

            // Record the exception in the transaction
            if (enlistment.Transaction._innerException == null)
            {
                // Arguably this is the second call to ForceRollback and not the call that
                // aborted the transaction but just in case.
                enlistment.Transaction._innerException = e;
            }

            // Process Finished InternalEnlistment
            enlistment.FinishEnlistment();
        }

        internal override void InternalAborted(InternalEnlistment enlistment)
        {
            // If this event comes from multiple places just ignore it.  Continue
            // waiting for the enlistment to respond so that we can respond to it.
        }
    }

    // Aborting state is when Rollback has been sent to the enlistment.
    internal sealed class VolatileEnlistmentAborting : VolatileEnlistmentState
    {
        internal override void EnterState(InternalEnlistment enlistment)
        {
            // Set the enlistment state
            enlistment.State = this;

            Monitor.Exit(enlistment.Transaction);
            try // Don't hold this lock while calling into the application code.
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.EnlistmentStatus(enlistment, NotificationCall.Rollback);
                }

                Debug.Assert(enlistment.EnlistmentNotification != null);
                enlistment.EnlistmentNotification.Rollback(enlistment.SinglePhaseEnlistment);
            }
            finally
            {
                Monitor.Enter(enlistment.Transaction);
            }
        }

        internal override void ChangeStatePreparing(InternalEnlistment enlistment)
        {
            // This enlistment was told to abort before being told to prepare
        }

        internal override void EnlistmentDone(InternalEnlistment enlistment)
        {
            // Move this enlistment to the ended state
            VolatileEnlistmentEnded.EnterState(enlistment);
        }

        internal override void InternalAborted(InternalEnlistment enlistment)
        {
            // Already working on it.
        }
    }

    // Committing state is when Commit has been sent to the enlistment.
    internal sealed class VolatileEnlistmentCommitting : VolatileEnlistmentState
    {
        internal override void EnterState(InternalEnlistment enlistment)
        {
            // Set the enlistment state
            enlistment.State = this;

            Monitor.Exit(enlistment.Transaction);
            try // Don't hold this lock while calling into the application code.
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.EnlistmentStatus(enlistment, NotificationCall.Commit);
                }

                Debug.Assert(enlistment.EnlistmentNotification != null);
                // Forward the notification to the enlistment
                enlistment.EnlistmentNotification.Commit(enlistment.Enlistment);
            }
            finally
            {
                Monitor.Enter(enlistment.Transaction);
            }
        }

        internal override void EnlistmentDone(InternalEnlistment enlistment)
        {
            // Move this enlistment to the ended state
            VolatileEnlistmentEnded.EnterState(enlistment);
        }
    }

    // InDoubt state is for an enlistment that has sent indoubt but has not been responeded to.
    internal sealed class VolatileEnlistmentInDoubt : VolatileEnlistmentState
    {
        internal override void EnterState(InternalEnlistment enlistment)
        {
            // Set the enlistment state
            enlistment.State = this;

            Monitor.Exit(enlistment.Transaction);
            try // Don't hold this lock while calling into the application code.
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.EnlistmentStatus(enlistment, NotificationCall.InDoubt);
                }

                Debug.Assert(enlistment.EnlistmentNotification != null);
                // Forward the notification to the enlistment
                enlistment.EnlistmentNotification.InDoubt(enlistment.PreparingEnlistment);
            }
            finally
            {
                Monitor.Enter(enlistment.Transaction);
            }
        }

        internal override void EnlistmentDone(InternalEnlistment enlistment)
        {
            // Move this enlistment to the ended state
            VolatileEnlistmentEnded.EnterState(enlistment);
        }
    }

    // Ended state is the state that is entered when the transaction has committed,
    // aborted, or said read only for an enlistment.  At this point there are no valid
    // operations on the enlistment.
    internal class VolatileEnlistmentEnded : VolatileEnlistmentState
    {
        internal override void EnterState(InternalEnlistment enlistment)
        {
            // Set the enlistment state
            enlistment.State = this;

            // Nothing to do.
        }

        internal override void ChangeStatePreparing(InternalEnlistment enlistment)
        {
            // This enlistment was told to abort before being told to prepare
        }

        internal override void InternalAborted(InternalEnlistment enlistment)
        {
            // Ignore this in case the enlistment gets here before
            // the transaction tells it to do so
        }

        internal override void InternalCommitted(InternalEnlistment enlistment)
        {
            // Ignore this in case the enlistment gets here before
            // the transaction tells it to do so
        }

        internal override void InternalIndoubt(InternalEnlistment enlistment)
        {
            // Ignore this in case the enlistment gets here before
            // the transaction tells it to do so
        }

        internal override void InDoubt(InternalEnlistment enlistment, Exception? e)
        {
            // Ignore this in case the enlistment gets here before
            // the transaction tells it to do so
        }
    }

    // At some point either early or late the enlistment responded ReadOnly
    internal sealed class VolatileEnlistmentDone : VolatileEnlistmentEnded
    {
        internal override void EnterState(InternalEnlistment enlistment)
        {
            // Set the enlistment state
            enlistment.State = this;

            // Nothing to do.
        }

        internal override void ChangeStatePreparing(InternalEnlistment enlistment)
        {
            enlistment.CheckComplete();
        }
    }
}
