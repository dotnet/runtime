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
        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_HmacCreate")]
        internal static partial SafeHmacCtxHandle HmacCreate(ref byte key, int keyLen, IntPtr md);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_HmacDestroy")]
        internal static partial void HmacDestroy(IntPtr ctx);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_HmacReset")]
        internal static partial int HmacReset(SafeHmacCtxHandle ctx);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_HmacUpdate")]
        internal static partial int HmacUpdate(SafeHmacCtxHandle ctx, ReadOnlySpan<byte> data, int len);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_HmacFinal")]
        internal static partial int HmacFinal(SafeHmacCtxHandle ctx, ref byte data, ref int len);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_HmacCurrent")]
        internal static partial int HmacCurrent(SafeHmacCtxHandle ctx, ref byte data, ref int len);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_HmacOneShot")]
        private static unsafe partial int HmacOneShot(IntPtr type, byte* key, int keySize, byte* source, int sourceSize, byte* md, ref int mdSize);

        internal static unsafe int HmacOneShot(IntPtr type, ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination)
        {
            int size = destination.Length;
            const int Success = 1;

            fixed (byte* pKey = key)
            fixed (byte* pSource = source)
            fixed (byte* pDestination = destination)
            {
                int result = HmacOneShot(type, pKey, key.Length, pSource, source.Length, pDestination, ref size);

                if (result != Success)
                {
                    Debug.Assert(result == 0);
                    throw CreateOpenSslCryptographicException();
                }
            }

            return size;
        }
    }
}
