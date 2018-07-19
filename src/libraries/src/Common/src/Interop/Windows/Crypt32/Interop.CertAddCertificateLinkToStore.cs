// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal const uint CERT_STORE_ADD_ALWAYS = 4;

        [DllImport(Interop.Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal extern static bool CertAddCertificateLinkToStore(SafeCertStoreHandle hCertStore, SafeCertContextHandle pCertContext, uint dwAddDisposition, [In, Out] SafeCertContextHandle ppStoreContext);
    }
}
