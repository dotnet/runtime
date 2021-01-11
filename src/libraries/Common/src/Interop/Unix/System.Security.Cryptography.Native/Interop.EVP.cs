// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        private static IntPtr s_md5;
        private static IntPtr s_sha1;
        private static IntPtr s_sha256;
        private static IntPtr s_sha384;
        private static IntPtr s_sha512;

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDigestOneShot")]
        internal static unsafe extern int EvpDigestOneShot(IntPtr type, byte* source, int sourceSize, byte* md, ref uint mdSize);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpMdSize")]
        internal static extern int EvpMdSize(IntPtr md);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpMd5")]
        private static extern IntPtr CryptoNative_EvpMd5();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpSha1")]
        private static extern IntPtr CryptoNative_EvpSha1();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpSha256")]
        private static extern IntPtr CryptoNative_EvpSha256();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpSha384")]
        private static extern IntPtr CryptoNative_EvpSha384();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpSha512")]
        private static extern IntPtr CryptoNative_EvpSha512();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetMaxMdSize")]
        private static extern int GetMaxMdSize();

        // For these shared algorithm handles the native functions are idempotent,
        // so it doesn't matter if there's a parallel race to set the field initially.
        internal static IntPtr EvpMd5() =>
            s_md5 != IntPtr.Zero ? s_md5 : (s_md5 = CryptoNative_EvpMd5());

        internal static IntPtr EvpSha1() =>
            s_sha1 != IntPtr.Zero ? s_sha1 : (s_sha1 = CryptoNative_EvpSha1());

        internal static IntPtr EvpSha256() =>
            s_sha256 != IntPtr.Zero ? s_sha256 : (s_sha256 = CryptoNative_EvpSha256());

        internal static IntPtr EvpSha384() =>
            s_sha384 != IntPtr.Zero ? s_sha384 : (s_sha384 = CryptoNative_EvpSha384());

        internal static IntPtr EvpSha512() =>
            s_sha512 != IntPtr.Zero ? s_sha512 : (s_sha512 = CryptoNative_EvpSha512());

        internal static IntPtr GetDigestAlgorithm(string algorithmName)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(algorithmName));

            switch (algorithmName)
            {
                case "MD5":
                    return EvpMd5();
                case "SHA1":
                    return EvpSha1();
                case "SHA256":
                    return EvpSha256();
                case "SHA384":
                    return EvpSha384();
                case "SHA512":
                    return EvpSha512();
                default:
                    throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, algorithmName);
            }
        }

        internal static readonly int EVP_MAX_MD_SIZE = GetMaxMdSize();
    }
}
