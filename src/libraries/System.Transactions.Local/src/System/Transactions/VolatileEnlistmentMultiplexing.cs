// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;

namespace System.Transactions
{
    // The volatile Demultiplexer is a fanout point for promoted volatile enlistments.
    // When a transaction is promoted a single volatile enlistment is created in the new
    // transaction for all volitile enlistments on the transaction.  When the VolatileDemux
    // receives a preprepare it will fan that notification out to all of the enlistments
    // on the transaction.  When it has gathered all of the responses it will send a
    // single vote back to the DistributedTransactionManager.
    internal abstract class VolatileDemultiplexer : IEnlistmentNotificationInternal
    {
        // Reference the transactions so that we have access to it's enlistments
        protected InternalTransaction _transaction;

        // Store the IVolatileEnlistment interface to call back to the Distributed TM
        internal IPromotedEnlistment? _promotedEnlistment;
        internal IPromotedEnlistment? _preparingEnlistment;

        public VolatileDemultiplexer(InternalTransaction transaction)
        {
            _transaction = transaction;
        }

        internal static void BroadcastCommitted(ref VolatileEnlistmentSet volatiles)
        {
            // Broadcast preprepare to the volatile subordinates
            for (int i = 0; i < volatiles._volatileEnlistmentCount; i++)
            {
                volatiles._volatileEnlistments[i]._twoPhaseState!.InternalCommitted(
                    volatiles._volatileEnlistments[i]);
            }
        }

        // This broadcast is used by the state machines and therefore must be internal.
        internal static void BroadcastRollback(ref VolatileEnlistmentSet volatiles)
        {
            // Broadcast preprepare to the volatile subordinates
            for (int i = 0; i < volatiles._volatileEnlistmentCount; i++)
            {
                volatiles._volatileEnlistments[i]._twoPhaseState!.InternalAborted(
                    volatiles._volatileEnlistments[i]);
            }
        }

        internal static void BroadcastInDoubt(ref VolatileEnlistmentSet volatiles)
        {
            // Broadcast preprepare to the volatile subordinates
            for (int i = 0; i < volatiles._volatileEnlistmentCount; i++)
            {
                volatiles._volatileEnlistments[i]._twoPhaseState!.InternalIndoubt(
                    volatiles._volatileEnlistments[i]);
            }
        }

        // Object for synchronizing access to the entire class( avoiding lock( typeof( ... )) )
        private static object? s_classSyncObject;
        internal static object ClassSyncObject
        {
            get
            {
                if (s_classSyncObject == null)
                {
                    object o = new object();
                    Interlocked.CompareExchange(ref s_classSyncObject, o, null);
                }
                return s_classSyncObject;
            }
        }

        private static WaitCallback? s_prepareCallback;
        private static WaitCallback PrepareCallback => LazyInitializer.EnsureInitialized(ref s_prepareCallback, ref s_classSyncObject, () => new WaitCallback(PoolablePrepare!));

        protected static void PoolablePrepare(object state)
        {
            VolatileDemultiplexer demux = (VolatileDemultiplexer)state;

            // Don't block an enlistment thread (or a thread pool thread).  So
            // try to get the transaction lock but if unsuccessfull give up and
            // queue this operation to try again later.
            bool tookLock = false;
            try
            {
                Monitor.TryEnter(demux._transaction, 250, ref tookLock);
                if (tookLock)
                {
                    demux.InternalPrepare();
                }
                else
                {
                    if (!ThreadPool.QueueUserWorkItem(PrepareCallback, demux))
                    {
                        throw TransactionException.CreateInvalidOperationException(
                            TraceSourceType.TraceSourceLtm,
                            SR.UnexpectedFailureOfThreadPool,
                            null
                            );
                    }
                }
            }
            finally
            {
                if (tookLock)
                {
                    Monitor.Exit(demux._transaction);
                }
            }
        }

        private static WaitCallback? s_commitCallback;
        private static WaitCallback CommitCallback => LazyInitializer.EnsureInitialized(ref s_commitCallback, ref s_classSyncObject, () => new WaitCallback(PoolableCommit!));

