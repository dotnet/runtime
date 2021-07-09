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
        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpMdCtxCreate")]
        internal static extern SafeEvpMdCtxHandle EvpMdCtxCreate(IntPtr type);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpMdCtxDestroy")]
        internal static extern void EvpMdCtxDestroy(IntPtr ctx);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpDigestReset")]
        internal static extern int EvpDigestReset(SafeEvpMdCtxHandle ctx, IntPtr type);

        internal static int EvpDigestUpdate(SafeEvpMdCtxHandle ctx, ReadOnlySpan<byte> d, int cnt) =>
            EvpDigestUpdate(ctx, ref MemoryMarshal.GetReference(d), cnt);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpDigestUpdate")]
        private static extern int EvpDigestUpdate(SafeEvpMdCtxHandle ctx, ref byte d, int cnt);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpDigestFinalEx")]
        internal static extern int EvpDigestFinalEx(SafeEvpMdCtxHandle ctx, ref byte md, ref uint s);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpDigestCurrent")]
        internal static extern int EvpDigestCurrent(SafeEvpMdCtxHandle ctx, ref byte md, ref uint s);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpDigestOneShot")]
        internal static unsafe extern int EvpDigestOneShot(IntPtr type, byte* source, int sourceSize, byte* md, ref uint mdSize);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpMdSize")]
        internal static extern int EvpMdSize(IntPtr md);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_GetMaxMdSize")]
        private static extern int GetMaxMdSize();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_Pbkdf2")]
        private static unsafe extern int Pbkdf2(
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
