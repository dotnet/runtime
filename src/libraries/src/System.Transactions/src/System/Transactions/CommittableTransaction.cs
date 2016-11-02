// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Transactions.Diagnostics;

namespace System.Transactions
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2229", Justification="Serialization not yet supported and will be done using DistributedTransaction")]
    [Serializable]
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
            if (DiagnosticTrace.Information)
            {
                TransactionCreatedTraceRecord.Trace(SR.TraceSourceLtm, TransactionTraceId);
            }
        }

        public IAsyncResult BeginCommit(AsyncCallback asyncCallback, object asyncState)
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "CommittableTransaction.BeginCommit");
                TransactionCommitCalledTraceRecord.Trace(SR.TraceSourceLtm, TransactionTraceId);
            }

            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(CommittableTransaction));
            }

            lock (_internalTransaction)
            {
                if (_complete)
                {
                    throw TransactionException.CreateTransactionCompletedException(SR.TraceSourceLtm, DistributedTxId);
                }

                // this.complete will get set to true when the transaction enters a state that is
                // beyond Phase0.
                _internalTransaction.State.BeginCommit(_internalTransaction, true, asyncCallback, asyncState);
            }

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceLtm, "CommittableTransaction.BeginCommit");
            }

            return this;
        }

        // Forward the commit to the state machine to take the appropriate action.
        //
        public void Commit()
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "CommittableTransaction.Commit");
                TransactionCommitCalledTraceRecord.Trace(SR.TraceSourceLtm, TransactionTraceId);
            }

            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(CommittableTransaction));
            }

            lock (_internalTransaction)
            {
                if (_complete)
                {
                    throw TransactionException.CreateTransactionCompletedException(SR.TraceSourceLtm, DistributedTxId);
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

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceLtm, "CommittableTransaction.Commit");
            }
        }

        internal override void InternalDispose()
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "IDisposable.Dispose");
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

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceLtm, "IDisposable.Dispose");
            }
        }

        public void EndCommit(IAsyncResult asyncResult)
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceLtm, "CommittableTransaction.EndCommit");
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

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceLtm, "CommittableTransaction.EndCommit");
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
