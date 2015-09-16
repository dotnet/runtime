// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [DllImport(Interop.Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CertFreeCertificateContext(IntPtr pCertContext);

        [DllImport(Interop.Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CertVerifyCertificateChainPolicy(
            IntPtr pszPolicyOID,
            SafeX509ChainHandle pChainContext,
            [In] ref CERT_CHAIN_POLICY_PARA pPolicyPara,
            [In, Out] ref CERT_CHAIN_POLICY_STATUS pPolicyStatus);
    }
}
