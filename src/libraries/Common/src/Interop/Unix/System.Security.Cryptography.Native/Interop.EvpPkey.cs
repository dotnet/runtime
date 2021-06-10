// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPkeyCreate")]
        internal static extern SafeEvpPKeyHandle EvpPkeyCreate();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPkeyDestroy")]
        internal static extern void EvpPkeyDestroy(IntPtr pkey);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPKeySize")]
        internal static extern int EvpPKeySize(SafeEvpPKeyHandle pkey);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_UpRefEvpPkey")]
        internal static extern int UpRefEvpPkey(SafeEvpPKeyHandle handle);

        [DllImport(Libraries.CryptoNative)]
        private static extern unsafe SafeEvpPKeyHandle CryptoNative_DecodeSubjectPublicKeyInfo(
            byte* buf,
            int len,
            int algId);

        [DllImport(Libraries.CryptoNative)]
        private static extern unsafe SafeEvpPKeyHandle CryptoNative_DecodePkcs8PrivateKey(
            byte* buf,
            int len,
            int algId);

        internal static unsafe SafeEvpPKeyHandle DecodeSubjectPublicKeyInfo(
            ReadOnlySpan<byte> source,
            EvpAlgorithmId algorithmId)
        {
            SafeEvpPKeyHandle handle;

            fixed (byte* sourcePtr = source)
            {
                handle = CryptoNative_DecodeSubjectPublicKeyInfo(
                    sourcePtr,
                    source.Length,
                    (int)algorithmId);
            }

            if (handle.IsInvalid)
            {
                handle.Dispose();
                throw CreateOpenSslCryptographicException();
            }

            return handle;
        }

        internal static unsafe SafeEvpPKeyHandle DecodePkcs8PrivateKey(
            ReadOnlySpan<byte> source,
            EvpAlgorithmId algorithmId)
        {
            SafeEvpPKeyHandle handle;

            fixed (byte* sourcePtr = source)
            {
                handle = CryptoNative_DecodePkcs8PrivateKey(
                    sourcePtr,
                    source.Length,
                    (int)algorithmId);
            }

            if (handle.IsInvalid)
            {
                handle.Dispose();
                throw CreateOpenSslCryptographicException();
            }

            return handle;
        }

        internal enum EvpAlgorithmId
        {
            Unknown = 0,
            RSA = 6,
            DSA = 116,
            ECC = 408,
        }
    }
}
