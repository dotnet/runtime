// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [LibraryImport(Interop.Libraries.Crypt32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CertFreeCertificateContext(IntPtr pCertContext);

        [LibraryImport(Interop.Libraries.Crypt32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CertVerifyCertificateChainPolicy(
            IntPtr pszPolicyOID,
            SafeX509ChainHandle pChainContext,
            ref CERT_CHAIN_POLICY_PARA pPolicyPara,
            ref CERT_CHAIN_POLICY_STATUS pPolicyStatus);
    }
}
