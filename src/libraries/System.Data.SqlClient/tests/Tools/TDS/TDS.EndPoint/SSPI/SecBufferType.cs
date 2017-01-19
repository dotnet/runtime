// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS.EndPoint.SSPI
{
    /// <summary>
    /// Type of security buffer
    /// </summary>
    internal enum SecBufferType : int
    {
        Empty = 0,
        Data = 1,
        Token = 2
    }
}
