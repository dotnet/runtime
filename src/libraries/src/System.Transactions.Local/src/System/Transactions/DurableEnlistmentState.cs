// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;

namespace System.Transactions
{
    // Base class for all durable enlistment states
    internal abstract class DurableEnlistmentState : EnlistmentState
    {
        private static DurableEnlistmentActive s_durableEnlistmentActive;
        private static DurableEnlistmentAborting s_durableEnlistmentAborting;
        private static DurableEnlistmentCommitting s_durableEnlistmentCommitting;
        private static DurableEnlistmentDelegated s_durableEnlistmentDelegated;
        private static DurableEnlistmentEnded s_durableEnlistmentEnded;

        // Object for synchronizing access to the entire class( avoiding lock( typeof( ... )) )
        private static object s_classSyncObject;

        internal static DurableEnlistmentActive DurableEnlistmentActive =>
            LazyInitializer.EnsureInitialized(ref s_durableEnlistmentActive, ref s_classSyncObject, () => new DurableEnlistmentActive());

        protected static DurableEnlistmentAborting DurableEnlistmentAborting =>
            LazyInitializer.EnsureInitialized(ref s_durableEnlistmentAborting, ref s_classSyncObject, () => new DurableEnlistmentAborting());

        protected static DurableEnlistmentCommitting DurableEnlistmentCommitting =>
            LazyInitializer.EnsureInitialized(ref s_durableEnlistmentCommitting, ref s_classSyncObject, () => new DurableEnlistmentCommitting());

        protected static DurableEnlistmentDelegated DurableEnlistmentDelegated =>
            LazyInitializer.EnsureInitialized(ref s_durableEnlistmentDelegated, ref s_classSyncObject, () => new DurableEnlistmentDelegated());

        protected static DurableEnlistmentEnded DurableEnlistmentEnded =>
            LazyInitializer.EnsureInitialized(ref s_durableEnlistmentEnded, ref s_classSyncObject, () => new DurableEnlistmentEnded());
    }

    // Active state for a durable enlistment.  In this state the transaction can be aborted 
    // asynchronously by calling abort.
    internal class DurableEnlistmentActive : DurableEnlistmentState
    {
        internal override void EnterState(InternalEnlistment enlistment)
        {
            // Set the enlistment state
            enlistment.State = this;

            // Yeah it's active
        }

        internal override void EnlistmentDone(InternalEnlistment enlistment)
        {
            // Mark the enlistment as done.
            DurableEnlistmentEnded.EnterState(enlistment);
        }

        internal override void InternalAborted(InternalEnlistment enlistment)
        {
            // Transition to the aborting state
            DurableEnlistmentAborting.EnterState(enlistment);
        }

        internal override void ChangeStateCommitting(InternalEnlistment enlistment)
        {
            // Transition to the committing state
            DurableEnlistmentCommitting.EnterState(enlistment);
        }

        internal override void ChangeStatePromoted(InternalEnlistment enlistment, IPromotedEnlistment promotedEnlistment)
        {
            // Save the promoted enlistment because future notifications must be sent here.
            enlistment.PromotedEnlistment = promotedEnlistment;

            // The transaction is being promoted promote the enlistment as well
            EnlistmentStatePromoted.EnterState(enlistment);
        }

        internal override void ChangeStateDelegated(InternalEnlistment enlistment)
        {
            // This is a valid state transition.
            DurableEnlistmentDelegated.EnterState(enlistment);
        }
    }

    // Aborting state for a durable enlistment.  In this state the transaction has been aborted,
    // by someone other than the enlistment.
    //
    internal class DurableEnlistmentAborting : DurableEnlistmentState
    {
        internal override void EnterState(InternalEnlistment enlistment)
        {
            // Set the enlistment state
            enlistment.State = this;

            Monitor.Exit(enlistment.Transaction);
            try
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.EnlistmentStatus(enlistment, NotificationCall.Rollback);
                }

