// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

using Microsoft.Win32.SafeHandles;

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
            internal FILETIME NotBefore;
            internal FILETIME NotAfter;
            internal DATA_BLOB Subject;
            internal CERT_PUBLIC_KEY_INFO SubjectPublicKeyInfo;
            internal CRYPT_BIT_BLOB IssuerUniqueId;
            internal CRYPT_BIT_BLOB SubjectUniqueId;
            internal int cExtension;
            internal IntPtr rgExtension;
        }
    }
}

