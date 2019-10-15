// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal const uint PKCS_7_ASN_ENCODING = 0x00010000;
        internal const uint X509_ASN_ENCODING = 0x00000001;
        internal const uint CERT_STORE_PROV_MEMORY = 2;

        [DllImport(Interop.Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern SafeCertStoreHandle CertOpenStore(IntPtr lpszStoreProvider, uint dwMsgAndCertEncodingType, IntPtr hCryptProv, uint dwFlags, string pvPara);
    }
}
