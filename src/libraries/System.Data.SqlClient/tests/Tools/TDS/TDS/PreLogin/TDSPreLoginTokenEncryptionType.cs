// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.SqlServer.TDS.PreLogin
{
    /// <summary>
    /// Type of encryption specified in the pre-login packet
    /// </summary>
    public enum TDSPreLoginTokenEncryptionType : byte
    {
        Off = 0x00,
        On = 0x01,
        NotSupported = 0x02,
        Required = 0x03
    }
}
