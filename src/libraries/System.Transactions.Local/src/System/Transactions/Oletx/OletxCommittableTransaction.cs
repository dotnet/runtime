// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Transactions.Oletx;

/// <summary>
/// A Transaction object represents a single transaction.  It is created by TransactionManager
/// objects through CreateTransaction or UnmarshalTransaction.  Alternatively, the static Create
/// methodis provided, which creates a "default" TransactionManager and requests that it create
/// a new transaction with default values.  A transaction can only be committed by
/// the client application that created the transaction.  If a client application wishes to allow
/// access to the transaction by multiple threads, but wants to prevent those other threads from
/// committing the transaction, the application can make a "clone" of the transaction.  Transaction
/// clones have the same capabilities as the original transaction, except for the ability to commit
/// the transaction.
/// </summary>
[Serializable]
internal sealed class OletxCommittableTransaction : OletxTransaction
{
    private bool _commitCalled;

    /// <summary>
    /// Constructor for the Transaction object.  Specifies the TransactionManager instance that is
    /// creating the transaction.
    /// </summary>
    internal OletxCommittableTransaction(RealOletxTransaction realOletxTransaction)
        : base(realOletxTransaction)
    {
        realOletxTransaction.CommittableTransaction = this;
    }

    internal bool CommitCalled => _commitCalled;

    internal void BeginCommit(InternalTransaction internalTransaction)
    {
        TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
        if (etwLog.IsEnabled())
        {
            etwLog.MethodEnter(TraceSourceType.TraceSourceOleTx, this);
            etwLog.TransactionCommit(TraceSourceType.TraceSourceOleTx, TransactionTraceId, "CommittableTransaction");
        }

        Debug.Assert(0 == Disposed, "OletxTransction object is disposed");
        RealOletxTransaction.InternalTransaction = internalTransaction;

        _commitCalled = true;

        RealOletxTransaction.Commit();

        if (etwLog.IsEnabled())
        {
            etwLog.MethodExit(TraceSourceType.TraceSourceOleTx, this, $"{nameof(OletxCommittableTransaction)}.{nameof(BeginCommit)}");
        }
    }
}
