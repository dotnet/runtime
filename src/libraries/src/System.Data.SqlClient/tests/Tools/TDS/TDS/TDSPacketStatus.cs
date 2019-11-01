// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.SqlServer.TDS
{
    /// <summary>
    /// Bitmask of the packet status
    /// </summary>
    [Flags]
    public enum TDSPacketStatus
    {
        Normal = 0x0,
        EndOfMessage = 0x1,
        IgnoreEvent = 0x2,
        ResetConnection = 0x08,
        ResetConnectionSkipTransaction = 0x10
    }
}
