// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        private static volatile IntPtr s_evpMd5;
        private static volatile IntPtr s_evpSha1;
        private static volatile IntPtr s_evpSha256;
        private static volatile IntPtr s_evpSha384;
        private static volatile IntPtr s_evpSha512;

        [DllImport(Libraries.AndroidCryptoNative)]
        private static extern IntPtr CryptoNative_EvpMd5();

        internal static IntPtr EvpMd5() =>
            s_evpMd5 != IntPtr.Zero ? s_evpMd5 : (s_evpMd5 = CryptoNative_EvpMd5());

        [DllImport(Libraries.AndroidCryptoNative)]
        internal static extern IntPtr CryptoNative_EvpSha1();

        internal static IntPtr EvpSha1() =>
            s_evpSha1 != IntPtr.Zero ? s_evpSha1 : (s_evpSha1 = CryptoNative_EvpSha1());

        [DllImport(Libraries.AndroidCryptoNative)]
        internal static extern IntPtr CryptoNative_EvpSha256();

        internal static IntPtr EvpSha256() =>
            s_evpSha256 != IntPtr.Zero ? s_evpSha256 : (s_evpSha256 = CryptoNative_EvpSha256());

        [DllImport(Libraries.AndroidCryptoNative)]
        internal static extern IntPtr CryptoNative_EvpSha384();

        internal static IntPtr EvpSha384() =>
            s_evpSha384 != IntPtr.Zero ? s_evpSha384 : (s_evpSha384 = CryptoNative_EvpSha384());

        [DllImport(Libraries.AndroidCryptoNative)]
        internal static extern IntPtr CryptoNative_EvpSha512();

        internal static IntPtr EvpSha512() =>
            s_evpSha512 != IntPtr.Zero ? s_evpSha512 : (s_evpSha512 = CryptoNative_EvpSha512());

        internal static IntPtr HashAlgorithmToEvp(string hashAlgorithmId) => hashAlgorithmId switch
        {
            nameof(HashAlgorithmName.SHA1) => EvpSha1(),
            nameof(HashAlgorithmName.SHA256) => EvpSha256(),
            nameof(HashAlgorithmName.SHA384) => EvpSha384(),
            nameof(HashAlgorithmName.SHA512) => EvpSha512(),
            nameof(HashAlgorithmName.MD5) => EvpMd5(),
            _ => throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId))
        };
    }
}
