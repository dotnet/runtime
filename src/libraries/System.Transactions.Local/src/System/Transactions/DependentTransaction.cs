// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Transactions
{
    public sealed class DependentTransaction : Transaction
    {
        private readonly bool _blocking;

        // Create a transaction with the given settings
        //
        internal DependentTransaction(IsolationLevel isoLevel, InternalTransaction internalTransaction, bool blocking) :
            base(isoLevel, internalTransaction)
        {
            _blocking = blocking;
            lock (_internalTransaction)
            {
                Debug.Assert(_internalTransaction.State != null);
                if (blocking)
                {
                    _internalTransaction.State.CreateBlockingClone(_internalTransaction);
                }
                else
                {
                    _internalTransaction.State.CreateAbortingClone(_internalTransaction);
                }
            }
        }

        public void Complete()
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceLtm, this);
            }

            lock (_internalTransaction)
            {
                ObjectDisposedException.ThrowIf(Disposed, this);

                if (_complete)
                {
                    throw TransactionException.CreateTransactionCompletedException(DistributedTxId);
                }

                _complete = true;

                Debug.Assert(_internalTransaction.State != null);
                if (_blocking)
                {
                    _internalTransaction.State.CompleteBlockingClone(_internalTransaction);
                }
                else
                {
                    _internalTransaction.State.CompleteAbortingClone(_internalTransaction);
                }
            }

            if (etwLog.IsEnabled())
            {
                etwLog.TransactionDependentCloneComplete(this, "DependentTransaction");
                etwLog.MethodExit(TraceSourceType.TraceSourceLtm, this);
            }
        }
    }
}
