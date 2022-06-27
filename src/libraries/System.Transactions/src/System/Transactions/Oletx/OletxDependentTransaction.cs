// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;
using System.Transactions.Diagnostics;

#nullable disable

namespace System.Transactions.Oletx
{
    [Serializable]
    internal class OletxDependentTransaction : OletxTransaction
    {
        private OletxVolatileEnlistmentContainer _volatileEnlistmentContainer;

        private int _completed;

        internal OletxDependentTransaction(RealOletxTransaction realTransaction, bool delayCommit)
            : base(realTransaction)
        {
            if (realTransaction == null)
            {
                throw new ArgumentNullException( "realTransaction" );
            }

            _volatileEnlistmentContainer = RealOletxTransaction.AddDependentClone(delayCommit);

            if (DiagnosticTrace.Information)
            {
                DependentCloneCreatedTraceRecord.Trace(
                    SR.TraceSourceOletx,
                    TransactionTraceId,
                    delayCommit
                        ? DependentCloneOption.BlockCommitUntilComplete
                        : DependentCloneOption.RollbackIfNotComplete);
            }
        }

        public void Complete()
        {
            if (DiagnosticTrace.Verbose)
            {
                MethodEnteredTraceRecord.Trace(SR.TraceSourceOletx, "DependentTransaction.Complete");
            }

            Debug.Assert(Disposed == 0, "OletxTransction object is disposed");

            int localCompleted = Interlocked.CompareExchange(ref _completed, 1, 0);
            if (localCompleted == 1)
            {
                throw TransactionException.CreateTransactionCompletedException(DistributedTxId);
            }

            if (DiagnosticTrace.Information)
            {
                DependentCloneCompleteTraceRecord.Trace(SR.TraceSourceOletx, TransactionTraceId);
            }

            _volatileEnlistmentContainer.DependentCloneCompleted();

            if (DiagnosticTrace.Verbose)
            {
                MethodExitedTraceRecord.Trace(SR.TraceSourceOletx, "DependentTransaction.Complete");
            }
        }
    }
}
