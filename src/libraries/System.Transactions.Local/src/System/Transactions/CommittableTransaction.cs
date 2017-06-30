// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace System.Transactions
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2229", Justification="Serialization not yet supported and will be done using DistributedTransaction")]
    public sealed class CommittableTransaction : Transaction, IAsyncResult
    {
        internal bool _completedSynchronously = false;

        // Create a transaction with defaults
        public CommittableTransaction() : this(TransactionManager.DefaultIsolationLevel, TransactionManager.DefaultTimeout)
        {
        }

        // Create a transaction with the given info
        public CommittableTransaction(TimeSpan timeout) : this(TransactionManager.DefaultIsolationLevel, timeout)
        {
        }

        // Create a transaction with the given options
        public CommittableTransaction(TransactionOptions options) : this(options.IsolationLevel, options.Timeout)
        {
        }

        internal CommittableTransaction(IsolationLevel isoLevel, TimeSpan timeout) : base(isoLevel, (InternalTransaction)null)
        {
            // object to use for synchronization rather than locking on a public object
            _internalTransaction = new InternalTransaction(timeout, this);

            // Because we passed null for the internal transaction to the base class, we need to
            // fill in the traceIdentifier field here.
            _internalTransaction._cloneCount = 1;
            _cloneId = 1;
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.TransactionCreated(this, "CommittableTransaction");
            }
        }

        public IAsyncResult BeginCommit(AsyncCallback asyncCallback, object asyncState)
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceLtm, this);
                etwLog.TransactionCommit(this, "CommittableTransaction");
            }

            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(CommittableTransaction));
            }

            lock (_internalTransaction)
            {
                if (_complete)
                {
                    throw TransactionException.CreateTransactionCompletedException(DistributedTxId);
                }

                // this.complete will get set to true when the transaction enters a state that is
                // beyond Phase0.
                _internalTransaction.State.BeginCommit(_internalTransaction, true, asyncCallback, asyncState);
            }

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceLtm, this);
            }

            return this;
        }

        // Forward the commit to the state machine to take the appropriate action.
        //
        public void Commit()
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceLtm, this);
                etwLog.TransactionCommit(this, "CommittableTransaction");
            }

            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(CommittableTransaction));
            }

            lock (_internalTransaction)
            {
                if (_complete)
                {
                    throw TransactionException.CreateTransactionCompletedException(DistributedTxId);
                }

                _internalTransaction.State.BeginCommit(_internalTransaction, false, null, null);

                // now that commit has started wait for the monitor on the transaction to know
                // if the transaction is done.
                do
                {
                    if (_internalTransaction.State.IsCompleted(_internalTransaction))
                    {
                        break;
                    }
                } while (Monitor.Wait(_internalTransaction));

                _internalTransaction.State.EndCommit(_internalTransaction);
            }

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceLtm, this);
            }

        }

        internal override void InternalDispose()
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceLtm, this);
            }

            if (Interlocked.Exchange(ref _disposed, Transaction._disposedTrueValue) == Transaction._disposedTrueValue)
            {
                return;
            }

            if (_internalTransaction.State.get_Status(_internalTransaction) == TransactionStatus.Active)
            {
                lock (_internalTransaction)
                {
                    // Since this is the root transaction do state based dispose.
                    _internalTransaction.State.DisposeRoot(_internalTransaction);
                }
            }

            // Attempt to clean up the internal transaction
            long remainingITx = Interlocked.Decrement(ref _internalTransaction._cloneCount);
            if (remainingITx == 0)
            {
                _internalTransaction.Dispose();
            }

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceLtm, this);
            }
        }

        public void EndCommit(IAsyncResult asyncResult)
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceLtm, this);
            }

            if (asyncResult != ((object)this))
            {
                throw new ArgumentException(SR.BadAsyncResult, nameof(asyncResult));
            }

            lock (_internalTransaction)
            {
                do
                {
                    if (_internalTransaction.State.IsCompleted(_internalTransaction))
                    {
                        break;
                    }
                } while (Monitor.Wait(_internalTransaction));

                _internalTransaction.State.EndCommit(_internalTransaction);
            }

            if (etwLog.IsEnabled())
            {
                etwLog.MethodExit(TraceSourceType.TraceSourceLtm, this);
            }
        }

        object IAsyncResult.AsyncState => _internalTransaction._asyncState;

        bool IAsyncResult.CompletedSynchronously => _completedSynchronously;

        WaitHandle IAsyncResult.AsyncWaitHandle
        {
            get
            {
                if (_internalTransaction._asyncResultEvent == null)
                {
                    lock (_internalTransaction)
                    {
                        if (_internalTransaction._asyncResultEvent == null)
                        {
                            // Demand create an event that is already signaled if the transaction has completed.
                            ManualResetEvent temp = new ManualResetEvent(
                                _internalTransaction.State.get_Status(_internalTransaction) != TransactionStatus.Active);

                            _internalTransaction._asyncResultEvent = temp;
                        }
                    }
                }

                return _internalTransaction._asyncResultEvent;
            }
        }

        bool IAsyncResult.IsCompleted
        {
            get
            {
                lock (_internalTransaction)
                {
                    return _internalTransaction.State.get_Status(_internalTransaction) != TransactionStatus.Active;
                }
            }
        }
    }
}
