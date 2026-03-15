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

        [LibraryImport(Libraries.AndroidCryptoNative)]
        private static partial IntPtr CryptoNative_EvpMd5();

        internal static IntPtr EvpMd5() =>
            s_evpMd5 != IntPtr.Zero ? s_evpMd5 : (s_evpMd5 = CryptoNative_EvpMd5());

        [LibraryImport(Libraries.AndroidCryptoNative)]
        internal static partial IntPtr CryptoNative_EvpSha1();

        internal static IntPtr EvpSha1() =>
            s_evpSha1 != IntPtr.Zero ? s_evpSha1 : (s_evpSha1 = CryptoNative_EvpSha1());

        [LibraryImport(Libraries.AndroidCryptoNative)]
        internal static partial IntPtr CryptoNative_EvpSha256();

        internal static IntPtr EvpSha256() =>
            s_evpSha256 != IntPtr.Zero ? s_evpSha256 : (s_evpSha256 = CryptoNative_EvpSha256());

        [LibraryImport(Libraries.AndroidCryptoNative)]
        internal static partial IntPtr CryptoNative_EvpSha384();

        internal static IntPtr EvpSha384() =>
            s_evpSha384 != IntPtr.Zero ? s_evpSha384 : (s_evpSha384 = CryptoNative_EvpSha384());

        [LibraryImport(Libraries.AndroidCryptoNative)]
        internal static partial IntPtr CryptoNative_EvpSha512();

        internal static IntPtr EvpSha512() =>
            s_evpSha512 != IntPtr.Zero ? s_evpSha512 : (s_evpSha512 = CryptoNative_EvpSha512());

        internal static IntPtr HashAlgorithmToEvp(string hashAlgorithmId) => hashAlgorithmId switch
        {
            HashAlgorithmNames.SHA1 => EvpSha1(),
            HashAlgorithmNames.SHA256 => EvpSha256(),
            HashAlgorithmNames.SHA384 => EvpSha384(),
            HashAlgorithmNames.SHA512 => EvpSha512(),
            HashAlgorithmNames.MD5 => EvpMd5(),
            HashAlgorithmNames.SHA3_256 or HashAlgorithmNames.SHA3_384 or HashAlgorithmNames.SHA3_512 =>
                throw new PlatformNotSupportedException(),
            HashAlgorithmNames.SHAKE128 or HashAlgorithmNames.SHAKE256 => throw new PlatformNotSupportedException(),
            _ => throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId))
        };

        internal static bool HashAlgorithmSupported(string hashAlgorithmId)
        {
            switch (hashAlgorithmId)
            {
                case HashAlgorithmNames.SHA1:
                case HashAlgorithmNames.SHA256:
                case HashAlgorithmNames.SHA384:
                case HashAlgorithmNames.SHA512:
                case HashAlgorithmNames.MD5:
                    return true;
                case HashAlgorithmNames.SHA3_256:
                case HashAlgorithmNames.SHA3_384:
                case HashAlgorithmNames.SHA3_512:
                case HashAlgorithmNames.SHAKE128:
                case HashAlgorithmNames.SHAKE256:
                    return false;
                default:
                    throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId));
            }
        }
    }
}
