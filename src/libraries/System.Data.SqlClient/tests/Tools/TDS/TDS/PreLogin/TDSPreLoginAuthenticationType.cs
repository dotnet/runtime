// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS.PreLogin
{
    /// <summary>
    /// FedAuthRequired option in the prelogin packet.
    /// </summary>
    public enum TdsPreLoginFedAuthRequiredOption : byte
    {
        FedAuthNotRequired = 0x00,
        FedAuthRequired = 0x01,
        Illegal = 0x02
    }
}