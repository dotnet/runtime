// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Transactions
{
    public struct TransactionOptions
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

        public override bool Equals(object obj) => obj is TransactionOptions && Equals((TransactionOptions)obj);

        private bool Equals(TransactionOptions other) =>
            _timeout == other._timeout &&
            _isolationLevel == other._isolationLevel;

        public static bool operator ==(TransactionOptions x, TransactionOptions y) => x.Equals(y);

        public static bool operator !=(TransactionOptions x, TransactionOptions y) => !x.Equals(y);
    }
}
