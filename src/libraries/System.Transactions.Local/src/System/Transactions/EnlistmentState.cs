// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;

namespace System.Transactions
{
    // Base class for all enlistment states
    internal abstract class EnlistmentState
    {
        internal abstract void EnterState(InternalEnlistment enlistment);

        internal static EnlistmentStatePromoted _enlistmentStatePromoted;

        // Object for synchronizing access to the entire class( avoiding lock( typeof( ... )) )
        private static object s_classSyncObject;

        internal static EnlistmentStatePromoted EnlistmentStatePromoted =>
            LazyInitializer.EnsureInitialized(ref _enlistmentStatePromoted, ref s_classSyncObject, () => new EnlistmentStatePromoted());

        internal virtual void EnlistmentDone(InternalEnlistment enlistment)
        {
            throw TransactionException.CreateEnlistmentStateException(null, enlistment == null ? Guid.Empty : enlistment.DistributedTxId);
        }

        internal virtual void Prepared(InternalEnlistment enlistment)
        {
            throw TransactionException.CreateEnlistmentStateException(null, enlistment == null ? Guid.Empty : enlistment.DistributedTxId);
        }

        internal virtual void ForceRollback(InternalEnlistment enlistment, Exception e)
        {
            throw TransactionException.CreateEnlistmentStateException(null, enlistment == null ? Guid.Empty : enlistment.DistributedTxId);
        }

        internal virtual void Committed(InternalEnlistment enlistment)
        {
            throw TransactionException.CreateEnlistmentStateException(null, enlistment == null ? Guid.Empty : enlistment.DistributedTxId);
        }

        internal virtual void Aborted(InternalEnlistment enlistment, Exception e)
        {
            throw TransactionException.CreateEnlistmentStateException(null, enlistment == null ? Guid.Empty : enlistment.DistributedTxId);
        }

        internal virtual void InDoubt(InternalEnlistment enlistment, Exception e)
        {
            throw TransactionException.CreateEnlistmentStateException(null, enlistment == null ? Guid.Empty : enlistment.DistributedTxId);
        }

        internal virtual byte[] RecoveryInformation(InternalEnlistment enlistment)
        {
            throw TransactionException.CreateEnlistmentStateException(null, enlistment == null ? Guid.Empty : enlistment.DistributedTxId);
        }

        internal virtual void InternalAborted(InternalEnlistment enlistment)
        {
            Debug.Assert(false, string.Format(null, "Invalid Event for InternalEnlistment State; Current State: {0}", GetType()));
            throw TransactionException.CreateEnlistmentStateException(null, enlistment == null ? Guid.Empty : enlistment.DistributedTxId);
        }

        internal virtual void InternalCommitted(InternalEnlistment enlistment)
        {
            Debug.Assert(false, string.Format(null, "Invalid Event for InternalEnlistment State; Current State: {0}", GetType()));
            throw TransactionException.CreateEnlistmentStateException(null, enlistment == null ? Guid.Empty : enlistment.DistributedTxId);
        }

        internal virtual void InternalIndoubt(InternalEnlistment enlistment)
        {
            Debug.Assert(false, string.Format(null, "Invalid Event for InternalEnlistment State; Current State: {0}", GetType()));
            throw TransactionException.CreateEnlistmentStateException(null, enlistment == null ? Guid.Empty : enlistment.DistributedTxId);
        }

        internal virtual void ChangeStateCommitting(InternalEnlistment enlistment)
        {
            Debug.Assert(false, string.Format(null, "Invalid Event for InternalEnlistment State; Current State: {0}", GetType()));
            throw TransactionException.CreateEnlistmentStateException(null, enlistment == null ? Guid.Empty : enlistment.DistributedTxId);
        }

        internal virtual void ChangeStatePromoted(InternalEnlistment enlistment, IPromotedEnlistment promotedEnlistment)
        {
            Debug.Assert(false, string.Format(null, "Invalid Event for InternalEnlistment State; Current State: {0}", GetType()));
            throw TransactionException.CreateEnlistmentStateException(null, enlistment == null ? Guid.Empty : enlistment.DistributedTxId);
        }

        internal virtual void ChangeStateDelegated(InternalEnlistment enlistment)
        {
            Debug.Assert(false, string.Format(null, "Invalid Event for InternalEnlistment State; Current State: {0}", GetType()));
            throw TransactionException.CreateEnlistmentStateException(null, enlistment == null ? Guid.Empty : enlistment.DistributedTxId);
        }

        internal virtual void ChangeStatePreparing(InternalEnlistment enlistment)
        {
            Debug.Assert(false, string.Format(null, "Invalid Event for InternalEnlistment State; Current State: {0}", GetType()));
            throw TransactionException.CreateEnlistmentStateException(null, enlistment == null ? Guid.Empty : enlistment.DistributedTxId);
        }

        internal virtual void ChangeStateSinglePhaseCommit(InternalEnlistment enlistment)
        {
            Debug.Assert(false, string.Format(null, "Invalid Event for InternalEnlistment State; Current State: {0}", GetType()));
            throw TransactionException.CreateEnlistmentStateException(null, enlistment == null ? Guid.Empty : enlistment.DistributedTxId);
        }
    }

    internal class EnlistmentStatePromoted : EnlistmentState
    {
        internal override void EnterState(InternalEnlistment enlistment)
        {
            enlistment.State = this;
        }

        internal override void EnlistmentDone(InternalEnlistment enlistment)
        {
            Monitor.Exit(enlistment.SyncRoot);
            try
            {
                enlistment.PromotedEnlistment.EnlistmentDone();
            }
            finally
            {
                Monitor.Enter(enlistment.SyncRoot);
            }
        }

        internal override void Prepared(InternalEnlistment enlistment)
        {
            Monitor.Exit(enlistment.SyncRoot);
            try
            {
                enlistment.PromotedEnlistment.Prepared();
            }
            finally
            {
                Monitor.Enter(enlistment.SyncRoot);
            }
        }

        internal override void ForceRollback(InternalEnlistment enlistment, Exception e)
        {
            Monitor.Exit(enlistment.SyncRoot);
            try
            {
                enlistment.PromotedEnlistment.ForceRollback(e);
            }
            finally
            {
                Monitor.Enter(enlistment.SyncRoot);
            }
        }

        internal override void Committed(InternalEnlistment enlistment)
        {
            Monitor.Exit(enlistment.SyncRoot);
            try
            {
                enlistment.PromotedEnlistment.Committed();
            }
            finally
            {
                Monitor.Enter(enlistment.SyncRoot);
            }
        }

        internal override void Aborted(InternalEnlistment enlistment, Exception e)
        {
            Monitor.Exit(enlistment.SyncRoot);
            try
            {
                enlistment.PromotedEnlistment.Aborted(e);
            }
            finally
            {
                Monitor.Enter(enlistment.SyncRoot);
            }
        }

        internal override void InDoubt(InternalEnlistment enlistment, Exception e)
        {
            Monitor.Exit(enlistment.SyncRoot);
            try
            {
                enlistment.PromotedEnlistment.InDoubt(e);
            }
            finally
            {
                Monitor.Enter(enlistment.SyncRoot);
            }
        }

        internal override byte[] RecoveryInformation(InternalEnlistment enlistment)
        {
            Monitor.Exit(enlistment.SyncRoot);
            try
            {
                return enlistment.PromotedEnlistment.GetRecoveryInformation();
            }
            finally
            {
                Monitor.Enter(enlistment.SyncRoot);
            }
        }
    }
}
