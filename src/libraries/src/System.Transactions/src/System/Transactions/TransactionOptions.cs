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

        public override bool Equals(object obj)
        {
            if (!(obj is TransactionOptions))
            {
                // Can't use 'as' for a value type
                return false;
            }
            TransactionOptions opts = (TransactionOptions)obj;

            return (opts._timeout == _timeout) && (opts._isolationLevel == _isolationLevel);
        }

        public static bool operator ==(TransactionOptions x, TransactionOptions y) => x.Equals(y);

        public static bool operator !=(TransactionOptions x, TransactionOptions y) => !x.Equals(y);
    }
}
