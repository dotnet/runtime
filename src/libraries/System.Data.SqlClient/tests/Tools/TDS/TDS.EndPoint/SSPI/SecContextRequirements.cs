// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS.EndPoint.SSPI
{
    /// <summary>
    /// Security context requirements
    /// </summary>
    internal enum SecContextRequirements : int
    {
        Delegate = 0x00000001,
        MutualAuthentication = 0x00000002,
        Integrity = 0x00010000,
        ExtendedError = 0x00004000
    }
}
