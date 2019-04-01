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
        // On Unix/Linux the format for the SPN is SERVICE@HOST. This is because the System.Net.Security.Native
        // GSS-API layer uses GSS_C_NT_HOSTBASED_SERVICE format for the SPN.
        private const string SpnServiceHostSeparator = "@";
    }
}
