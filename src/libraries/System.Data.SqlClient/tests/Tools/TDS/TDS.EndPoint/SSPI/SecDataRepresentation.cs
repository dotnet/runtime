// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS.EndPoint.SSPI
{
    /// <summary>
    /// Data representation requirements
    /// </summary>
    internal enum SecDataRepresentation : int
    {
        Network = 0x00000000,
        Native = 0x00000010
    }
}
