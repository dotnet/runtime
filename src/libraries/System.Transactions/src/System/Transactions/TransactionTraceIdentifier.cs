// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Transactions
{
    /// <summary>
    /// This identifier is used in tracing to distiguish instances
    /// of transaction objects.  This identifier is only unique within
    /// a given AppDomain.
    /// </summary>
    internal struct TransactionTraceIdentifier
    {
        public static readonly TransactionTraceIdentifier Empty = new TransactionTraceIdentifier();

        public TransactionTraceIdentifier(string transactionIdentifier, int cloneIdentifier)
        {
            _transactionIdentifier = transactionIdentifier;
            _cloneIdentifier = cloneIdentifier;
        }

        private string _transactionIdentifier;
        /// <summary>
        /// The string representation of the transaction identifier.
        /// </summary>
        public string TransactionIdentifier => _transactionIdentifier;

        private int _cloneIdentifier;
        /// <summary>
        /// An integer value that allows different clones of the same
        /// transaction to be distiguished in the tracing.
        /// </summary>
        public int CloneIdentifier => _cloneIdentifier;

        public override int GetHashCode() => base.GetHashCode();  // Don't have anything better to do.

        public override bool Equals(object objectToCompare)
        {
            if (!(objectToCompare is TransactionTraceIdentifier))
            {
                return false;
            }

            TransactionTraceIdentifier id = (TransactionTraceIdentifier)objectToCompare;
            if ((id.TransactionIdentifier != TransactionIdentifier) ||
                (id.CloneIdentifier != CloneIdentifier))
            {
                return false;
            }
            return true;
        }

        public static bool operator ==(TransactionTraceIdentifier id1, TransactionTraceIdentifier id2) => id1.Equals(id2);

        public static bool operator !=(TransactionTraceIdentifier id1, TransactionTraceIdentifier id2) => !id1.Equals(id2);
    }
}
