// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal static partial class CngPkcs8
    {
        internal static bool AllowsOnlyEncryptedExport(CngKey key)
        {
            const CngExportPolicies Exportable = CngExportPolicies.AllowPlaintextExport | CngExportPolicies.AllowExport;
            return (key.ExportPolicy & Exportable) == CngExportPolicies.AllowExport;
        }
    }
}
