// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal const uint CERT_STORE_ADD_ALWAYS = 4;

        [GeneratedDllImport(Interop.Libraries.Crypt32, SetLastError = true)]
        internal static partial bool CertAddCertificateLinkToStore(SafeCertStoreHandle hCertStore, SafeCertContextHandle pCertContext, uint dwAddDisposition, SafeCertContextHandle ppStoreContext);
    }
}
