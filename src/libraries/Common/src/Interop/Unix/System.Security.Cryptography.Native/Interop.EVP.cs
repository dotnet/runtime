// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpMdCtxCopyEx")]
        internal static partial SafeEvpMdCtxHandle EvpMdCtxCopyEx(SafeEvpMdCtxHandle ctx);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpMdCtxCreate")]
        internal static partial SafeEvpMdCtxHandle EvpMdCtxCreate(IntPtr type);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpMdCtxDestroy")]
        internal static partial void EvpMdCtxDestroy(IntPtr ctx);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDigestReset")]
        internal static partial int EvpDigestReset(SafeEvpMdCtxHandle ctx, IntPtr type);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDigestSqueeze")]
        private static partial int EvpDigestSqueeze(
            SafeEvpMdCtxHandle ctx,
            Span<byte> md,
            uint len,
            [MarshalAs(UnmanagedType.Bool)] out bool haveFeature);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDigestUpdate")]
        internal static partial int EvpDigestUpdate(SafeEvpMdCtxHandle ctx, ReadOnlySpan<byte> d, int cnt);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDigestFinalEx")]
        internal static partial int EvpDigestFinalEx(SafeEvpMdCtxHandle ctx, ref byte md, ref uint s);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDigestFinalXOF")]
        private static unsafe partial int EvpDigestFinalXOF(SafeEvpMdCtxHandle ctx, Span<byte> md, uint len);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDigestCurrentXOF")]
        private static unsafe partial int EvpDigestCurrentXOF(SafeEvpMdCtxHandle ctx, Span<byte> md, uint len);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDigestCurrent")]
        internal static partial int EvpDigestCurrent(SafeEvpMdCtxHandle ctx, ref byte md, ref uint s);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDigestOneShot")]
        internal static unsafe partial int EvpDigestOneShot(IntPtr type, byte* source, int sourceSize, byte* md, uint* mdSize);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDigestXOFOneShot")]
        private static unsafe partial int EvpDigestXOFOneShot(IntPtr type, ReadOnlySpan<byte> source, int sourceSize, Span<byte> md, uint len);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpMdSize")]
        internal static partial int EvpMdSize(IntPtr md);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetMaxMdSize")]
        private static partial int GetMaxMdSize();

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_Pbkdf2")]
        private static unsafe partial int Pbkdf2(
            ReadOnlySpan<byte> pPassword,
            int passwordLength,
            ReadOnlySpan<byte> pSalt,
            int saltLength,
            int iterations,
            IntPtr digestEvp,
            Span<byte> pDestination,
            int destinationLength);

        internal static unsafe int Pbkdf2(
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            IntPtr digestEvp,
            Span<byte> destination)
        {
            return Pbkdf2(
                password,
                password.Length,
                salt,
                salt.Length,
                iterations,
                digestEvp,
                destination,
                destination.Length);
        }

        internal static unsafe int EvpDigestFinalXOF(SafeEvpMdCtxHandle ctx, Span<byte> destination)
        {
            return EvpDigestFinalXOF(ctx, destination, (uint)destination.Length);
        }

        internal static unsafe int EvpDigestCurrentXOF(SafeEvpMdCtxHandle ctx, Span<byte> destination)
        {
            return EvpDigestCurrentXOF(ctx, destination, (uint)destination.Length);
        }

        internal static unsafe int EvpDigestXOFOneShot(IntPtr type, ReadOnlySpan<byte> source, Span<byte> destination)
        {
            return EvpDigestXOFOneShot(type, source, source.Length, destination, (uint)destination.Length);
        }

        internal static int EvpDigestSqueeze(SafeEvpMdCtxHandle ctx, Span<byte> destination)
        {
            int ret = EvpDigestSqueeze(ctx, destination, (uint)destination.Length, out bool haveFeature);

            if (!haveFeature)
            {
                throw new PlatformNotSupportedException();
            }

            return ret;
        }

        internal static readonly int EVP_MAX_MD_SIZE = GetMaxMdSize();
    }
}
