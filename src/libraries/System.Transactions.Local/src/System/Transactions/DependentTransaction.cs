// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Transactions
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2229", Justification = "Serialization not yet supported and will be done using DistributedTransaction")]
    public sealed class DependentTransaction : Transaction
    {
        private bool _blocking;

        // Create a transaction with the given settings
        //
        internal DependentTransaction(IsolationLevel isoLevel, InternalTransaction internalTransaction, bool blocking) :
            base(isoLevel, internalTransaction)
        {
            _blocking = blocking;
            lock (_internalTransaction)
            {
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
                if (Disposed)
                {
                    throw new ObjectDisposedException(nameof(DependentTransaction));
                }

                if (_complete)
                {
                    throw TransactionException.CreateTransactionCompletedException(DistributedTxId);
                }

                _complete = true;

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
