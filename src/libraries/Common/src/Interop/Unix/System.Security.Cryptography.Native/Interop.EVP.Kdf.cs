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
