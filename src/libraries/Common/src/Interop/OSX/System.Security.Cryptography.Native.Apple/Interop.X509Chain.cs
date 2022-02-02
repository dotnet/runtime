// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        [GeneratedDllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X509ChainCreateDefaultPolicy")]
        internal static partial SafeCreateHandle X509ChainCreateDefaultPolicy();

        [GeneratedDllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X509ChainCreateRevocationPolicy")]
        internal static partial SafeCreateHandle X509ChainCreateRevocationPolicy();

        [GeneratedDllImport(Libraries.AppleCryptoNative)]
        internal static partial int AppleCryptoNative_X509ChainCreate(
            SafeCreateHandle certs,
            SafeCreateHandle policies,
            out SafeX509ChainHandle pTrustOut,
            out int pOSStatus);

        [GeneratedDllImport(Libraries.AppleCryptoNative)]
        internal static partial int AppleCryptoNative_X509ChainEvaluate(
            SafeX509ChainHandle chain,
            SafeCFDateHandle cfEvaluationTime,
            bool allowNetwork,
            out int pOSStatus);

        [GeneratedDllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X509ChainGetChainSize")]
        internal static partial long X509ChainGetChainSize(SafeX509ChainHandle chain);

        [GeneratedDllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X509ChainGetCertificateAtIndex")]
        internal static partial IntPtr X509ChainGetCertificateAtIndex(SafeX509ChainHandle chain, long index);

        [GeneratedDllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X509ChainGetTrustResults")]
        internal static partial SafeCreateHandle X509ChainGetTrustResults(SafeX509ChainHandle chain);

        [GeneratedDllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X509ChainGetStatusAtIndex")]
        internal static partial int X509ChainGetStatusAtIndex(SafeCreateHandle trustResults, long index, out int pdwStatus);

        [GeneratedDllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_GetOSStatusForChainStatus")]
        internal static partial int GetOSStatusForChainStatus(X509ChainStatusFlags flag);

        [GeneratedDllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X509ChainSetTrustAnchorCertificates")]
        internal static partial int X509ChainSetTrustAnchorCertificates(SafeX509ChainHandle chain, SafeCreateHandle anchorCertificates);
    }
}
