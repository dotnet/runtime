// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative)]
        private static extern SafeEvpPKeyHandle CryptoNative_RsaGenerateKey(int keySize);

        internal static SafeEvpPKeyHandle RsaGenerateKey(int keySize)
        {
            SafeEvpPKeyHandle pkey = CryptoNative_RsaGenerateKey(keySize);

            if (pkey.IsInvalid)
            {
                pkey.Dispose();
                throw CreateOpenSslCryptographicException();
            }

            return pkey;
        }

        [DllImport(Libraries.CryptoNative)]
        private static extern int CryptoNative_RsaDecrypt(
            SafeEvpPKeyHandle pkey,
            ref byte source,
            int sourceLength,
            RSAEncryptionPaddingMode paddingMode,
            IntPtr digestAlgorithm,
            ref byte destination);

        internal static int RsaDecrypt(
            SafeEvpPKeyHandle pkey,
            ReadOnlySpan<byte> source,
            RSAEncryptionPaddingMode paddingMode,
            IntPtr digestAlgorithm,
            Span<byte> destination)
        {
            int written = CryptoNative_RsaDecrypt(
                pkey,
                ref MemoryMarshal.GetReference(source),
                source.Length,
                paddingMode,
                digestAlgorithm,
                ref MemoryMarshal.GetReference(destination));

            if (written < 0)
            {
                Debug.Assert(written == -1);
                throw CreateOpenSslCryptographicException();
            }

            return written;
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPkeyGetRsa")]
        internal static extern SafeRsaHandle EvpPkeyGetRsa(SafeEvpPKeyHandle pkey);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPkeySetRsa")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvpPkeySetRsa(SafeEvpPKeyHandle pkey, SafeRsaHandle rsa);
    }
}
