// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.SqlServer.TDS
{
    /// <summary>
    /// Type of TDS packet
    /// </summary>
    public enum TDSPacketType: byte
    {
        SQLBatch = 1,
        PreTDS7Login = 2,
        RPC = 3,
        Response = 4,
        Attention = 6,
        BulkLoad = 7,
        TransactionManager = 14,
        TDS7Login = 16,
        SSPI = 17,
        PreLogin = 18
    }
}
