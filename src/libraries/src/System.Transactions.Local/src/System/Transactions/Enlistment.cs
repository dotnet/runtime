// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace System.Transactions
{
    internal interface IPromotedEnlistment
    {
        void EnlistmentDone();

        void Prepared();

        void ForceRollback();

        void ForceRollback(Exception e);

        void Committed();

        void Aborted();

        void Aborted(Exception e);

        void InDoubt();

        void InDoubt(Exception e);

        byte[] GetRecoveryInformation();

        InternalEnlistment InternalEnlistment
        {
            get;
            set;
        }
    }

    //
    // InternalEnlistment by itself can support a Phase0 volatile enlistment.
    // There are derived classes to support durable, phase1 volatile & PSPE
    // enlistments.
    //
    internal class InternalEnlistment : ISinglePhaseNotificationInternal
    {
        // Storage for the state of the enlistment.
        internal EnlistmentState _twoPhaseState;

        // Interface implemented by the enlistment owner for notifications
        protected IEnlistmentNotification _twoPhaseNotifications;

        // Store a reference to the single phase notification interface in case
        // the enlisment supports it.
        protected ISinglePhaseNotification _singlePhaseNotifications;

        // Reference to the containing transaction.
        protected InternalTransaction _transaction;

        // Reference to the lightweight transaction.
        private Transaction _atomicTransaction;

        // The EnlistmentTraceIdentifier for this enlistment.
        private EnlistmentTraceIdentifier _traceIdentifier;

        // Unique value amongst all enlistments for a given internal transaction.
        private int _enlistmentId;

        internal Guid DistributedTxId
        {
            get
            {
                Guid returnValue = Guid.Empty;

                if (Transaction != null)
                {
                    returnValue = Transaction.DistributedTxId;
                }
                return returnValue;
            }
        }

        // Parent Enlistment Object
        private Enlistment _enlistment;
        private PreparingEnlistment _preparingEnlistment;
        private SinglePhaseEnlistment _singlePhaseEnlistment;

        // If this enlistment is promoted store the object it delegates to.
        private IPromotedEnlistment _promotedEnlistment;

        // For Recovering Enlistments
        protected InternalEnlistment(Enlistment enlistment, IEnlistmentNotification twoPhaseNotifications)
        {
            Debug.Assert(this is RecoveringInternalEnlistment, "this is RecoveringInternalEnlistment");
            _enlistment = enlistment;
            _twoPhaseNotifications = twoPhaseNotifications;
            _enlistmentId = 1;
            _traceIdentifier = EnlistmentTraceIdentifier.Empty;
        }

        // For Promotable Enlistments
        protected InternalEnlistment(Enlistment enlistment, InternalTransaction transaction, Transaction atomicTransaction)
        {
            Debug.Assert(this is PromotableInternalEnlistment, "this is PromotableInternalEnlistment");
            _enlistment = enlistment;
            _transaction = transaction;
            _atomicTransaction = atomicTransaction;
            _enlistmentId = transaction._enlistmentCount++;
            _traceIdentifier = EnlistmentTraceIdentifier.Empty;
        }

        internal InternalEnlistment(
            Enlistment enlistment,
            InternalTransaction transaction,
            IEnlistmentNotification twoPhaseNotifications,
            ISinglePhaseNotification singlePhaseNotifications,
            Transaction atomicTransaction)
        {
            _enlistment = enlistment;
            _transaction = transaction;
            _twoPhaseNotifications = twoPhaseNotifications;
            _singlePhaseNotifications = singlePhaseNotifications;
            _atomicTransaction = atomicTransaction;
            _enlistmentId = transaction._enlistmentCount++;
            _traceIdentifier = EnlistmentTraceIdentifier.Empty;
        }

        internal InternalEnlistment(
            Enlistment enlistment,
            IEnlistmentNotification twoPhaseNotifications,
            InternalTransaction transaction,
            Transaction atomicTransaction)
        {
            _enlistment = enlistment;
            _twoPhaseNotifications = twoPhaseNotifications;
            _transaction = transaction;
            _atomicTransaction = atomicTransaction;
        }

        internal EnlistmentState State
        {
            get { return _twoPhaseState; }
            set { _twoPhaseState = value; }
        }

        internal Enlistment Enlistment => _enlistment;

        internal PreparingEnlistment PreparingEnlistment
        {
            get
            {
                if (_preparingEnlistment == null)
                {
                    // If there is a race here one of the objects would simply be garbage collected.
                    _preparingEnlistment = new PreparingEnlistment(this);
                }
                return _preparingEnlistment;
            }
        }

        internal SinglePhaseEnlistment SinglePhaseEnlistment
        {
            get
            {
                if (_singlePhaseEnlistment == null)
                {
                    // If there is a race here one of the objects would simply be garbage collected.
                    _singlePhaseEnlistment = new SinglePhaseEnlistment(this);
                }
                return _singlePhaseEnlistment;
            }
        }

        internal InternalTransaction Transaction => _transaction;

        internal virtual object SyncRoot
        {
            get
            {
                Debug.Assert(_transaction != null, "this.transaction != null");
                return _transaction;
            }
        }

        internal IEnlistmentNotification EnlistmentNotification => _twoPhaseNotifications;

        internal ISinglePhaseNotification SinglePhaseNotification => _singlePhaseNotifications;

        internal virtual IPromotableSinglePhaseNotification PromotableSinglePhaseNotification
        {
            get
            {
                Debug.Fail("PromotableSinglePhaseNotification called for a non promotable enlistment.");
                throw new NotImplementedException();
            }
        }

        internal IPromotedEnlistment PromotedEnlistment
        {
            get { return _promotedEnlistment; }
            set { _promotedEnlistment = value; }
        }

        internal EnlistmentTraceIdentifier EnlistmentTraceId
        {
            get
            {
                if (_traceIdentifier == EnlistmentTraceIdentifier.Empty)
                {
                    lock (SyncRoot)
                    {
                        if (_traceIdentifier == EnlistmentTraceIdentifier.Empty)
                        {
                            EnlistmentTraceIdentifier temp;
                            if (null != _atomicTransaction)
                            {
                                temp = new EnlistmentTraceIdentifier(
                                    Guid.Empty,
                                    _atomicTransaction.TransactionTraceId,
                                    _enlistmentId);
                            }
                            else
                            {
                                temp = new EnlistmentTraceIdentifier(
                                    Guid.Empty,
                                    new TransactionTraceIdentifier(
                                        InternalTransaction.InstanceIdentifier +
                                            Convert.ToString(Interlocked.Increment(ref InternalTransaction._nextHash), CultureInfo.InvariantCulture),
                                        0),
                                    _enlistmentId);
                            }
                            Interlocked.MemoryBarrier();
                            _traceIdentifier = temp;
                        }
                    }
                }
                return _traceIdentifier;
            }
        }

        internal virtual void FinishEnlistment()
        {
            // Note another enlistment finished.
            Transaction._phase0Volatiles._preparedVolatileEnlistments++;
            CheckComplete();
        }

        internal virtual void CheckComplete()
        {
            // Make certain we increment the right list.
            Debug.Assert(Transaction._phase0Volatiles._preparedVolatileEnlistments <=
                Transaction._phase0Volatiles._volatileEnlistmentCount + Transaction._phase0Volatiles._dependentClones);

            // Check to see if all of the volatile enlistments are done.
            if (Transaction._phase0Volatiles._preparedVolatileEnlistments ==
                Transaction._phase0VolatileWaveCount + Transaction._phase0Volatiles._dependentClones)
            {
                Transaction.State.Phase0VolatilePrepareDone(Transaction);
            }
        }

        internal virtual Guid ResourceManagerIdentifier
        {
            get
            {
                Debug.Fail("ResourceManagerIdentifier called for non durable enlistment");
                throw new NotImplementedException();
            }
        }

        void ISinglePhaseNotificationInternal.SinglePhaseCommit(IPromotedEnlistment singlePhaseEnlistment)
        {
            bool spcCommitted = false;
            _promotedEnlistment = singlePhaseEnlistment;
            try
            {
                _singlePhaseNotifications.SinglePhaseCommit(SinglePhaseEnlistment);
                spcCommitted = true;
            }
            finally
            {
                if (!spcCommitted)
                {
                    SinglePhaseEnlistment.InDoubt();
                }
            }
        }

        void IEnlistmentNotificationInternal.Prepare(
            IPromotedEnlistment preparingEnlistment
            )
        {
            _promotedEnlistment = preparingEnlistment;
            _twoPhaseNotifications.Prepare(PreparingEnlistment);
        }

        void IEnlistmentNotificationInternal.Commit(
            IPromotedEnlistment enlistment
            )
        {
            _promotedEnlistment = enlistment;
            _twoPhaseNotifications.Commit(Enlistment);
        }

        void IEnlistmentNotificationInternal.Rollback(
            IPromotedEnlistment enlistment
            )
        {
            _promotedEnlistment = enlistment;
            _twoPhaseNotifications.Rollback(Enlistment);
        }

        void IEnlistmentNotificationInternal.InDoubt(
            IPromotedEnlistment enlistment
            )
        {
            _promotedEnlistment = enlistment;
            _twoPhaseNotifications.InDoubt(Enlistment);
        }
    }

    internal class DurableInternalEnlistment : InternalEnlistment
    {
        // Resource Manager Identifier for this enlistment if it is durable
        internal Guid _resourceManagerIdentifier;

        internal DurableInternalEnlistment(
            Enlistment enlistment,
            Guid resourceManagerIdentifier,
            InternalTransaction transaction,
            IEnlistmentNotification twoPhaseNotifications,
            ISinglePhaseNotification singlePhaseNotifications,
            Transaction atomicTransaction) :
            base(enlistment, transaction, twoPhaseNotifications, singlePhaseNotifications, atomicTransaction)
        {
            _resourceManagerIdentifier = resourceManagerIdentifier;
        }

        protected DurableInternalEnlistment(Enlistment enlistment, IEnlistmentNotification twoPhaseNotifications) :
            base(enlistment, twoPhaseNotifications)
        {
        }

        internal override Guid ResourceManagerIdentifier => _resourceManagerIdentifier;
    }

    //
    // Since RecoveringInternalEnlistment does not have a transaction it must take
    // a separate object as its sync root.
    //
    internal class RecoveringInternalEnlistment : DurableInternalEnlistment
    {
        private object _syncRoot;

        internal RecoveringInternalEnlistment(Enlistment enlistment, IEnlistmentNotification twoPhaseNotifications, object syncRoot) :
            base(enlistment, twoPhaseNotifications)
        {
            _syncRoot = syncRoot;
        }

        internal override object SyncRoot => _syncRoot;
    }

    internal class PromotableInternalEnlistment : InternalEnlistment
    {
        // This class acts as the durable single phase enlistment for a
        // promotable single phase enlistment.
        private IPromotableSinglePhaseNotification _promotableNotificationInterface;

        internal PromotableInternalEnlistment(
            Enlistment enlistment,
            InternalTransaction transaction,
            IPromotableSinglePhaseNotification promotableSinglePhaseNotification,
            Transaction atomicTransaction) :
            base(enlistment, transaction, atomicTransaction)
        {
            _promotableNotificationInterface = promotableSinglePhaseNotification;
        }

        internal override IPromotableSinglePhaseNotification PromotableSinglePhaseNotification => _promotableNotificationInterface;
    }


    // This class supports volatile enlistments
    //
    internal class Phase1VolatileEnlistment : InternalEnlistment
    {
        public Phase1VolatileEnlistment(
            Enlistment enlistment,
            InternalTransaction transaction,
            IEnlistmentNotification twoPhaseNotifications,
            ISinglePhaseNotification singlePhaseNotifications,
            Transaction atomicTransaction)
            : base(enlistment, transaction, twoPhaseNotifications, singlePhaseNotifications, atomicTransaction)
        {
        }

        internal override void FinishEnlistment()
        {
            // Note another enlistment finished.
            _transaction._phase1Volatiles._preparedVolatileEnlistments++;
            CheckComplete();
        }

        internal override void CheckComplete()
        {
            // Make certain we increment the right list.
            Debug.Assert(_transaction._phase1Volatiles._preparedVolatileEnlistments <=
                _transaction._phase1Volatiles._volatileEnlistmentCount +
                _transaction._phase1Volatiles._dependentClones);

            // Check to see if all of the volatile enlistments are done.
            if (_transaction._phase1Volatiles._preparedVolatileEnlistments ==
                _transaction._phase1Volatiles._volatileEnlistmentCount +
                _transaction._phase1Volatiles._dependentClones)
            {
                _transaction.State.Phase1VolatilePrepareDone(_transaction);
            }
        }
    }

    public class Enlistment
    {
        // Interface for communicating with the state machine.
        internal InternalEnlistment _internalEnlistment;

        internal Enlistment(InternalEnlistment internalEnlistment)
        {
            _internalEnlistment = internalEnlistment;
        }

        internal Enlistment(
            Guid resourceManagerIdentifier,
            InternalTransaction transaction,
            IEnlistmentNotification twoPhaseNotifications,
            ISinglePhaseNotification singlePhaseNotifications,
            Transaction atomicTransaction)
        {
            _internalEnlistment = new DurableInternalEnlistment(
                this,
                resourceManagerIdentifier,
                transaction,
                twoPhaseNotifications,
                singlePhaseNotifications,
                atomicTransaction
                );
        }

        internal Enlistment(
            InternalTransaction transaction,
            IEnlistmentNotification twoPhaseNotifications,
            ISinglePhaseNotification singlePhaseNotifications,
            Transaction atomicTransaction,
            EnlistmentOptions enlistmentOptions)
        {
            if ((enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0)
            {
                _internalEnlistment = new InternalEnlistment(
                    this,
                    transaction,
                    twoPhaseNotifications,
                    singlePhaseNotifications,
                    atomicTransaction
                    );
            }
            else
            {
                _internalEnlistment = new Phase1VolatileEnlistment(
                this,
                transaction,
                twoPhaseNotifications,
                singlePhaseNotifications,
                atomicTransaction
                );
            }
        }

        // This constructor is for a promotable single phase enlistment.
        internal Enlistment(
            InternalTransaction transaction,
            IPromotableSinglePhaseNotification promotableSinglePhaseNotification,
            Transaction atomicTransaction)
        {
            _internalEnlistment = new PromotableInternalEnlistment(
                this,
                transaction,
                promotableSinglePhaseNotification,
                atomicTransaction
                );
        }

        internal Enlistment(
            IEnlistmentNotification twoPhaseNotifications,
            InternalTransaction transaction,
            Transaction atomicTransaction)
        {
            _internalEnlistment = new InternalEnlistment(
                this,
                twoPhaseNotifications,
                transaction,
                atomicTransaction
                );
        }

        internal Enlistment(IEnlistmentNotification twoPhaseNotifications, object syncRoot)
        {
            _internalEnlistment = new RecoveringInternalEnlistment(
                this,
                twoPhaseNotifications,
                syncRoot
                );
        }

        public void Done()
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceLtm, this);
                etwLog.EnlistmentDone(_internalEnlistment);
            }

            lock (_internalEnlistment.SyncRoot)
            {
                _internalEnlistment.State.EnlistmentDone(_internalEnlistment);
            }

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceLtm, this);
            }
        }


        internal InternalEnlistment InternalEnlistment => _internalEnlistment;
    }
}
