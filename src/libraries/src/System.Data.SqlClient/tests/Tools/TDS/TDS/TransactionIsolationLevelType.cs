// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS
{
    /// <summary>
    /// Transaction isolation level
    /// </summary>
    public enum TransactionIsolationLevelType : byte
    {
        /// <summary>
        /// Specifies that statements can read rows that have been modified by other transactions but not yet committed
        /// </summary>
        ReadUncommited = 1,

        /// <summary>
        /// Specifies that statements cannot read data that has been modified but not committed by other transactions
        /// </summary>
        ReadCommited = 2,

        /// <summary>
        /// Specifies that statements cannot read data that has been modified but not yet committed by other transactions and that no other transactions can modify data that has been read by the current transaction until the current transaction completes.
        /// </summary>
        RepeatableRead = 3,

        /// <summary>
        /// Specifies that data read by any statement in a transaction will be the transactionally consistent version of the data that existed at the start of the transaction
        /// </summary>
        Serializable = 4,

        /// <summary>
        /// Statements cannot read data that has been modified but not yet committed by other transactions
        /// </summary>
        Snapshot = 5
    }
}
