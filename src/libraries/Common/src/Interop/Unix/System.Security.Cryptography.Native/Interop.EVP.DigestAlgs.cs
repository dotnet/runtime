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
        private static volatile bool s_evpSha3_256Cached;
        private static volatile bool s_evpSha3_384Cached;
        private static volatile bool s_evpSha3_512Cached;
        private static volatile bool s_evpSha3_Shake128Cached;
        private static volatile bool s_evpSha3_Shake256Cached;

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

        private static IntPtr EvpSha3_256()
        {
            if (!s_evpSha3_256Cached)
            {
                s_evpSha3_256 = CryptoNative_EvpSha3_256();
                s_evpSha3_256Cached = true;
            }

            return s_evpSha3_256;
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial IntPtr CryptoNative_EvpSha3_384();

        private static IntPtr EvpSha3_384()
        {
            if (!s_evpSha3_384Cached)
            {
                s_evpSha3_384 = CryptoNative_EvpSha3_384();
                s_evpSha3_384Cached = true;
            }

            return s_evpSha3_384;
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial IntPtr CryptoNative_EvpSha3_512();

        private static IntPtr EvpSha3_512()
        {
            if (!s_evpSha3_512Cached)
            {
                s_evpSha3_512 = CryptoNative_EvpSha3_512();
                s_evpSha3_512Cached = true;
            }

            return s_evpSha3_512;
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial IntPtr CryptoNative_EvpShake128();

        private static IntPtr EvpShake128()
        {
            if (!s_evpSha3_Shake128Cached)
            {
                s_evpSha3_Shake128 = CryptoNative_EvpShake128();
                s_evpSha3_Shake128Cached = true;
            }

            return s_evpSha3_Shake128;
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial IntPtr CryptoNative_EvpShake256();

        private static IntPtr EvpShake256()
        {
            if (!s_evpSha3_Shake256Cached)
            {
                s_evpSha3_Shake256 = CryptoNative_EvpShake256();
                s_evpSha3_Shake256Cached = true;
            }

            return s_evpSha3_Shake256;
        }

        internal static IntPtr HashAlgorithmToEvp(string hashAlgorithmId)
        {
            switch (hashAlgorithmId)
            {
                case HashAlgorithmNames.SHA1: return EvpSha1();
                case HashAlgorithmNames.SHA256: return EvpSha256();
                case HashAlgorithmNames.SHA384: return EvpSha384();
                case HashAlgorithmNames.SHA512: return EvpSha512();
                case HashAlgorithmNames.SHA3_256:
                    IntPtr sha3_256 = EvpSha3_256();
                    return sha3_256 != 0 ? sha3_256 : throw new PlatformNotSupportedException();
                case HashAlgorithmNames.SHA3_384:
                    IntPtr sha3_384 = EvpSha3_384();
                    return sha3_384 != 0 ? sha3_384 : throw new PlatformNotSupportedException();
                case HashAlgorithmNames.SHA3_512:
                    IntPtr sha3_512 = EvpSha3_512();
                    return sha3_512 != 0 ? sha3_512 : throw new PlatformNotSupportedException();
                case HashAlgorithmNames.SHAKE128:
                    IntPtr shake128 = EvpShake128();
                    return shake128 != 0 ? shake128 : throw new PlatformNotSupportedException();
                case HashAlgorithmNames.SHAKE256:
                    IntPtr shake256 = EvpShake256();
                    return shake256 != 0 ? shake256 : throw new PlatformNotSupportedException();
                case nameof(HashAlgorithmName.MD5): return EvpMd5();
                default:
                    throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId));
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
                    return true;
                case HashAlgorithmNames.SHA3_256:
                    return EvpSha3_256() != 0;
                case HashAlgorithmNames.SHA3_384:
                    return EvpSha3_384() != 0;
                case HashAlgorithmNames.SHA3_512:
                    return EvpSha3_512() != 0;
                case HashAlgorithmNames.SHAKE128:
                    return EvpShake128() != 0;
                case HashAlgorithmNames.SHAKE256:
                    return EvpShake256() != 0;
                default:
                    throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId));
            }
        }
    }
}
