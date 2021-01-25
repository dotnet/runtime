// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Transactions
{
    /// <summary>
    /// This identifier is used in tracing to distinguish instances
    /// of transaction objects.  This identifier is only unique within
    /// a given AppDomain.
    /// </summary>
    internal readonly struct TransactionTraceIdentifier : IEquatable<TransactionTraceIdentifier>
    {
        public static TransactionTraceIdentifier Empty => default;

        public TransactionTraceIdentifier(string transactionIdentifier, int cloneIdentifier)
        {
            _transactionIdentifier = transactionIdentifier;
            _cloneIdentifier = cloneIdentifier;
        }

        private readonly string _transactionIdentifier;
        /// <summary>
        /// The string representation of the transaction identifier.
        /// </summary>
        public string TransactionIdentifier => _transactionIdentifier;

        private readonly int _cloneIdentifier;
        /// <summary>
        /// An integer value that allows different clones of the same
        /// transaction to be distinguished in the tracing.
        /// </summary>
        public int CloneIdentifier => _cloneIdentifier;

        public override int GetHashCode() => base.GetHashCode();  // Don't have anything better to do.

        public override bool Equals(object? obj) => obj is TransactionTraceIdentifier transactionTraceId && Equals(transactionTraceId);

        public bool Equals(TransactionTraceIdentifier other) =>
            _cloneIdentifier == other._cloneIdentifier &&
            _transactionIdentifier == other._transactionIdentifier;

        public static bool operator ==(TransactionTraceIdentifier left, TransactionTraceIdentifier right) => left.Equals(right);

        public static bool operator !=(TransactionTraceIdentifier left, TransactionTraceIdentifier right) => !left.Equals(right);
    }
}
