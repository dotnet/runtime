// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct CERT_INFO
        {
            internal int dwVersion;
            internal DATA_BLOB SerialNumber;
            internal CRYPT_ALGORITHM_IDENTIFIER SignatureAlgorithm;
            internal DATA_BLOB Issuer;
            internal System.Runtime.InteropServices.ComTypes.FILETIME NotBefore;
            internal System.Runtime.InteropServices.ComTypes.FILETIME NotAfter;
            internal DATA_BLOB Subject;
            internal CERT_PUBLIC_KEY_INFO SubjectPublicKeyInfo;
            internal CRYPT_BIT_BLOB IssuerUniqueId;
            internal CRYPT_BIT_BLOB SubjectUniqueId;
            internal int cExtension;
            internal IntPtr rgExtension;
        }
    }
}
