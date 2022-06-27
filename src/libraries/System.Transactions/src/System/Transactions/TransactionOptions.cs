// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Transactions
{
    public struct TransactionOptions : IEquatable<TransactionOptions>
    {
        private TimeSpan _timeout;
        private IsolationLevel _isolationLevel;

        public TimeSpan Timeout
        {
            get { return _timeout; }
            set { _timeout = value; }
        }

        public IsolationLevel IsolationLevel
        {
            get { return _isolationLevel; }
            set { _isolationLevel = value; }
        }

        public override int GetHashCode() => base.GetHashCode();  // Don't have anything better to do.

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is TransactionOptions transactionOptions && Equals(transactionOptions);

        /// <summary>Indicates whether the current instance is equal to another instance of the same type.</summary>
        /// <param name="other">An instance to compare with this instance.</param>
        /// <returns>true if the current instance is equal to the other instance; otherwise, false.</returns>
        public bool Equals(TransactionOptions other) =>
            _timeout == other._timeout &&
            _isolationLevel == other._isolationLevel;

        public static bool operator ==(TransactionOptions x, TransactionOptions y) => x.Equals(y);

        public static bool operator !=(TransactionOptions x, TransactionOptions y) => !x.Equals(y);
    }
}
