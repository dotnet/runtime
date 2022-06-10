// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.InteropServices;
// using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading;
using System.Transactions.Diagnostics;

#nullable disable

namespace System.Transactions.Oletx
{
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
    internal class OletxCommittableTransaction : OletxTransaction
    {
        bool commitCalled = false;

        /// <summary>
        /// Constructor for the Transaction object.  Specifies the TransactionManager instance that is
        /// creating the transaction.
        /// </summary>
        /// <param name="transactionManager">
        /// Specifies the TransactionManager instance that is creating the transaction.
        /// </param>
        internal OletxCommittableTransaction( RealOletxTransaction realOletxTransaction )
            : base( realOletxTransaction )
        {
            realOletxTransaction.committableTransaction = this;
        }

        internal bool CommitCalled
        {
            get { return this.commitCalled; }
        }


        internal void BeginCommit(
            InternalTransaction internalTransaction
            )
        {
            if ( DiagnosticTrace.Verbose )
            {
                MethodEnteredTraceRecord.Trace( SR.TraceSourceOletx,
                    "CommittableTransaction.BeginCommit"
                    );
                TransactionCommitCalledTraceRecord.Trace( SR.TraceSourceOletx,
                    this.TransactionTraceId
                    );
            }

            Debug.Assert( ( 0 == this.disposed ), "OletxTransction object is disposed" );
            this.realOletxTransaction.InternalTransaction = internalTransaction;

            this.commitCalled = true;

            this.realOletxTransaction.Commit();

            if ( DiagnosticTrace.Verbose )
            {
                MethodExitedTraceRecord.Trace( SR.TraceSourceOletx,
                    "CommittableTransaction.BeginCommit"
                    );
            }

            return;
        }

    }

}
