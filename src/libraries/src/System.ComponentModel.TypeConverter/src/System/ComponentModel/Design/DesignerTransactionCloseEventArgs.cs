// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Design
{
    public class DesignerTransactionCloseEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new event args. Commit is true if the transaction is committed. This
        /// defaults the LastTransaction property to true.
        /// </summary>
        [Obsolete("This constructor is obsolete. Use DesignerTransactionCloseEventArgs(bool, bool) instead. https://go.microsoft.com/fwlink/?linkid=14202")]
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