        protected static void PoolableCommit(object state)
        {
            VolatileDemultiplexer demux = (VolatileDemultiplexer)state;

            // Don't block an enlistment thread (or a thread pool thread).  So
            // try to get the transaction lock but if unsuccessfull give up and
            // queue this operation to try again later.
            bool tookLock = false;
            try
            {
                Monitor.TryEnter(demux._transaction, 250, ref tookLock);
                if (tookLock)
                {
                    demux.InternalCommit();
                }
                else
                {
                    if (!ThreadPool.QueueUserWorkItem(CommitCallback, demux))
                    {
                        throw TransactionException.CreateInvalidOperationException(
                            TraceSourceType.TraceSourceLtm,
                            SR.UnexpectedFailureOfThreadPool,
                            null
                            );
                    }
                }
            }
            finally
            {
                if (tookLock)
                {
                    Monitor.Exit(demux._transaction);
                }
            }
        }

        private static WaitCallback? s_rollbackCallback;
        private static WaitCallback RollbackCallback => LazyInitializer.EnsureInitialized(ref s_rollbackCallback, ref s_classSyncObject, () => new WaitCallback(PoolableRollback!));

        protected static void PoolableRollback(object state)
        {
            VolatileDemultiplexer demux = (VolatileDemultiplexer)state;

            // Don't block an enlistment thread (or a thread pool thread).  So
            // try to get the transaction lock but if unsuccessfull give up and
            // queue this operation to try again later.
            bool tookLock = false;
            try
            {
                Monitor.TryEnter(demux._transaction, 250, ref tookLock);
                if (tookLock)
                {
                    demux.InternalRollback();
                }
                else
                {
                    if (!ThreadPool.QueueUserWorkItem(RollbackCallback, demux))
                    {
                        throw TransactionException.CreateInvalidOperationException(
                            TraceSourceType.TraceSourceLtm,
                            SR.UnexpectedFailureOfThreadPool,
                            null
                            );
                    }
                }
            }
            finally
            {
                if (tookLock)
                {
                    Monitor.Exit(demux._transaction);
                }
            }
        }

        private static WaitCallback? s_inDoubtCallback;
        private static WaitCallback InDoubtCallback => LazyInitializer.EnsureInitialized(ref s_inDoubtCallback, ref s_classSyncObject, () => new WaitCallback(PoolableInDoubt!));

        protected static void PoolableInDoubt(object state)
        {
            VolatileDemultiplexer demux = (VolatileDemultiplexer)state;

            // Don't block an enlistment thread (or a thread pool thread).  So
            // try to get the transaction lock but if unsuccessfull give up and
            // queue this operation to try again later.
            bool tookLock = false;
            try
            {
                Monitor.TryEnter(demux._transaction, 250, ref tookLock);
                if (tookLock)
                {
                    demux.InternalInDoubt();
                }
                else
                {
                    if (!ThreadPool.QueueUserWorkItem(InDoubtCallback, demux))
                    {
                        throw TransactionException.CreateInvalidOperationException(
                            TraceSourceType.TraceSourceLtm,
                            SR.UnexpectedFailureOfThreadPool,
                            null
                            );
                    }
                }
            }
            finally
            {
                if (tookLock)
                {
                    Monitor.Exit(demux._transaction);
                }
            }
        }

        protected abstract void InternalPrepare();
        protected abstract void InternalCommit();
        protected abstract void InternalRollback();
        protected abstract void InternalInDoubt();

        #region IEnlistmentNotification Members

        // Fanout Preprepare notifications
        public abstract void Prepare(IPromotedEnlistment en);

        public abstract void Commit(IPromotedEnlistment en);

        public abstract void Rollback(IPromotedEnlistment en);

        public abstract void InDoubt(IPromotedEnlistment en);

        #endregion
    }


    // This class implements the phase 0 version of a volatile demux.
    internal sealed class Phase0VolatileDemultiplexer : VolatileDemultiplexer
    {
        public Phase0VolatileDemultiplexer(InternalTransaction transaction) : base(transaction) { }

