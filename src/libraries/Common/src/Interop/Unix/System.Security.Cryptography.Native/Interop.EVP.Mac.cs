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
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpMacFree")]
        internal static partial void EvpMacFree(IntPtr mac);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpMacCtxFree")]
        internal static partial void EvpMacCtxFree(IntPtr ctx);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpMacCtxNew")]
        private static partial SafeEvpMacCtxHandle CryptoNative_EvpMacCtxNew(SafeEvpMacHandle mac);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpMacInit")]
        private static partial int CryptoNative_EvpMacInit(
            SafeEvpMacCtxHandle ctx,
            ReadOnlySpan<byte> key,
            int keyLength,
            ReadOnlySpan<byte> customizationString,
            int customizationStringLength,
            [MarshalAs(UnmanagedType.Bool)] bool xof);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpMacUpdate")]
        private static partial int CryptoNative_EvpMacUpdate(SafeEvpMacCtxHandle ctx, ReadOnlySpan<byte> data, int dataLength);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpMacFinal")]
        private static partial int CryptoNative_EvpMacFinal(SafeEvpMacCtxHandle ctx, Span<byte> mac, int macLength);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpMacReset")]
        private static partial int CryptoNative_EvpMacReset(SafeEvpMacCtxHandle ctx);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpMacCurrent")]
        private static partial int CryptoNative_EvpMacCurrent(SafeEvpMacCtxHandle ctx, Span<byte> mac, int macLength);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpMacOneShot", StringMarshalling = StringMarshalling.Utf8)]
        private static partial int CryptoNative_EvpMacOneShot(
            SafeEvpMacHandle mac,
            ReadOnlySpan<byte> key,
            int keyLength,
            ReadOnlySpan<byte> customizationString,
            int customizationStringLength,
            ReadOnlySpan<byte> data,
            int dataLength,
            Span<byte> destination,
            int destinationLength,
            [MarshalAs(UnmanagedType.Bool)] bool xof);

        internal static void EvpMacOneShot(
            SafeEvpMacHandle mac,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> customizationString,
            ReadOnlySpan<byte> data,
            Span<byte> destination,
            bool xof)
        {
            const int Success = 1;

            int ret = CryptoNative_EvpMacOneShot(
                mac,
                key,
                key.Length,
                customizationString,
                customizationString.Length,
                data,
                data.Length,
                destination,
                destination.Length,
                xof);

            if (ret != Success)
            {
                Debug.Assert(ret == 0);
                throw CreateOpenSslCryptographicException();
            }
        }

        internal static void EvpMacFinal(SafeEvpMacCtxHandle ctx, Span<byte> mac)
        {
            int ret = CryptoNative_EvpMacFinal(ctx, mac, mac.Length);
            const int Success = 1;

            if (ret != Success)
            {
                Debug.Assert(ret == 0);
                throw CreateOpenSslCryptographicException();
            }
        }

        internal static void EvpMacCurrent(SafeEvpMacCtxHandle ctx, Span<byte> mac)
        {
            int ret = CryptoNative_EvpMacCurrent(ctx, mac, mac.Length);
            const int Success = 1;

            if (ret != Success)
            {
                Debug.Assert(ret == 0);
                throw CreateOpenSslCryptographicException();
            }
        }

        internal static SafeEvpMacCtxHandle EvpMacCtxNew(SafeEvpMacHandle mac)
        {
            SafeEvpMacCtxHandle ctx = CryptoNative_EvpMacCtxNew(mac);

            if (ctx.IsInvalid)
            {
                ctx.Dispose();
                throw CreateOpenSslCryptographicException();
            }

            return ctx;
        }

        internal static void EvpMacInit(
            SafeEvpMacCtxHandle ctx,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> customizationString,
            bool xof)
        {
            int ret = CryptoNative_EvpMacInit(ctx, key, key.Length, customizationString, customizationString.Length, xof);
            const int Success = 1;

            if (ret != Success)
            {
                Debug.Assert(ret == 0);
                throw CreateOpenSslCryptographicException();
            }
        }

        internal static void EvpMacUpdate(SafeEvpMacCtxHandle ctx, ReadOnlySpan<byte> data)
        {
            int ret = CryptoNative_EvpMacUpdate(ctx, data, data.Length);
            const int Success = 1;

            if (ret != Success)
            {
                Debug.Assert(ret == 0);
                throw CreateOpenSslCryptographicException();
            }
        }

        internal static void EvpMacReset(SafeEvpMacCtxHandle ctx)
        {
            int ret = CryptoNative_EvpMacReset(ctx);
            const int Success = 1;

            if (ret != Success)
            {
                Debug.Assert(ret == 0);
                throw CreateOpenSslCryptographicException();
            }
        }
    }
}
