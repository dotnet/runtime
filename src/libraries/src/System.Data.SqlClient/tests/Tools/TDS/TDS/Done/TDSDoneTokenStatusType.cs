// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.SqlServer.TDS.Done
{
    /// <summary>
    /// Status of the token
    /// </summary>
    [Flags]
    public enum TDSDoneTokenStatusType : ushort
    {
        Final = 0x00,
        More = 0x01,
        Error = 0x02,
        TransactionInProgress = 0x04,
        Count = 0x10,
        Attention = 0x20,
        RPCInBatch = 0x80,
        ServerError = 0x100,
    }
}
