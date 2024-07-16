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
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPKeyCtxCreateFromPKey")]
        private static partial SafeEvpPKeyCtxHandle EvpPKeyCtxCreate(SafeEvpPKeyHandle pkey, IntPtr extraHandle);

        internal static SafeEvpPKeyCtxHandle EvpPKeyCtxCreate(SafeEvpPKeyHandle pkey)
            => EvpPKeyCtxCreate(pkey, pkey.ExtraHandle);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPKeyCtxCreate")]
        private static partial SafeEvpPKeyCtxHandle EvpPKeyCtxCreate(SafeEvpPKeyHandle pkey, IntPtr extraHandle, SafeEvpPKeyHandle peerkey, out uint secretLength);

        internal static SafeEvpPKeyCtxHandle EvpPKeyCtxCreate(SafeEvpPKeyHandle pkey, SafeEvpPKeyHandle peerkey, out uint secretLength)
            => EvpPKeyCtxCreate(pkey, pkey.ExtraHandle, peerkey, out secretLength);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPKeyCtxConfigureForECDSASign")]
        private static partial int EvpPKeyCtxConfigureForECDSASignCore(SafeEvpPKeyCtxHandle ctx);

        internal static void EvpPKeyCtxConfigureForECDSASign(SafeEvpPKeyCtxHandle ctx)
        {
            Debug.Assert(ctx != null);
            Debug.Assert(!ctx.IsInvalid);

            if (EvpPKeyCtxConfigureForECDSASignCore(ctx) != 1)
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPKeyCtxConfigureForECDSAVerify")]
        private static partial int EvpPKeyCtxConfigureForECDSAVerifyCore(SafeEvpPKeyCtxHandle ctx);

        internal static void EvpPKeyCtxConfigureForECDSAVerify(SafeEvpPKeyCtxHandle ctx)
        {
            Debug.Assert(ctx != null);
            Debug.Assert(!ctx.IsInvalid);

            if (EvpPKeyCtxConfigureForECDSAVerifyCore(ctx) != 1)
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPKeyCtxSignHash")]
        private static unsafe partial int EvpPKeyCtxSignHash(SafeEvpPKeyCtxHandle ctx, byte* hash, int hashLen, byte* destination, ref int destinationLen);

        internal static unsafe bool TryEvpPKeyCtxSignHash(SafeEvpPKeyCtxHandle ctx, ReadOnlySpan<byte> hash, Span<byte> destination, out int bytesWritten)
        {
            Debug.Assert(ctx != null);
            Debug.Assert(!ctx.IsInvalid);

            if (hash.Length == 0 || destination.Length == 0)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = destination.Length;
            ref byte hashRef = ref MemoryMarshal.GetReference(hash);
            ref byte destRef = ref MemoryMarshal.GetReference(destination);
            fixed (byte* hashPtr = &hashRef)
            fixed (byte* destPtr = &destRef)
            {
                return EvpPKeyCtxSignHash(ctx, hashPtr, hash.Length, destPtr, ref bytesWritten) == 1;
            }
        }

        internal static unsafe bool TryEvpPKeyCtxSignatureSize(SafeEvpPKeyCtxHandle ctx, ReadOnlySpan<byte> hash, out int bytesWritten)
        {
            Debug.Assert(ctx != null);
            Debug.Assert(!ctx.IsInvalid);

            bytesWritten = 0;

            if (hash.Length == 0)
            {
                return false;
            }

            ref byte hashRef = ref MemoryMarshal.GetReference(hash);
            fixed (byte* hashPtr = &hashRef)
            {
                byte* destPtr = null;
                return EvpPKeyCtxSignHash(ctx, hashPtr, hash.Length, destPtr, ref bytesWritten) == 1;
            }
        }

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPKeyCtxVerifyHash")]
        private static partial int EvpPKeyCtxVerifyHash(SafeEvpPKeyCtxHandle ctx, ref byte hash, int hashLen, ref byte signature, int signatureLen);

        internal static bool EvpPKeyCtxVerifyHash(SafeEvpPKeyCtxHandle ctx, ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature)
        {
            Debug.Assert(ctx != null);
            Debug.Assert(!ctx.IsInvalid);

            return EvpPKeyCtxVerifyHash(ctx, ref MemoryMarshal.GetReference(hash), hash.Length, ref MemoryMarshal.GetReference(signature), signature.Length) == 1;
        }

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPKeyDeriveSecretAgreement")]
        private static partial int EvpPKeyDeriveSecretAgreement(
            ref byte secret,
            uint secretLength,
            SafeEvpPKeyCtxHandle ctx);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPKeyCtxDestroy")]
        internal static partial void EvpPKeyCtxDestroy(IntPtr ctx);

        internal static void EvpPKeyDeriveSecretAgreement(SafeEvpPKeyCtxHandle ctx, Span<byte> destination)
        {
            Debug.Assert(ctx != null);
            Debug.Assert(!ctx.IsInvalid);

            int ret = EvpPKeyDeriveSecretAgreement(
                ref MemoryMarshal.GetReference(destination),
                (uint)destination.Length,
                ctx);

            if (ret != 1)
            {
                throw CreateOpenSslCryptographicException();
            }
        }
    }
}