                // Send the Rollback notification to the enlistment
                if (enlistment.SinglePhaseNotification != null)
                {
                    enlistment.SinglePhaseNotification.Rollback(enlistment.SinglePhaseEnlistment);
                }
                else
                {
                    enlistment.PromotableSinglePhaseNotification.Rollback(enlistment.SinglePhaseEnlistment);
                }
            }
            finally
            {
                Monitor.Enter(enlistment.Transaction);
            }
        }

        internal override void Aborted(InternalEnlistment enlistment, Exception e)
        {
            if (enlistment.Transaction._innerException == null)
            {
                enlistment.Transaction._innerException = e;
            }

            // Transition to the ended state
            DurableEnlistmentEnded.EnterState(enlistment);
        }

        internal override void EnlistmentDone(InternalEnlistment enlistment)
        {
            // Transition to the ended state
            DurableEnlistmentEnded.EnterState(enlistment);
        }
    }

    // Committing state is when SPC has been sent to an enlistment but no response
    // has been received.
    //
    internal class DurableEnlistmentCommitting : DurableEnlistmentState
    {
        internal override void EnterState(InternalEnlistment enlistment)
        {
            bool spcCommitted = false;
            // Set the enlistment state
            enlistment.State = this;

            Monitor.Exit(enlistment.Transaction);
            try
            {
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.EnlistmentStatus(enlistment, NotificationCall.SinglePhaseCommit);
                }

                // Send the Commit notification to the enlistment
                if (enlistment.SinglePhaseNotification != null)
                {
                    enlistment.SinglePhaseNotification.SinglePhaseCommit(enlistment.SinglePhaseEnlistment);
                }
                else
                {
                    enlistment.PromotableSinglePhaseNotification.SinglePhaseCommit(enlistment.SinglePhaseEnlistment);
                }
                spcCommitted = true;
            }
            finally
            {
                if (!spcCommitted)
                {
                    enlistment.SinglePhaseEnlistment.InDoubt();
                }
                Monitor.Enter(enlistment.Transaction);
            }
        }

        internal override void EnlistmentDone(InternalEnlistment enlistment)
        {
            // EnlistmentDone should be treated the same as Committed from this state.
            // This eliminates a race between the SPC call and the EnlistmentDone call.

            // Transition to the ended state
            DurableEnlistmentEnded.EnterState(enlistment);

            // Make the transaction commit
            enlistment.Transaction.State.ChangeStateTransactionCommitted(enlistment.Transaction);
        }

        internal override void Committed(InternalEnlistment enlistment)
        {
            // Transition to the ended state
            DurableEnlistmentEnded.EnterState(enlistment);

            // Make the transaction commit
            enlistment.Transaction.State.ChangeStateTransactionCommitted(enlistment.Transaction);
        }

        internal override void Aborted(InternalEnlistment enlistment, Exception e)
        {
            // Transition to the ended state
            DurableEnlistmentEnded.EnterState(enlistment);

            // Start the transaction aborting
            enlistment.Transaction.State.ChangeStateTransactionAborted(enlistment.Transaction, e);
        }

        internal override void InDoubt(InternalEnlistment enlistment, Exception e)
        {
            // Transition to the ended state
            DurableEnlistmentEnded.EnterState(enlistment);

            if (enlistment.Transaction._innerException == null)
            {
                enlistment.Transaction._innerException = e;
            }

            // Make the transaction in dobut
            enlistment.Transaction.State.InDoubtFromEnlistment(enlistment.Transaction);
        }
    }

    // Delegated state for a durable enlistment represents an enlistment that was
    // origionally a PromotableSinglePhaseEnlisment that where promotion has happened.
    // These enlistments don't need to participate in the commit process anymore.
    internal class DurableEnlistmentDelegated : DurableEnlistmentState
    {
        internal override void EnterState(InternalEnlistment enlistment)
        {
            // Set the enlistment state
            enlistment.State = this;

            // At this point the durable enlistment should have someone to forward to.
            Debug.Assert(enlistment.PromotableSinglePhaseNotification != null);
        }

        internal override void Committed(InternalEnlistment enlistment)
        {
            // Transition to the ended state
            DurableEnlistmentEnded.EnterState(enlistment);

            // Change the transaction to committed.
            enlistment.Transaction.State.ChangeStatePromotedCommitted(enlistment.Transaction);
        }

        internal override void Aborted(InternalEnlistment enlistment, Exception e)
        {
            // Transition to the ended state
            DurableEnlistmentEnded.EnterState(enlistment);

            if (enlistment.Transaction._innerException == null)
            {
                enlistment.Transaction._innerException = e;
            }

            // Start the transaction aborting
            enlistment.Transaction.State.ChangeStatePromotedAborted(enlistment.Transaction);
        }

        internal override void InDoubt(InternalEnlistment enlistment, Exception e)
        {
            // Transition to the ended state
            DurableEnlistmentEnded.EnterState(enlistment);

            if (enlistment.Transaction._innerException == null)
            {
                enlistment.Transaction._innerException = e;
            }

            // Tell the transaction that the enlistment is InDoubt.  Note that
            // for a transaction that has been delegated and then promoted there
            // are two chances to get a better answer than indoubt.  So it may be that
            // the TM will have a better answer.
            enlistment.Transaction.State.InDoubtFromEnlistment(enlistment.Transaction);
        }
    }

    // Ended state is the state that is entered when the durable enlistment has committed,
    // aborted, or said read only for an enlistment.  At this point there are no valid
    // operations on the enlistment.
    internal class DurableEnlistmentEnded : DurableEnlistmentState
    {
        internal override void EnterState(InternalEnlistment enlistment)
        {
            // Set the enlistment state
            enlistment.State = this;
        }

        internal override void InternalAborted(InternalEnlistment enlistment)
        {
            // From the Aborting state the transaction may tell the enlistment to abort.  At this point 
            // it already knows.  Eat this message.
        }

        internal override void InDoubt(InternalEnlistment enlistment, Exception e)
        {
            // Ignore this in case the enlistment gets here before
            // the transaction tells it to do so
        }
    }
}
