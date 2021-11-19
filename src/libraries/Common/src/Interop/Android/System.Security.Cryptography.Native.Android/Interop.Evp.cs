// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpMdCtxCreate")]
        internal static partial SafeEvpMdCtxHandle EvpMdCtxCreate(IntPtr type);

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpMdCtxDestroy")]
        internal static partial void EvpMdCtxDestroy(IntPtr ctx);

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpDigestReset")]
        internal static partial int EvpDigestReset(SafeEvpMdCtxHandle ctx, IntPtr type);

        internal static int EvpDigestUpdate(SafeEvpMdCtxHandle ctx, ReadOnlySpan<byte> d, int cnt) =>
            EvpDigestUpdate(ctx, ref MemoryMarshal.GetReference(d), cnt);

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpDigestUpdate")]
        private static partial int EvpDigestUpdate(SafeEvpMdCtxHandle ctx, ref byte d, int cnt);

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpDigestFinalEx")]
        internal static partial int EvpDigestFinalEx(SafeEvpMdCtxHandle ctx, ref byte md, ref uint s);

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpDigestCurrent")]
        internal static partial int EvpDigestCurrent(SafeEvpMdCtxHandle ctx, ref byte md, ref uint s);

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpDigestOneShot")]
        internal static unsafe partial int EvpDigestOneShot(IntPtr type, byte* source, int sourceSize, byte* md, uint* mdSize);

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpMdSize")]
        internal static partial int EvpMdSize(IntPtr md);

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_GetMaxMdSize")]
        private static partial int GetMaxMdSize();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_Pbkdf2")]
        private static unsafe partial int Pbkdf2(
            byte* pPassword,
            int passwordLength,
            byte* pSalt,
            int saltLength,
            int iterations,
            IntPtr digestEvp,
            byte* pDestination,
            int destinationLength);

        internal static unsafe int Pbkdf2(
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            IntPtr digestEvp,
            Span<byte> destination)
        {
            fixed (byte* pPassword = password)
            fixed (byte* pSalt = salt)
            fixed (byte* pDestination = destination)
            {
                return Pbkdf2(
                    pPassword,
                    password.Length,
                    pSalt,
                    salt.Length,
                    iterations,
                    digestEvp,
                    pDestination,
                    destination.Length);
            }
        }

        internal static readonly int EVP_MAX_MD_SIZE = GetMaxMdSize();
    }
}
