// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS.AllHeaders
{
    /// <summary>
    /// Type of the individual header
    /// </summary>
    public enum TDSHeaderType : ushort
    {
        QueryNotifications = 0x0001,
        TransactionDescriptor = 0x0002,
        Trace = 0x0003
    }
}
