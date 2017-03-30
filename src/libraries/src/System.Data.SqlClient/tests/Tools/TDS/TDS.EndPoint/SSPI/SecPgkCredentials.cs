// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS.EndPoint.SSPI
{
    /// <summary>
    /// Security package credentials enumeration
    /// </summary>
    internal enum SecPgkCredentials : int
    {
        Inbound = 1,
        Outbound = 2
    }
}
