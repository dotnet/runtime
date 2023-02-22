// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [LibraryImport(Libraries.Crypt32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool CertSetCertificateContextProperty(SafeCertContextHandle pCertContext, CertContextPropId dwPropId, CertSetPropertyFlags dwFlags, CRYPT_KEY_PROV_INFO* pvData);
    }
}
