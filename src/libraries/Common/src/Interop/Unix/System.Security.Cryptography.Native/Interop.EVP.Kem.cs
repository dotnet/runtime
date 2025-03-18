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
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpKemFree")]
        internal static partial void EvpKemFree(IntPtr kem);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpKemGeneratePkey")]
        private static partial SafeEvpPKeyHandle CryptoNative_EvpKemGeneratePkey(SafeEvpKemHandle kem, ReadOnlySpan<byte> seed, int seedLength);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpKemExportPrivateSeed")]
        private static partial int CryptoNative_EvpKemExportPrivateSeed(SafeEvpPKeyHandle kem, Span<byte> destination, int destinationLength);

        internal static SafeEvpPKeyHandle EvpKemGeneratePkey(SafeEvpKemHandle kem)
        {
            SafeEvpPKeyHandle handle = CryptoNative_EvpKemGeneratePkey(kem, ReadOnlySpan<byte>.Empty, 0);

            if (handle.IsInvalid)
            {
                Exception ex = CreateOpenSslCryptographicException();
                handle.Dispose();
                throw ex;
            }

            return handle;
        }

        internal static SafeEvpPKeyHandle EvpKemGeneratePkey(SafeEvpKemHandle kem, ReadOnlySpan<byte> seed)
        {
            if (seed.IsEmpty)
            {
                Debug.Fail("Generating a key with a seed requires a non-empty seed.");
                throw new CryptographicException();
            }

            SafeEvpPKeyHandle handle = CryptoNative_EvpKemGeneratePkey(kem, seed, seed.Length);

            if (handle.IsInvalid)
            {
                Exception ex = CreateOpenSslCryptographicException();
                handle.Dispose();
                throw ex;
            }

            return handle;
        }

        internal static void EvpKemExportPrivateSeed(SafeEvpPKeyHandle kem, Span<byte> destination)
        {
            const int Success = 1;
            const int Fail = 0;

            int ret = CryptoNative_EvpKemExportPrivateSeed(kem, destination, destination.Length);

            switch (ret)
            {
                case Success:
                    return;
                case Fail:
                    destination.Clear();
                    throw CreateOpenSslCryptographicException();
                default:
                    destination.Clear();
                    Debug.Fail($"Unexpected return value {ret} from {nameof(CryptoNative_EvpKemExportPrivateSeed)}.");
                    throw new CryptographicException();
            }
        }
    }
}
