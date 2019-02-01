// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Transactions;

namespace System.Data.SqlClient
{
    sealed internal partial class SqlDelegatedTransaction : IPromotableSinglePhaseNotification
    {
        // Get the server-side Global Transaction Id from the PromotedDTCToken
        // Skip first 4 bytes since they contain the version
        private Guid GetGlobalTxnIdentifierFromToken() => new Guid(new ReadOnlySpan<byte>(_connection.PromotedDTCToken, _globalTransactionsTokenVersionSizeInBytes, 16));
    }
}
