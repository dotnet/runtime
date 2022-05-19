// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [LibraryImport(Libraries.Crypt32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CertGetCertificateContextProperty(
            SafeCertContextHandle pCertContext,
            CertContextPropId dwPropId,
            byte[]? pvData,
            ref int pcbData);

        [LibraryImport(Libraries.Crypt32, SetLastError = true, EntryPoint = "CertGetCertificateContextProperty")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool CertGetCertificateContextPropertyPtr(
            SafeCertContextHandle pCertContext,
            CertContextPropId dwPropId,
            byte* pvData,
            ref int pcbData);

        [LibraryImport(Libraries.Crypt32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CertGetCertificateContextProperty(
            SafeCertContextHandle pCertContext,
            CertContextPropId dwPropId,
            out IntPtr pvData,
            ref int pcbData);

        [LibraryImport(Libraries.Crypt32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CertGetCertificateContextProperty(
            SafeCertContextHandle pCertContext,
            CertContextPropId dwPropId,
            out DATA_BLOB pvData,
            ref int pcbData);
    }
}
