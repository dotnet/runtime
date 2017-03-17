// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X509ChainCreateDefaultPolicy")]
        internal static extern SafeCreateHandle X509ChainCreateDefaultPolicy();

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X509ChainCreateRevocationPolicy")]
        internal static extern SafeCreateHandle X509ChainCreateRevocationPolicy();

        [DllImport(Libraries.AppleCryptoNative)]
        internal static extern int AppleCryptoNative_X509ChainCreate(
            SafeCreateHandle certs,
            SafeCreateHandle policies,
            out SafeX509ChainHandle pTrustOut,
            out int pOSStatus);

        [DllImport(Libraries.AppleCryptoNative)]
        internal static extern int AppleCryptoNative_X509ChainEvaluate(
            SafeX509ChainHandle chain,
            SafeCFDateHandle cfEvaluationTime,
            bool allowNetwork,
            out int pOSStatus);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X509ChainGetChainSize")]
        internal static extern long X509ChainGetChainSize(SafeX509ChainHandle chain);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X509ChainGetCertificateAtIndex")]
        internal static extern IntPtr X509ChainGetCertificateAtIndex(SafeX509ChainHandle chain, long index);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X509ChainGetTrustResults")]
        internal static extern SafeCreateHandle X509ChainGetTrustResults(SafeX509ChainHandle chain);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X509ChainGetStatusAtIndex")]
        internal static extern int X509ChainGetStatusAtIndex(SafeCreateHandle trustResults, long index, out int pdwStatus);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_GetOSStatusForChainStatus")]
        internal static extern int GetOSStatusForChainStatus(X509ChainStatusFlags flag);
    }
}
