// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Transactions
{
    /// <summary>
    /// This identifier is used in tracing to distinguish transaction
    /// enlistments.  This identifier is only unique within
    /// a given AppDomain.
    /// </summary>
    internal readonly struct EnlistmentTraceIdentifier : IEquatable<EnlistmentTraceIdentifier>
    {
        public static readonly EnlistmentTraceIdentifier Empty = new EnlistmentTraceIdentifier();

        public EnlistmentTraceIdentifier(
            Guid resourceManagerIdentifier,
            TransactionTraceIdentifier transactionTraceId,
            int enlistmentIdentifier)
        {
            _resourceManagerIdentifier = resourceManagerIdentifier;
            _transactionTraceIdentifier = transactionTraceId;
            _enlistmentIdentifier = enlistmentIdentifier;
        }

        private readonly Guid _resourceManagerIdentifier;
        /// <summary>
        /// The Guid identifier of the resource manager that made the
        /// enlistment.  If the enlistment is a Volatile enlistment,
        /// this value will be Guid.Empty.
        /// </summary>
        public Guid ResourceManagerIdentifier => _resourceManagerIdentifier;

        private readonly TransactionTraceIdentifier _transactionTraceIdentifier;
        /// <summary>
        /// The TransactionTraceIdentifier for the transaction that is
        /// enlisted upon.
        /// </summary>
        public TransactionTraceIdentifier TransactionTraceId => _transactionTraceIdentifier;

        private readonly int _enlistmentIdentifier;
        /// <summary>
        /// A value that distinguishes between multiple enlistments on the same
        /// transaction instance by the same resource manager.
        /// </summary>
        public int EnlistmentIdentifier => _enlistmentIdentifier;

        public override int GetHashCode() => base.GetHashCode();  // Don't have anything better to do.

        public override bool Equals(object obj) => obj is EnlistmentTraceIdentifier && Equals((EnlistmentTraceIdentifier)obj);

        public bool Equals(EnlistmentTraceIdentifier other) =>
            _enlistmentIdentifier == other._enlistmentIdentifier &&
            _resourceManagerIdentifier == other._resourceManagerIdentifier &&
            _transactionTraceIdentifier == other._transactionTraceIdentifier;

        public static bool operator ==(EnlistmentTraceIdentifier left, EnlistmentTraceIdentifier right) => left.Equals(right);

        public static bool operator !=(EnlistmentTraceIdentifier left, EnlistmentTraceIdentifier right) => !left.Equals(right);
    }
}
