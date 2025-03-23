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
        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_EcDsaSignHash(
            SafeEvpPKeyHandle pkey,
            IntPtr extraHandle,
            ref byte hash,
            int hashLength,
            ref byte destination,
            int destinationLength);

        internal static int EcDsaSignHash(
            SafeEvpPKeyHandle pkey,
            ReadOnlySpan<byte> hash,
            Span<byte> destination)
        {
            int written = CryptoNative_EcDsaSignHash(
                pkey,
                pkey.ExtraHandle,
                ref MemoryMarshal.GetReference(hash),
                hash.Length,
                ref MemoryMarshal.GetReference(destination),
                destination.Length);

            if (written < 0)
            {
                Debug.Assert(written == -1);
                throw CreateOpenSslCryptographicException();
            }

            return written;
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_EcDsaVerifyHash(
            SafeEvpPKeyHandle pkey,
            IntPtr extraHandle,
            ref byte hash,
            int hashLength,
            ref byte signature,
            int signatureLength);

        internal static bool EcDsaVerifyHash(
            SafeEvpPKeyHandle pkey,
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> signature)
        {
            int ret = CryptoNative_EcDsaVerifyHash(
                pkey,
                pkey.ExtraHandle,
                ref MemoryMarshal.GetReference(hash),
                hash.Length,
                ref MemoryMarshal.GetReference(signature),
                signature.Length);

            if (ret == 1)
            {
                return true;
            }

            if (ret == 0)
            {
                return false;
            }

            Debug.Assert(ret == -1);
            throw CreateOpenSslCryptographicException();
        }
    }
}
