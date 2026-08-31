// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;

namespace System.Transactions.Oletx;

[Serializable]
internal sealed class OletxDependentTransaction : OletxTransaction
{
    private readonly OletxVolatileEnlistmentContainer _volatileEnlistmentContainer;

    private int _completed;

    internal OletxDependentTransaction(RealOletxTransaction realTransaction, bool delayCommit)
        : base(realTransaction)
    {
        ArgumentNullException.ThrowIfNull(realTransaction);

        _volatileEnlistmentContainer = RealOletxTransaction.AddDependentClone(delayCommit);

        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        if (etwLog.IsEnabled())
        {
            etwLog.TransactionDependentCloneCreate(TraceSourceType.TraceSourceOleTx, TransactionTraceId, delayCommit
                ? DependentCloneOption.BlockCommitUntilComplete
                : DependentCloneOption.RollbackIfNotComplete);
        }
    }

    public void Complete()
    {
        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        if (etwLog.IsEnabled())
        {
            etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, this, $"{nameof(DependentTransaction)}.{nameof(Complete)}");
        }

        Debug.Assert(Disposed == 0, "OletxTransction object is disposed");

        int localCompleted = Interlocked.Exchange(ref _completed, 1);
        if (localCompleted == 1)
        {
            throw TransactionException.CreateTransactionCompletedException(DistributedTxId);
        }

        if (etwLog.IsEnabled())
        {
            etwLog.TransactionDependentCloneComplete(TraceSourceType.TraceSourceOleTx, TransactionTraceId, "DependentTransaction");
        }

        _volatileEnlistmentContainer.DependentCloneCompleted();

        if (etwLog.IsEnabled())
        {
            etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, $"{nameof(DependentTransaction)}.{nameof(Complete)}");
        }
    }
}
