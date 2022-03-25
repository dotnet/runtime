// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        // Note: CertDeleteCertificateFromStore always calls CertFreeCertificateContext on pCertContext, even if an error is encountered.
        [LibraryImport(Libraries.Crypt32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool CertDeleteCertificateFromStore(CERT_CONTEXT* pCertContext);
    }
}
