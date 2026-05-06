// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpKdfFree")]
        internal static partial void EvpKdfFree(IntPtr kdf);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_HkdfDeriveKey", StringMarshalling = StringMarshalling.Utf8)]
        private static partial int CryptoNative_HkdfDeriveKey(
            SafeEvpKdfHandle kdf,
            ReadOnlySpan<byte> ikm,
            int ikmLength,
            string algorithm,
            ReadOnlySpan<byte> salt,
            int saltLength,
            ReadOnlySpan<byte> info,
            int infoLength,
            Span<byte> destination,
            int destinationLength);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_HkdfExpand", StringMarshalling = StringMarshalling.Utf8)]
        private static partial int CryptoNative_HkdfExpand(
            SafeEvpKdfHandle kdf,
            ReadOnlySpan<byte> prk,
            int prkLength,
            string algorithm,
            ReadOnlySpan<byte> info,
            int infoLength,
            Span<byte> destination,
            int destinationLength);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_HkdfExtract", StringMarshalling = StringMarshalling.Utf8)]
        private static partial int CryptoNative_HkdfExtract(
            SafeEvpKdfHandle kdf,
            ReadOnlySpan<byte> ikm,
            int ikmLength,
            string algorithm,
            ReadOnlySpan<byte> salt,
            int saltLength,
            Span<byte> destination,
            int destinationLength);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_KbkdfHmacOneShot", StringMarshalling = StringMarshalling.Utf8)]
        private static unsafe partial int CryptoNative_KbkdfHmacOneShot(
            SafeEvpKdfHandle kdf,
            ReadOnlySpan<byte> key,
            int keyLength,
            string algorithm,
            ReadOnlySpan<byte> label,
            int labelLength,
            ReadOnlySpan<byte> context,
            int contextLength,
            Span<byte> destination,
            int destinationLength);

        internal static void HkdfDeriveKey(
            SafeEvpKdfHandle kdf,
            ReadOnlySpan<byte> ikm,
            string algorithm,
            ReadOnlySpan<byte> salt,
            ReadOnlySpan<byte> info,
            Span<byte> destination)
        {
            const int Success = 1;
            int ret = CryptoNative_HkdfDeriveKey(
                kdf,
                ikm,
                ikm.Length,
                algorithm,
                salt,
                salt.Length,
                info,
                info.Length,
                destination,
                destination.Length);

            if (ret != Success)
            {
                Debug.Assert(ret == 0);
                throw CreateOpenSslCryptographicException();
            }
        }

        internal static void HkdfExpand(
            SafeEvpKdfHandle kdf,
            ReadOnlySpan<byte> prk,
            string algorithm,
            ReadOnlySpan<byte> info,
            Span<byte> destination)
        {
            const int Success = 1;
            int ret = CryptoNative_HkdfExpand(
                kdf,
                prk,
                prk.Length,
                algorithm,
                info,
                info.Length,
                destination,
                destination.Length);

            if (ret != Success)
            {
                Debug.Assert(ret == 0);
                throw CreateOpenSslCryptographicException();
            }
        }

        internal static void HkdfExtract(
            SafeEvpKdfHandle kdf,
            ReadOnlySpan<byte> ikm,
            string algorithm,
            ReadOnlySpan<byte> salt,
            Span<byte> destination)
        {
            const int Success = 1;
            int ret = CryptoNative_HkdfExtract(
                kdf,
                ikm,
                ikm.Length,
                algorithm,
                salt,
                salt.Length,
                destination,
                destination.Length);

            if (ret != Success)
            {
                Debug.Assert(ret == 0);
                throw CreateOpenSslCryptographicException();
            }
        }

        internal static void KbkdfHmacOneShot(
            SafeEvpKdfHandle kdf,
            ReadOnlySpan<byte> key,
            string algorithm,
            ReadOnlySpan<byte> label,
            ReadOnlySpan<byte> context,
            Span<byte> destination)
        {
            const int Success = 1;
            int ret = CryptoNative_KbkdfHmacOneShot(
                kdf,
                key,
                key.Length,
                algorithm,
                label,
                label.Length,
                context,
                context.Length,
                destination,
                destination.Length);

            if (ret != Success)
            {
                Debug.Assert(ret == 0);
                throw CreateOpenSslCryptographicException();
            }
        }
    }
}