        protected override void InternalPrepare()
        {
            Debug.Assert(_promotedEnlistment != null && _transaction.State != null);
            try
            {
                _transaction.State.ChangeStatePromotedPhase0(_transaction);
            }
            catch (TransactionAbortedException e)
            {
                _promotedEnlistment.ForceRollback(e);
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.ExceptionConsumed(e);
                }
            }
            catch (TransactionInDoubtException e)
            {
                _promotedEnlistment.EnlistmentDone();
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.ExceptionConsumed(e);
                }
            }
        }

        protected override void InternalCommit()
        {
            Debug.Assert(_promotedEnlistment != null && _transaction.State != null);

            // Respond immediately to the TM
            _promotedEnlistment.EnlistmentDone();

            _transaction.State.ChangeStatePromotedCommitted(_transaction);
        }

        protected override void InternalRollback()
        {
            Debug.Assert(_promotedEnlistment != null && _transaction.State != null);

            // Respond immediately to the TM
            _promotedEnlistment.EnlistmentDone();

            _transaction.State.ChangeStatePromotedAborted(_transaction);
        }

        protected override void InternalInDoubt()
        {
            Debug.Assert(_transaction.State != null);
            _transaction.State.InDoubtFromDtc(_transaction);
        }

        #region IEnlistmentNotification Members

        // Fanout Preprepare notifications
        public override void Prepare(IPromotedEnlistment en)
        {
            _preparingEnlistment = en;
            PoolablePrepare(this);
        }


        public override void Commit(IPromotedEnlistment en)
        {
            _promotedEnlistment = en;
            PoolableCommit(this);
        }


        public override void Rollback(IPromotedEnlistment en)
        {
            _promotedEnlistment = en;
            PoolableRollback(this);
        }


        public override void InDoubt(IPromotedEnlistment en)
        {
            _promotedEnlistment = en;
            PoolableInDoubt(this);
        }

        #endregion
    }

    // This class implements the phase 1 version of a volatile demux.
    internal sealed class Phase1VolatileDemultiplexer : VolatileDemultiplexer
    {
        public Phase1VolatileDemultiplexer(InternalTransaction transaction) : base(transaction) { }

        protected override void InternalPrepare()
        {
            Debug.Assert(_promotedEnlistment != null && _transaction.State != null);
            try
            {
                _transaction.State.ChangeStatePromotedPhase1(_transaction);
            }
            catch (TransactionAbortedException e)
            {
                _promotedEnlistment.ForceRollback(e);
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.ExceptionConsumed(e);
                }
            }
            catch (TransactionInDoubtException e)
            {
                _promotedEnlistment.EnlistmentDone();
                TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    etwLog.ExceptionConsumed(e);
                }
            }
        }


        protected override void InternalCommit()
        {
            Debug.Assert(_promotedEnlistment != null && _transaction.State != null);

            // Respond immediately to the TM
            _promotedEnlistment.EnlistmentDone();

            _transaction.State.ChangeStatePromotedCommitted(_transaction);
        }


        protected override void InternalRollback()
        {
            Debug.Assert(_promotedEnlistment != null && _transaction.State != null);

            // Respond immediately to the TM
            _promotedEnlistment.EnlistmentDone();

            _transaction.State.ChangeStatePromotedAborted(_transaction);
        }


        protected override void InternalInDoubt()
        {
            Debug.Assert(_transaction.State != null);
            _transaction.State.InDoubtFromDtc(_transaction);
        }


        // Fanout Preprepare notifications
        public override void Prepare(IPromotedEnlistment en)
        {
            _preparingEnlistment = en;
            PoolablePrepare(this);
        }


        public override void Commit(IPromotedEnlistment en)
        {
            _promotedEnlistment = en;
            PoolableCommit(this);
        }


        public override void Rollback(IPromotedEnlistment en)
        {
            _promotedEnlistment = en;
            PoolableRollback(this);
        }


        public override void InDoubt(IPromotedEnlistment en)
        {
            _promotedEnlistment = en;
            PoolableInDoubt(this);
        }
    }



    internal struct VolatileEnlistmentSet
    {
        internal InternalEnlistment[] _volatileEnlistments;
        internal int _volatileEnlistmentCount;
        internal int _volatileEnlistmentSize;
        internal int _dependentClones;

        // Track the number of volatile enlistments that have prepared.
        internal int _preparedVolatileEnlistments;

        // This is a single pinpoint enlistment to represent all volatile enlistments that
        // may exist on a promoted transaction.  This member should only be initialized if
        // a transaction is promoted.
        private VolatileDemultiplexer _volatileDemux;
        internal VolatileDemultiplexer VolatileDemux
        {
            get { return _volatileDemux; }
            set
            {
                Debug.Assert(_volatileDemux == null, "volatileDemux can only be set once.");
                _volatileDemux = value;
            }
        }
    }
}
