// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.Design
{
    public class DesignerTransactionCloseEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new event args. Commit is true if the transaction is committed. This
        /// defaults the LastTransaction property to true.
        /// </summary>
        [Obsolete("This constructor has been deprecated. Use DesignerTransactionCloseEventArgs(bool, bool) instead.")]
        public DesignerTransactionCloseEventArgs(bool commit) : this(commit, lastTransaction: true)
        {
        }

        /// <summary>
        /// Creates a new event args. Commit is true if the transaction is committed, and
        /// lastTransaction is true if this is the last transaction to close.
        /// </summary>
        public DesignerTransactionCloseEventArgs(bool commit, bool lastTransaction)
        {
            TransactionCommitted = commit;
            LastTransaction = lastTransaction;
        }

        public bool TransactionCommitted { get; }

        public bool LastTransaction { get; }
    }
}
