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
        private static volatile IntPtr s_evpSha3_256;
        private static volatile IntPtr s_evpSha3_384;
        private static volatile IntPtr s_evpSha3_512;
        private static volatile IntPtr s_evpSha3_Shake128;
        private static volatile IntPtr s_evpSha3_Shake256;

        [LibraryImport(Libraries.CryptoNative)]
        private static partial IntPtr CryptoNative_EvpMd5();

        private static IntPtr EvpMd5() =>
            s_evpMd5 != IntPtr.Zero ? s_evpMd5 : (s_evpMd5 = CryptoNative_EvpMd5());

        [LibraryImport(Libraries.CryptoNative)]
        private static partial IntPtr CryptoNative_EvpSha1();

        private static IntPtr EvpSha1() =>
            s_evpSha1 != IntPtr.Zero ? s_evpSha1 : (s_evpSha1 = CryptoNative_EvpSha1());

        [LibraryImport(Libraries.CryptoNative)]
        private static partial IntPtr CryptoNative_EvpSha256();

        private static IntPtr EvpSha256() =>
            s_evpSha256 != IntPtr.Zero ? s_evpSha256 : (s_evpSha256 = CryptoNative_EvpSha256());

        [LibraryImport(Libraries.CryptoNative)]
        private static partial IntPtr CryptoNative_EvpSha384();

        private static IntPtr EvpSha384() =>
            s_evpSha384 != IntPtr.Zero ? s_evpSha384 : (s_evpSha384 = CryptoNative_EvpSha384());

        [LibraryImport(Libraries.CryptoNative)]
        private static partial IntPtr CryptoNative_EvpSha512();

        private static IntPtr EvpSha512() =>
            s_evpSha512 != IntPtr.Zero ? s_evpSha512 : (s_evpSha512 = CryptoNative_EvpSha512());

        [LibraryImport(Libraries.CryptoNative)]
        private static partial IntPtr CryptoNative_EvpSha3_256();

        private static IntPtr EvpSha3_256() =>
            s_evpSha3_256 != IntPtr.Zero ? s_evpSha3_256 : (s_evpSha3_256 = CryptoNative_EvpSha3_256());

        [LibraryImport(Libraries.CryptoNative)]
        private static partial IntPtr CryptoNative_EvpSha3_384();

        private static IntPtr EvpSha3_384() =>
            s_evpSha3_384 != IntPtr.Zero ? s_evpSha3_384 : (s_evpSha3_384 = CryptoNative_EvpSha3_384());

        [LibraryImport(Libraries.CryptoNative)]
        private static partial IntPtr CryptoNative_EvpSha3_512();

        private static IntPtr EvpSha3_512() =>
            s_evpSha3_512 != IntPtr.Zero ? s_evpSha3_512 : (s_evpSha3_512 = CryptoNative_EvpSha3_512());

        [LibraryImport(Libraries.CryptoNative)]
        private static partial IntPtr CryptoNative_EvpShake128();

        private static IntPtr EvpShake128() =>
            s_evpSha3_Shake128 != IntPtr.Zero ? s_evpSha3_Shake128 : (s_evpSha3_Shake128 = CryptoNative_EvpShake128());

        [LibraryImport(Libraries.CryptoNative)]
        private static partial IntPtr CryptoNative_EvpShake256();

        private static IntPtr EvpShake256() =>
            s_evpSha3_Shake256 != IntPtr.Zero ? s_evpSha3_Shake256 : (s_evpSha3_Shake256 = CryptoNative_EvpShake256());

        internal static IntPtr HashAlgorithmToEvp(string hashAlgorithmId)
        {
            return hashAlgorithmId switch
            {
                HashAlgorithmNames.MD5 => EvpMd5(),
                HashAlgorithmNames.SHA1 => EvpSha1(),
                HashAlgorithmNames.SHA256 => EvpSha256(),
                HashAlgorithmNames.SHA384 => EvpSha384(),
                HashAlgorithmNames.SHA512 => EvpSha512(),
                HashAlgorithmNames.SHA3_256 => EvpSha3_256(),
                HashAlgorithmNames.SHA3_384 => EvpSha3_384(),
                HashAlgorithmNames.SHA3_512 => EvpSha3_512(),
                HashAlgorithmNames.SHAKE128 => EvpShake128(),
                HashAlgorithmNames.SHAKE256 => EvpShake256(),
                _ => throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId)),
            };
        }

        internal static bool HashAlgorithmSupported(string hashAlgorithmId)
        {
            switch (hashAlgorithmId)
            {
                case HashAlgorithmNames.SHA1:
                case HashAlgorithmNames.SHA256:
                case HashAlgorithmNames.SHA384:
                case HashAlgorithmNames.SHA512:
                case HashAlgorithmNames.MD5:
                case HashAlgorithmNames.SHA3_256:
                case HashAlgorithmNames.SHA3_384:
                case HashAlgorithmNames.SHA3_512:
                case HashAlgorithmNames.SHAKE128:
                case HashAlgorithmNames.SHAKE256:
                    return true;
                default:
                    throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId));
            }
        }
    }
}
