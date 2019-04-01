// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data.SqlClient.SNI
{
    /// <summary>
    /// Managed SNI proxy implementation. Contains many SNI entry points used by SqlClient.
    /// </summary>
    internal partial class SNIProxy
    {
        // On Windows the format for the SPN is SERVICE/HOST. So the separator character between the
        // SERVICE and HOST components is "/".
        private const string SpnServiceHostSeparator = "/";
    }
}
