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
        private static extern SafeEvpPKeyHandle CryptoNative_EvpPKeyCreateDsa(IntPtr dsa);

        internal static SafeEvpPKeyHandle EvpPKeyCreateDsa(IntPtr dsa)
        {
            Debug.Assert(dsa != IntPtr.Zero);

            SafeEvpPKeyHandle pkey = CryptoNative_EvpPKeyCreateDsa(dsa);

            if (pkey.IsInvalid)
            {
                pkey.Dispose();
                throw CreateOpenSslCryptographicException();
            }

            return pkey;
        }

        [DllImport(Libraries.CryptoNative)]
        private static extern SafeEvpPKeyHandle CryptoNative_DsaGenerateKey(int keySize);

        internal static SafeEvpPKeyHandle DsaGenerateKey(int keySize)
        {
            SafeEvpPKeyHandle handle = CryptoNative_DsaGenerateKey(keySize);

            if (handle.IsInvalid)
            {
                handle.Dispose();
                throw CreateOpenSslCryptographicException();
            }

            return handle;
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DsaSizeQ")]
        private static extern int DsaSizeQ(SafeEvpPKeyHandle dsa);

        /// <summary>
        /// Return the size of the 'r' or 's' signature fields in bytes.
        /// </summary>
        internal static int DsaSignatureFieldSize(SafeEvpPKeyHandle dsa)
        {
            int size = DsaSizeQ(dsa);
            Debug.Assert(size * 2 < EvpPKeySize(dsa));
            return size;
        }

        [DllImport(Libraries.CryptoNative)]
        private static extern unsafe int CryptoNative_DsaSignHash(
            SafeEvpPKeyHandle key,
            byte* hash,
            int hashLen,
            byte* destination,
            int destinationLen);

        internal static int DsaSignHash(
            SafeEvpPKeyHandle key,
            ReadOnlySpan<byte> hash,
            Span<byte> destination)
        {
            int written;

            unsafe
            {
                fixed (byte* hashPtr = hash)
                fixed (byte* destPtr = destination)
                {
                    written = CryptoNative_DsaSignHash(
                        key,
                        hashPtr,
                        hash.Length,
                        destPtr,
                        destination.Length);
                }
            }

            if (written < 0)
            {
                Debug.Assert(written == -1);
                throw CreateOpenSslCryptographicException();
            }

            return written;
        }
    }
}
