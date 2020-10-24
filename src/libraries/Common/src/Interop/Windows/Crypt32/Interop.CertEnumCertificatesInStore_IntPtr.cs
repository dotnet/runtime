// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [DllImport(Interop.Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr CertEnumCertificatesInStore(SafeCertStoreHandle hCertStore, IntPtr pPrevCertContext);
    }
}
