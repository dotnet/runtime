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
        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X509ChainCreateDefaultPolicy")]
        internal static partial SafeCreateHandle X509ChainCreateDefaultPolicy();

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X509ChainCreateRevocationPolicy")]
        internal static partial SafeCreateHandle X509ChainCreateRevocationPolicy();

        [LibraryImport(Libraries.AppleCryptoNative)]
        internal static partial int AppleCryptoNative_X509ChainCreate(
            SafeCreateHandle certs,
            SafeCreateHandle policies,
            out SafeX509ChainHandle pTrustOut,
            out int pOSStatus);

        [LibraryImport(Libraries.AppleCryptoNative)]
        internal static partial int AppleCryptoNative_X509ChainEvaluate(
            SafeX509ChainHandle chain,
            SafeCFDateHandle cfEvaluationTime,
            [MarshalAs(UnmanagedType.Bool)] bool allowNetwork,
            out int pOSStatus);

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X509ChainGetChainSize")]
        internal static partial long X509ChainGetChainSize(SafeX509ChainHandle chain);

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X509ChainGetCertificateAtIndex")]
        internal static partial IntPtr X509ChainGetCertificateAtIndex(SafeX509ChainHandle chain, long index);

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X509ChainGetTrustResults")]
        internal static partial SafeCreateHandle X509ChainGetTrustResults(SafeX509ChainHandle chain);

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X509ChainGetStatusAtIndex")]
        internal static partial int X509ChainGetStatusAtIndex(SafeCreateHandle trustResults, long index, out int pdwStatus);

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_GetOSStatusForChainStatus")]
        internal static partial int GetOSStatusForChainStatus(X509ChainStatusFlags flag);

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X509ChainSetTrustAnchorCertificates")]
        internal static partial int X509ChainSetTrustAnchorCertificates(SafeX509ChainHandle chain, SafeCreateHandle anchorCertificates);
    }
}
